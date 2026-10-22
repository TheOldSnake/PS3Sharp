using PS3Sharp.Cell.PPU.Recompiler;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS;
using PS3Sharp.OS.Syscall;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Memory.MemoryManager;

namespace PS3Sharp.Cell.PPU
{
    internal unsafe sealed partial class PPUThread : IDisposable, IEmulatorThread
    {
        internal enum PPUThreadStates
        {
            Running,
            Paused,
            Stopped
        }

        internal const uint StackReg = 1;

        internal Thread NativeThread { get; }
        internal CancellationTokenSource? LocalCancellationSource { get; private set; } = new();
        internal MemoryManager MemoryManager { get; }
        internal ThreadManager ThreadManager { get; }
        internal LV2SyscallDispatcher SyscallDispatcher { get; }
        internal PPURecompiler PPURecompiler { get; }
        internal string ThreadName { get; }
        internal uint ThreadID { get; }
        internal int ThreadPriority { get; private set; }
        internal uint Stack { get; private set; }
        internal uint StackSize { get; private set; }
        internal int ExitCode { get; private set; }
        internal List<uint> CallStack { get; private set; } = [];
        internal SysUtil.SysUtilCallback[] SysUtilCallbacks { get; private set; } = new SysUtil.SysUtilCallback[4];   // Each thread has its own?
        internal PPUThreadStates PPUThreadState { get; private set; } = PPUThreadStates.Stopped;
        internal Stack<PPUState> SavedStates = new();

        internal bool IsMainThread => ThreadName == "main_thread";
        internal bool ShouldExit => ThreadManager.GlobalCancellationSource.IsCancellationRequested || LocalCancellationSource!.IsCancellationRequested;

        internal NativeMemoryHandle? StateHandle { get; private set; } = new((nuint)sizeof(PPUState));
        internal PPUState* State => (PPUState*)StateHandle!.DangerousGetHandle();
        internal PPUGPR* GPR => &State->GPR[0];
        internal PPUFPR* FPR => &State->FPR[0];
        internal PPUVR* VRF => &State->VRF[0];

        internal GCHandle? GCHandle { get; private set; }

        // Temporary
        private Queue<Trace> TraceQueue = new();
        const int TraceMaxCount = 200;

        internal struct Trace
        {
            internal uint PC;
            internal PPUInstruction Instruction;
        }

        internal PPUThread(
            string threadName,
            uint entryPointer,
            uint stackSize,
            string? executablePath,
            uint thread_id,
            ulong thread_arg,
            ThreadManager threadManager,
            MemoryManager memoryManager,
            LV2SyscallDispatcher syscallDispatcher,
            PPURecompiler ppuRecompiler)
        {

            ThreadManager = threadManager;
            MemoryManager = memoryManager;
            SyscallDispatcher = syscallDispatcher;
            ThreadID = thread_id;
            ThreadName = threadName;
            PPURecompiler = ppuRecompiler;

            ThreadStart threadStart = PPURecompiler.IsRecompilerUsable ? RunPPUThreadJIT : RunPPUThreadInterpreter;

            NativeThread = new Thread(threadStart)
            {
                Name = $"Native PPU Thread: {ThreadName}"
            };

            SetGCHandle();

            State->PC = entryPointer != 0 ? MemoryManager.Read<BigEndian<uint>>(entryPointer) : 0;
            State->VRSAVE = 0xFFFFFFFF;

            MemoryBlock stackArea = MemoryManager.AllocateStackSpace(stackSize);
            MemoryBlock tls = MemoryManager.AllocateTLS();

            // Stack base points at the top, but leave some headroom
            // because games do access [SP] before they decrement SP
            uint stackBase = stackArea.VirtualAddress + stackSize - 32;
            Stack = stackArea.VirtualAddress;
            StackSize = stackSize;

            InitializeGPR(stackBase, entryPointer, thread_arg, tls);

            if (!string.IsNullOrEmpty(executablePath))
            {
                SetupArgs(executablePath);
            }

            CallStack.Add(State->PC);
            Console.WriteLine($"PPU Thread: PC entry = 0x{State->PC:X} stack = {GPR[1]} r2 = {GPR[2]} tls = {GPR[13]}");
        }

