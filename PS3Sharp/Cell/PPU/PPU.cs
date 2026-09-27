using PS3Sharp.Cell.RSX;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Memory_Manager.MemoryManager;

namespace PS3Sharp.Cell.PPU
{
    internal unsafe sealed partial class PPU : IThread, IDisposable
    {
        // PPU thread sealed class — all fields here are single-threaded by default.
        // DO NOT access or mutate from another thread unless using thread-safe mechanisms
        // like Interlocked, locks, or a message queue. Violating this will cause race conditions.
        internal const uint STACK_REG = 1;

        internal Thread NativeThread;
        internal RealitySynthesizer RSX;
        private readonly string _threadName;
        private readonly uint _threadID;
        private int _threadPriority;

        internal uint ThreadID => _threadID;
        internal int ThreadPriority => _threadPriority;
        internal string ThreadName => _threadName;
        internal bool IsMainThread => _threadName == "main_thread";

        internal readonly uint Stack;
        internal readonly uint StackSize;

        internal int ExitCode;
        internal List<uint> CallStack = new();
        internal SysUtil.SysUtilCallback[] SysUtilCallbacks = new SysUtil.SysUtilCallback[4];   // Each thread has its own?

        internal enum PPUThreadStates
        {
            Running,
            Paused,
            Stopped
        }

        internal PPUThreadStates PPUThreadState = PPUThreadStates.Stopped;

        // internal ConditionRegister CR;            //Condition Register
        // internal uint LR;                         //Link Register
        // internal uint TB;                         //Time Base Register -- this is actually global, not per thread
        // internal ulong CTR;                       //Count Register
        // internal uint VRSAVE = 0xFFFFFFFF;        

        // internal uint PC;                         //Program Counter


        // Unmanaged State
        [StructLayout(LayoutKind.Sequential)]
        internal struct PPUState
        {
            internal uint PC;                     // Program Counter
            internal uint LR;                     // Link Register      
            internal uint TB;                     // Time Base Register -- is this global, or per thread ?
            internal uint VRSAVE;                 // Vector Save/Restore Register, default value = 0xFFFFFFFF
            internal ConditionRegister CR;        // Condition Register
            internal ulong CTR;                   // Count Register
            internal XERRegister XER;             // XER Register
            internal InlineData.InlineArray32<PPUGPR> GPR;
            internal InlineData.InlineArray32<PPUFPR> FPR;
            internal InlineData.InlineArray32<PPUVR> VRF;
            internal void* MemoryBase;
            internal void* Context;
        }

        internal PPUState* State { get; private set; } = (PPUState*)NativeMemory.AllocZeroed((nuint)sizeof(PPUState));
        internal Stack<PPUState> SavedStates = new();

        internal PPUGPR* GPR => &State->GPR[0];
        internal PPUFPR* FPR => &State->FPR[0];
        internal PPUVR* VRF => &State->VRF[0];

        internal GCHandle Handle { get; private set; }

        // Helpers for syscalls and libraries

        internal uint GetPC() => State->PC;
        internal uint GetLR() => State->LR;
        internal void Return(ulong result) => GPR[ARG0].Value64 = result;


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

        internal PPUGPR[] CaptureGPRState()
        {
            PPUGPR[] gprs = new PPUGPR[32];
            for (int i = 0; i < gprs.Length; i++)
            {
                gprs[i] = GPR[i];
            }

            return gprs;
        }

        internal PPUFPR[] CaptureFPRState()
        {
            PPUFPR[] fprs = new PPUFPR[32];
            for (int i = 0; i < fprs.Length; i++)
            {
                fprs[i] = FPR[i];
            }

            return fprs;
        }

        internal void SetThreadPriority(int threadPriority)
        {
            _threadPriority = threadPriority;
        }

        //Fixed-Point Exception Register
        //internal XERRegister XER;
        internal struct XERRegister
        {
            internal const int SOBit = 31;
            internal const int OVBit = 30;
            internal const int CABit = 29;

            internal ulong Raw;
            internal uint SO => (uint)((Raw >> SOBit) & 1);
            internal uint OV => (uint)((Raw >> OVBit) & 1);
            internal uint CA => (uint)((Raw >> CABit) & 1);

            internal void SetSO(uint v)
            {
                Raw &= ~(1u << SOBit);
                Raw |= (v << SOBit);
            }

            internal void SetOV(uint v)
            {
                Raw &= ~(1u << OVBit);
                Raw |= (v << OVBit);
            }

            internal void SetCA(uint v)
            {
                Raw &= ~(1u << CABit);
                Raw |= (v << CABit);
            }
        }

