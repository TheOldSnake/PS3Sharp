using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Memory_Manager.MemoryManager;

namespace PS3Sharp.Cell.PPU
{
    public unsafe sealed partial class PPU : IThread, IDisposable
    {
        // PPU thread sealed class — all fields here are single-threaded by default.
        // DO NOT access or mutate from another thread unless using thread-safe mechanisms
        // like Interlocked, locks, or a message queue. Violating this will cause race conditions.
        public const uint STACK_REG = 1;

        public Thread NativeThread;
        public RealitySynthesizer RSX;
        private readonly string _threadName;
        private readonly uint _threadID;
        private int _threadPriority;

        public uint ThreadID => _threadID;
        public int ThreadPriority => _threadPriority;
        public string ThreadName => _threadName;
        public bool IsMainThread => _threadName == "main_thread";

        public readonly uint Stack;
        public readonly uint StackSize;

        public int ExitCode;
        public List<uint> CallStack = new List<uint>();
        public SysUtil.SysUtilCallback[] SysUtilCallbacks = new SysUtil.SysUtilCallback[4];   // Each thread has its own?

        public enum PPUThreadStates
        {
            Running,
            Paused,
            Stopped
        }

        public PPUThreadStates PPUThreadState = PPUThreadStates.Stopped;

        // public ConditionRegister CR;            //Condition Register
        // public uint LR;                         //Link Register
        // public uint TB;                         //Time Base Register -- this is actually global, not per thread
        // public ulong CTR;                       //Count Register
        // public uint VRSAVE = 0xFFFFFFFF;        

        // public uint PC;                         //Program Counter


        // Unmanaged State
        [StructLayout(LayoutKind.Sequential)]
        public struct PPUState
        {
            public uint PC;                     // Program Counter
            public uint LR;                     // Link Register      
            public uint TB;                     // Time Base Register -- is this global, or per thread ?
            public uint VRSAVE;                 // Vector Save/Restore Register, default value = 0xFFFFFFFF
            public ConditionRegister CR;        // Condition Register
            public ulong CTR;                   // Count Register
            public XERRegister XER;             // XER Register
            public InlineData.InlineArray32<PPUGPR> GPR;
            public InlineData.InlineArray32<PPUFPR> FPR;
            public InlineData.InlineArray32<PPUVR> VRF;
            public void* MemoryBase;
            public void* Context;
        }

        public PPUState* State { get; private set; } = (PPUState*)NativeMemory.AllocZeroed((nuint)sizeof(PPUState));
        public Stack<PPUState> SavedStates = new Stack<PPUState>();

        public PPUGPR* GPR => &State->GPR[0];
        public PPUFPR* FPR => &State->FPR[0];
        public PPUVR* VRF => &State->VRF[0];

        public GCHandle Handle { get; private set; }

        // Helpers for syscalls and libraries

        public uint GetPC() => State->PC;
        public uint GetLR() => State->LR;
        public void Return(ulong result) => GPR[ARG0].Value64 = result;


        public T ReadArg<T>(uint index) where T : unmanaged
        {
            if (sizeof(T) > sizeof(PPUGPR))
            {
                throw new ArgumentException($"Can't read type <{nameof(T)}> (too large)");
            }

            byte* base_address = &GPR[index].Value8;
            return Unsafe.Read<T>(base_address);
        }

        public void WriteReg<T>(uint index, T value) where T : unmanaged
        {
            if (sizeof(T) > sizeof(PPUGPR))
            {
                throw new ArgumentException($"Can't read type <{nameof(T)}> (too large)");
            }

            byte* base_address = &GPR[index].Value8;
            Unsafe.Write<T>(base_address, value);
        }

        public PPUGPR[] CaptureGPRState()
        {
            PPUGPR[] gprs = new PPUGPR[32];
            for (int i = 0; i < gprs.Length; i++)
            {
                gprs[i] = GPR[i];
            }

            return gprs;
        }