        public void StartThread()
        {
            Console.WriteLine($"PPU Thread Started");
            PPUThreadState = PPUThreadStates.Running;
            NativeThread.Start();
        }

        public void PauseThread()
        {
            Console.WriteLine($"PPU Thread Paused");
            PPUThreadState = PPUThreadStates.Paused;
        }

        public void StopThread()
        {
            Console.WriteLine($"PPU Thread Stopped");
            PPUThreadState = PPUThreadStates.Stopped;
            LocalCancellationSource!.Cancel();
        }

        internal uint GetPC() => State->PC;

        internal uint GetLR() => State->LR;

        internal void Return(ulong result) => GPR[Arg0].Value64 = result;

        internal T ReadArg<T>(uint index) where T : unmanaged
        {
            if (sizeof(T) > sizeof(PPUGPR))
            {
                throw new ArgumentException($"Can't read type <{nameof(T)}> (too large)");
            }

            byte* base_address = &GPR[index].Value8;
            return Unsafe.Read<T>(base_address);
        }

        internal void WriteReg<T>(uint index, T value) where T : unmanaged
        {
            if (sizeof(T) > sizeof(PPUGPR))
            {
                throw new ArgumentException($"Can't read type <{nameof(T)}> (too large)");
            }

            byte* base_address = &GPR[index].Value8;
            Unsafe.Write<T>(base_address, value);
        }

        internal void SetThreadPriority(int threadPriority)
        {
            ThreadPriority = threadPriority;
        }

        internal void SetExitCode(int code)
        {
            ExitCode = code;
        }

        internal static PPUThread GetInstance(IntPtr context)
        {
            return (PPUThread)System.Runtime.InteropServices.GCHandle.FromIntPtr(context).Target!;
        }

        private void InitializeGPR(uint stackBase, uint entryPointer, ulong thread_arg, MemoryBlock tls)
        {
            GPR[0].Value64 = State->PC;
            GPR[1].Value64 = stackBase;
            GPR[2].Value64 = entryPointer != 0 ? MemoryManager.Read<BigEndian<uint>>(entryPointer + 4) : 0;
            GPR[3].Value64 = thread_arg;
            GPR[11].Value64 = entryPointer;
            GPR[12].Value64 = 0x100000;                      //malloc pagesize or something
            GPR[13].Value64 = tls.VirtualAddress + 0x7030;   //first 0x30 bytes are reserved, guest code seems to access tls from r13 - ~0x7000 ?
        }

        private void RunPPUThreadInterpreter()
        {
            try
            {
                while (!ShouldExit)
                {
                    Interpreter.PPUInterpreter.Step(this);
                }
            }
            catch (Exception exception)
            {
                string msg = GetException(exception);      // Get the exception info
                Console.WriteLine(msg);
                ExitCode = 1;
                ThreadManager.RequestShutdown();           // Signal the thread manager to stop all threads
            }
            finally
            {
                // If we reach here then the PPU thread has been stopped
                // Call dispose
                Dispose();
            }
        }

        private void RunPPUThreadJIT()
        {
            try
            {
                while (!ShouldExit)
                {
                    PPURecompiler.RunPPU(this);
                }
            }
            catch (Exception exception)
            {
                string msg = GetException(exception);      // Get the exception info
                Console.WriteLine(msg);
                ExitCode = 1;
                ThreadManager.RequestShutdown();           // Signal the thread manager to stop all threads
            }
            finally
            {
                // If we reach here then the PPU thread has been stopped
                // Call dispose
                Dispose();
            }
        }

