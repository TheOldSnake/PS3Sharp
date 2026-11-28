using PS3Sharp.Cell.Ppu.Recompiler;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.HostMemory;
using PS3Sharp.OS;
using PS3Sharp.OS.Syscall;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.GuestMemory.MemoryManager;

namespace PS3Sharp.Cell.Ppu
{
    internal sealed unsafe partial class PpuThread : IDisposable, IEmulatorThread
    {
        internal enum PpuThreadStates
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
        internal PpuRecompiler PpuRecompiler { get; }
        internal string ThreadName { get; }
        internal uint ThreadID { get; }
        internal int ThreadPriority { get; private set; }
        internal uint Stack { get; private set; }
        internal uint StackSize { get; private set; }
        internal int ExitCode { get; private set; }
        internal List<uint> CallStack { get; private set; } = [];
        internal SysUtil.SysUtilCallback[] SysUtilCallbacks { get; private set; } = new SysUtil.SysUtilCallback[4];   // Each thread has its own?
        internal PpuThreadStates PPUThreadState { get; private set; } = PpuThreadStates.Stopped;
        internal Stack<PpuState> SavedStates = new();

        internal bool IsMainThread => ThreadName == "main_thread";
        internal bool ShouldExit => ThreadManager.GlobalCancellationSource.IsCancellationRequested || LocalCancellationSource!.IsCancellationRequested;

        internal NativeMemoryHandle<PpuState> StateHandle { get; private set; } = new((nuint)sizeof(PpuState));
        internal PpuState* State => StateHandle.NativePointer;
        internal PpuGpr* Gpr => &State->Gpr[0];
        internal PpuFpr* Fpr => &State->Fpr[0];
        internal PpuVr* Vrf => &State->Vrf[0];

        internal GCHandle? GCHandle { get; private set; }

        // Temporary
        private Queue<Trace> TraceQueue = new();
        const int TraceMaxCount = 200;

        internal struct Trace
        {
            internal uint PC;
            internal PpuInstruction Instruction;
        }

        internal PpuThread(
            string threadName,
            uint entryPointer,
            uint stackSize,
            string? executablePath,
            uint thread_id,
            ulong thread_arg,
            ThreadManager threadManager,
            MemoryManager memoryManager,
            LV2SyscallDispatcher syscallDispatcher,
            PpuRecompiler ppuRecompiler)
        {

            ThreadManager = threadManager;
            MemoryManager = memoryManager;
            SyscallDispatcher = syscallDispatcher;
            ThreadID = thread_id;
            ThreadName = threadName;
            PpuRecompiler = ppuRecompiler;

            ThreadStart threadStart = PpuRecompiler.IsRecompilerUsable ? RunPpuThreadJIT : RunPpuThreadInterpreter;

            NativeThread = new Thread(threadStart)
            {
                Name = $"Native PPU Thread: {ThreadName}"
            };

            SetGCHandle();

            State->PC = entryPointer != 0 ? MemoryManager.Read<BigEndian<uint>>(entryPointer) : 0;
            State->VrSave = 0xFFFFFFFF;

            MemoryBlock stackArea = MemoryManager.Allocator.AllocateStackSpace(stackSize);
            MemoryBlock tls = MemoryManager.AllocateTlsBlock();

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

            MemoryManager.RegisterReservationKey(ThreadID);

            CallStack.Add(State->PC);
            Console.WriteLine($"PPU Thread: PC entry = 0x{State->PC:X} stack = {Gpr[1]} r2 = {Gpr[2]} tls = {Gpr[13]}");
        }

        public void StartThread()
        {
            Console.WriteLine($"PPU Thread Started");
            PPUThreadState = PpuThreadStates.Running;
            NativeThread.Start();
        }

        public void PauseThread()
        {
            Console.WriteLine($"PPU Thread Paused");
            PPUThreadState = PpuThreadStates.Paused;
        }

        public void StopThread()
        {
            Console.WriteLine($"PPU Thread Stopped");
            PPUThreadState = PpuThreadStates.Stopped;
            LocalCancellationSource!.Cancel();
        }

        internal uint GetPC() => State->PC;

        internal uint GetLR() => State->LR;