        // The bits in the Condition Register are grouped into
        // eight 4-bit fields, named CR Field 0 (CR0), ..., CR Field 7 (CR7)
        internal enum CRNumber
        {
            CR0 = 0,
            CR1 = 1,
            CR2 = 2,
            CR3 = 3,
            CR4 = 4,
            CR5 = 5,
            CR6 = 6,
            CR7 = 7
        }

        internal struct ConditionRegister
        {
            internal const uint EQ = 0b0010;
            internal const uint GT = 0b0100;
            internal const uint LT = 0b1000;

            internal uint Raw;

            internal void WriteCRField(CRNumber cr, uint value)
            {
                value &= 0xF;
                int bit = 28 - ((int)cr * 4);  // CR Fields are reversed
                uint clearMask = (uint)~(0xF << bit);
                Raw &= clearMask;
                Raw |= value << bit;
            }

            internal uint GetCRField(CRNumber cr)
            {
                int bit = 28 - ((int)cr * 4);  // CR Fields are reversed
                return (Raw >> bit) & 0xF;
            }

            internal void WriteCRBit(uint bit, uint value)
            {       //bit number is in big endian
                int position = (int)(31 - bit);
                Raw &= ((uint)~(1 << position));
                Raw |= (value << position);
            }

            internal uint ReadCRBit(uint bit)
            {                 //bit number is in big endian
                int position = (int)(31 - bit);
                return (Raw >> position) & 1;
            }
        }

        internal static PPU GetInstance(IntPtr context)
        {
            return (PPU)GCHandle.FromIntPtr(context).Target!;
        }

        //Constructor for main thread -- called by PS3Sharp.cs
        internal PPU(
            string threadName,
            uint entryPointer,
            uint stackSize = MemoryManager.DEFAULT_STACK_SIZE,
            string? path_to_executable = null,
            uint thread_id = 0)
        {

            if ((stackSize & 0xF) > 0)
            {
                stackSize = AlignPowerOfTwo(stackSize, 16);
            }

            if (stackSize < 0x1000)
            {
                stackSize = 0x1000;
            }

            SetGCHandle();
            SetPageTable();

            _threadName = threadName;
            _threadID = ThreadManager.GetNewID();

            RSX = PS3Sharp.RSX;

            State->PC = MemoryManager.Read<uint>(entryPointer);
            State->VRSAVE = 0xFFFFFFFF;

            MemoryBlock stackArea = MemoryManager.AllocateStackSpace(stackSize);
            uint stackBase = (uint)(stackArea.VirtualAddress + stackSize - 16);

            Stack = stackArea.VirtualAddress;
            StackSize = stackSize;

            MemoryBlock tls = MemoryManager.AllocateTLS();

            GPR[0].Value64 = State->PC;
            GPR[1].Value64 = stackBase;
            GPR[2].Value64 = MemoryManager.Read<uint>(entryPointer + 4);

            // Not sure
            // GPR[7].Value64 = _threadID;
            // GPR[8].Value64 = tls.VirtualAddress;
            // GPR[9].Value64 = tls.Size;
            // GPR[10].Value64 = tls.Size;

            GPR[11].Value64 = entryPointer;
            GPR[12].Value64 = 0x100000;     //malloc pagesize or something

            GPR[13].Value64 = tls.VirtualAddress + 0x7030;   //first 0x30 bytes are reserved, guest code seems to access tls from r13 - ~0x7000 ?

            ArgumentNullException.ThrowIfNullOrEmpty(path_to_executable);
            SetupArgs(path_to_executable);


            CallStack.Add(State->PC);
            Console.WriteLine($"PPU Thread: PC entry = 0x{State->PC:X} stack = {GPR[1]} r2 = {GPR[2]} tls = {GPR[13]}");
        }