        private string GetException(Exception exception)
        {
            string exceptionMessage = exception.Message;
            string? trace = exception.StackTrace;
            string threadName = ThreadName;
            uint id = ThreadID;
            uint pc = GetPC();
            uint lr = GetLR();
            string msg = $"PPU Exception at thread: [{threadName}] ID: [0x{ThreadID:X8}] --  PC [0x{pc:X8}] -- LR [0x{lr:X8}] " +
                $"Message: {exceptionMessage} Trace: {trace}";

            // todo get PPU stack 
            // PPUInterpreter.PPUInterpreter.PrintCallstack(ppu);
            return msg;
        }

        private void SetupArgs(string? name)
        {
            //TODO: Do this properly 

            // Push envp NULL
            WriteStack(0);

            //Push argv NULL terminator
            WriteStack(0);

            //Save argv pointer (current SP)
            GPR[4].Value64 = GPR[StackReg].Value64;

            //Push argc
            GPR[3].Value64 = 0;
            WriteStack(GPR[3].Value64);

            //Push another 0 for alignment
            WriteStack(0);

            // Allocate 0x70 bytes for main()
            GPR[StackReg].Value64 -= 0x70;

            if ((GPR[StackReg].Value64 & 0xF) > 0)
            {
                throw new Exception("Alignment check fails");
            }

            //R28/29 are copies of R3/4 ?
            GPR[28].Value64 = GPR[3].Value64;
            GPR[29].Value64 = GPR[4].Value64;

            //R6 contains environment count
            GPR[6].Value64 = 0;

            //R5/31 are copies of R1 ?
            GPR[5].Value64 = GPR[1].Value64;
            GPR[31].Value64 = GPR[1].Value64; //Frame pointer?
        }

        private void SetGCHandle()
        {
            GCHandle = System.Runtime.InteropServices.GCHandle.Alloc(this);
            State->Context = (void*)System.Runtime.InteropServices.GCHandle.ToIntPtr(GCHandle.Value);
        }

        private void WriteStack(ulong value)
        {
            GPR[StackReg].Value32 -= sizeof(ulong); // -= 8
            MemoryManager.Write<BigEndian<ulong>>(GPR[StackReg].Value32, value);
            Console.WriteLine($"STACK[{GPR[StackReg].Value32:X8}] = {value:X16}");
        }

        private uint WritePathArg(string path)
        {
            byte[] chars = Encoding.UTF8.GetBytes(path);

            //Allocate memory for the string + a null character
            MemoryBlock nameBlock = MemoryManager.AllocateVirtualMemory((uint)chars.Length + 1);
            MemoryManager.CopyBufferToVirtualMemory(chars, (uint)nameBlock.VirtualAddress);

            //null terminate string
            MemoryManager.Write<byte>((uint)nameBlock.VirtualAddress + (uint)chars.Length, 0);
            return (uint)nameBlock.VirtualAddress;
        }

        internal void PrintTraceQueue(bool reverse = false)
        {
            Console.WriteLine($"Printing Last {TraceQueue.Count} Instructions:");
            int count = TraceQueue.Count;
            Trace t;
            if (reverse)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    t = TraceQueue.ElementAt(i);
                    Console.WriteLine($"PC = {t.PC:X} - {t.Instruction.Raw:X}");
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    t = TraceQueue.ElementAt(i);
                    Console.WriteLine($"PC = {t.PC:X} - {t.Instruction.Raw:X}");
                }
            }
        }

        internal void AddTrace(uint pc, PPUInstruction instruction)
        {
            TraceQueue.Enqueue(new Trace { PC = pc, Instruction = instruction });
            if (TraceQueue.Count > TraceMaxCount)
            {
                TraceQueue.Dequeue();
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"[{ThreadName}] Freeing PPU memory");

            // Dispose the cancellation token
            LocalCancellationSource?.Dispose();
            LocalCancellationSource = null;

            // Free the gc handle
            GCHandle?.Free();
            GCHandle = null;

            // Dispose the state handle
            StateHandle?.Dispose();
            StateHandle = null;
        }
    }
}