        internal void Return(ulong result) => Gpr[Arg0].Value64 = result;

        internal T ReadArg<T>(uint index) where T : unmanaged
        {
            if (sizeof(T) > sizeof(PpuGpr))
            {
                throw new ArgumentException($"Can't read type <{nameof(T)}> (too large)");
            }

            byte* base_address = &Gpr[index].Value8;
            return Unsafe.Read<T>(base_address);
        }

        internal void WriteReg<T>(uint index, T value) where T : unmanaged
        {
            if (sizeof(T) > sizeof(PpuGpr))
            {
                throw new ArgumentException($"Can't read type <{nameof(T)}> (too large)");
            }

            byte* base_address = &Gpr[index].Value8;
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

        internal static PpuThread GetInstance(IntPtr context)
        {
            return (PpuThread)System.Runtime.InteropServices.GCHandle.FromIntPtr(context).Target!;
        }

        private void InitializeGPR(uint stackBase, uint entryPointer, ulong thread_arg, MemoryBlock tls)
        {
            Gpr[0].Value64 = State->PC;
            Gpr[1].Value64 = stackBase;
            Gpr[2].Value64 = entryPointer != 0 ? MemoryManager.Read<BigEndian<uint>>(entryPointer + 4) : 0;
            Gpr[3].Value64 = thread_arg;
            Gpr[11].Value64 = entryPointer;
            Gpr[12].Value64 = 0x100000;                      //malloc pagesize or something
            Gpr[13].Value64 = tls.VirtualAddress + 0x7030;   //first 0x30 bytes are reserved, guest code seems to access tls from r13 - ~0x7000 ?
        }

        private void RunPpuThreadInterpreter()
        {
            try
            {
                while (!ShouldExit)
                {
                    Interpreter.PpuInterpreter.Step(this);
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

        private void RunPpuThreadJIT()
        {
            try
            {
                while (!ShouldExit)
                {
                    PpuRecompiler.RunPPU(this);
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
            uint pc = GetPC();
            uint lr = GetLR();

            string msg = $"PPU Exception at thread: [{threadName}] ID: [0x{ThreadID:X8}] -- PC [0x{pc:X8}] -- LR [0x{lr:X8}] \n" +
                $"Message: {exceptionMessage}\n" +
                $"{trace}";

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
            Gpr[4].Value64 = Gpr[StackReg].Value64;

            //Push argc
            Gpr[3].Value64 = 0;
            WriteStack(Gpr[3].Value64);

            //Push another 0 for alignment
            WriteStack(0);

            // Allocate 0x70 bytes for main()
            Gpr[StackReg].Value64 -= 0x70;

            if ((Gpr[StackReg].Value64 & 0xF) > 0)
            {
                throw new Exception("Alignment check fails");
            }

            //R28/29 are copies of R3/4 ?
            Gpr[28].Value64 = Gpr[3].Value64;
            Gpr[29].Value64 = Gpr[4].Value64;

            //R6 contains environment count
            Gpr[6].Value64 = 0;

            //R5/31 are copies of R1 ?
            Gpr[5].Value64 = Gpr[1].Value64;
            Gpr[31].Value64 = Gpr[1].Value64; //Frame pointer?
        }

        private void SetGCHandle()
        {
            GCHandle = System.Runtime.InteropServices.GCHandle.Alloc(this);
            State->Context = (void*)System.Runtime.InteropServices.GCHandle.ToIntPtr(GCHandle.Value);
        }

        private void WriteStack(ulong value)
        {
            Gpr[StackReg].Value32 -= sizeof(ulong); // -= 8
            MemoryManager.Write<BigEndian<ulong>>(Gpr[StackReg].Value32, value);
            Console.WriteLine($"STACK[{Gpr[StackReg].Value32:X8}] = {value:X16}");
        }

        private uint WritePathArg(string path)
        {
            byte[] chars = Encoding.UTF8.GetBytes(path);

            //Allocate memory for the string + a null character
            MemoryBlock nameBlock = MemoryManager.Allocator.AllocateVirtualMemory((uint)chars.Length + 1);
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

        internal void AddTrace(uint pc, PpuInstruction instruction)
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
            StateHandle.Dispose();
        }
    }
}