        public PPUFPR[] CaptureFPRState()
        {
            PPUFPR[] fprs = new PPUFPR[32];
            for (int i = 0; i < fprs.Length; i++)
            {
                fprs[i] = FPR[i];
            }

            return fprs;
        }

        public void SetThreadPriority(int threadPriority)
        {
            _threadPriority = threadPriority;
        }

        //Fixed-Point Exception Register
        //public XERRegister XER;
        public struct XERRegister
        {
            public const int SOBit = 31;
            public const int OVBit = 30;
            public const int CABit = 29;

            public ulong Raw;
            public uint SO => (uint)((Raw >> SOBit) & 1);
            public uint OV => (uint)((Raw >> OVBit) & 1);
            public uint CA => (uint)((Raw >> CABit) & 1);

            public void SetSO(uint v)
            {
                Raw &= ~(1u << SOBit);
                Raw |= (v << SOBit);
            }

            public void SetOV(uint v)
            {
                Raw &= ~(1u << OVBit);
                Raw |= (v << OVBit);
            }

            public void SetCA(uint v)
            {
                Raw &= ~(1u << CABit);
                Raw |= (v << CABit);
            }
        }

        // The bits in the Condition Register are grouped into
        // eight 4-bit fields, named CR Field 0 (CR0), ..., CR Field 7 (CR7)
        public enum CRNumber
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

        public struct ConditionRegister
        {
            public const uint EQ = 0b0010;
            public const uint GT = 0b0100;
            public const uint LT = 0b1000;

            public uint Raw;

            public void WriteCRField(CRNumber cr, uint value)
            {
                value &= 0xF;
                int bit = 28 - ((int)cr * 4);  // CR Fields are reversed
                uint clearMask = (uint)~(0xF << bit);
                Raw &= clearMask;
                Raw |= value << bit;
            }

            public uint GetCRField(CRNumber cr)
            {
                int bit = 28 - ((int)cr * 4);  // CR Fields are reversed
                return (Raw >> bit) & 0xF;
            }

            public void WriteCRBit(uint bit, uint value)
            {       //bit number is in big endian
                int position = (int)(31 - bit);
                Raw &= ((uint)~(1 << position));
                Raw |= (value << position);
            }

            public uint ReadCRBit(uint bit)
            {                 //bit number is in big endian
                int position = (int)(31 - bit);
                return (Raw >> position) & 1;
            }
        }

        public static PPU GetInstance(IntPtr context)
        {
            return (PPU)GCHandle.FromIntPtr(context).Target!;
        }

        //Constructor for main thread -- called by PS3Sharp.cs
        public PPU(
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
        public PPU(
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

        public PPU(string threadName, uint pc, uint threadID, uint stackSize)
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


        public void SetupArgs(string? name)
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

        public void SetGCHandle()
        {
            Handle = GCHandle.Alloc(this);
            State->Context = (void*)GCHandle.ToIntPtr(Handle);
        }

        public void FreeGCHandle()
        {
            Handle.Free();
            State->Context = null;
        }

        public void SetPageTable()
        {
            State->MemoryBase = MemoryManager.GetGuestMemoryBase();
        }

        public void WriteStack(ulong value)
        {
            GPR[STACK_REG].Value32 -= sizeof(ulong); // -= 8
            MemoryManager.Write<ulong>(GPR[STACK_REG].Value32, value);
            Console.WriteLine($"STACK[{GPR[STACK_REG].Value32:X8}] = {value:X16}");
        }

        public uint WritePathArg(string path)
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
        private Queue<Trace> TraceQueue = new Queue<Trace>();
        private bool disposedValue;
        const int TraceMaxCount = 200;

        public struct Trace
        {
            public uint PC;
            public PPUInstruction Instruction;
        }

        public void PrintTraceQueue(bool reverse = false)
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

        public void AddTrace(uint pc, PPUInstruction instruction)
        {
            TraceQueue.Enqueue(new Trace { PC = pc, Instruction = instruction });
            if (TraceQueue.Count > TraceMaxCount)
            {
                TraceQueue.Dequeue();
            }
        }

        // Dispose Pattern 
        public void Dispose(bool disposing)
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