        //Constructor for dynamically created threads -- called by SysPPUThread.cs
        internal PPU(
            string threadName,
            uint entryPointer,
            ulong arg,
            uint stackSize)
        {

            if ((stackSize & 0xF) > 0)
            {
                stackSize = AlignPowerOfTwo(stackSize, 16);
            }

            if (stackSize < 0x1000)
            {
                stackSize = 0x1000;
            }

            SetGCHandle();
            SetPageTable();

            _threadName = threadName;
            _threadID = ThreadManager.GetNewID();

            RSX = PS3Sharp.RSX;
            State->VRSAVE = 0xFFFFFFFF;

            if (entryPointer != 0)
            {
                State->PC = MemoryManager.Read<uint>(entryPointer);
                GPR[2].Value64 = MemoryManager.Read<uint>(entryPointer + 4);
            }

            // allocate stack memory
            MemoryBlock stackArea = MemoryManager.AllocateVirtualMemory(stackSize, 0x1000); //0x1000 bytes aligned
            uint stackBase = (uint)(stackArea.VirtualAddress + stackSize - 16);

            Stack = stackArea.VirtualAddress;
            StackSize = stackSize;

            MemoryBlock tls = MemoryManager.AllocateTLS();

            GPR[0].Value64 = State->PC;
            GPR[1].Value64 = stackBase;
            GPR[3].Value64 = arg;

            // Not sure
            // GPR[7].Value64 = _threadID;
            // GPR[8].Value64 = tls.VirtualAddress;
            // GPR[9].Value64 = tls.Size;
            // GPR[10].Value64 = tls.Size;

            GPR[11].Value64 = entryPointer;
            GPR[12].Value64 = 0x100000;     //malloc pagesize or something

            GPR[13].Value64 = tls.VirtualAddress + 0x7030;   //first 0x30 bytes are reserved, guest code seems to access tls from r13 - ~0x7000 ?

            CallStack.Add(State->PC);
            Console.WriteLine($"PPU Thread: PC entry = 0x{State->PC:X} stack = {GPR[1]} r2 = {GPR[2]} tls = {GPR[13]}");
        }

        internal PPU(string threadName, uint pc, uint threadID, uint stackSize)
        {
            SetGCHandle();
            SetPageTable();

            _threadName = threadName;
            _threadID = threadID;

            MemoryBlock tls = MemoryManager.AllocateTLS();
            MemoryBlock stackArea = MemoryManager.AllocateVirtualMemory(stackSize, 0x1000);
            uint stackBase = (uint)(stackArea.VirtualAddress + stackSize - 32);

            State->PC = pc;
            State->VRSAVE = 0xFFFFFFFF;

            GPR[0].Value64 = pc;
            GPR[1].Value64 = stackBase;
            GPR[13].Value64 = tls.VirtualAddress + 0x7030;

            SetupArgs("test");
            Console.WriteLine($"PPU Thread: PC entry = 0x{State->PC:X} stack = {GPR[1]} r2 = {GPR[2]} tls = {GPR[13]}");
        }


        internal void SetupArgs(string? name)
        {
            //TODO: Do this properly 

            // Push envp NULL
            WriteStack(0);

            //Push argv NULL terminator
            WriteStack(0);

            //Save argv pointer (current SP)
            GPR[4].Value64 = GPR[STACK_REG].Value64;

            //Push argc
            GPR[3].Value64 = 0;
            WriteStack(GPR[3].Value64);

            //Push another 0 for alignment
            WriteStack(0);

            // Allocate 0x70 bytes for main()
            GPR[STACK_REG].Value64 -= 0x70;

            if ((GPR[STACK_REG].Value64 & 0xF) > 0)
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

        internal void SetGCHandle()
        {
            Handle = GCHandle.Alloc(this);
            State->Context = (void*)GCHandle.ToIntPtr(Handle);
        }

        internal void FreeGCHandle()
        {
            Handle.Free();
            State->Context = null;
        }

        internal void SetPageTable()
        {
            State->MemoryBase = MemoryManager.GetGuestMemoryBase();
        }

        internal void WriteStack(ulong value)
        {
            GPR[STACK_REG].Value32 -= sizeof(ulong); // -= 8
            MemoryManager.Write<ulong>(GPR[STACK_REG].Value32, value);
            Console.WriteLine($"STACK[{GPR[STACK_REG].Value32:X8}] = {value:X16}");
        }

        internal uint WritePathArg(string path)
        {
            byte[] chars = Encoding.UTF8.GetBytes(path);

            //Allocate memory for the string + a null character
            MemoryBlock nameBlock = MemoryManager.AllocateVirtualMemory((uint)chars.Length + 1);
            MemoryManager.CopyBufferToVirtualMemory(chars, (uint)nameBlock.VirtualAddress);

            //null terminate string
            MemoryManager.Write<byte>((uint)nameBlock.VirtualAddress + (uint)chars.Length, 0);
            return (uint)nameBlock.VirtualAddress;
        }

        public void StartThread()
        {
            Console.WriteLine($"PPU Thread Started");
            PPUThreadState = PPUThreadStates.Running;
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
        }


        //Temporary
        private Queue<Trace> TraceQueue = new();
        private bool disposedValue;
        const int TraceMaxCount = 200;

        internal struct Trace
        {
            internal uint PC;
            internal PPUInstruction Instruction;
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

        // Dispose Pattern 
        internal void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Handle.Free();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null

                NativeMemory.Free(State);       // Free unmanaged state (allocated in NativeMemory)
                State = null;
                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~PPU()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
