using PS3Sharp.Cell.SPU.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.SPU
{
    internal unsafe sealed class SPUThread : IDisposable
    {
        internal enum ThreadStatus
        {
            Stopped = 0,
            Running = 1,
        }

        // Unmanaged state
        [StructLayout(LayoutKind.Sequential)]
        internal struct SPUState
        {
            internal uint PC;
            internal SPURegisterArray GPR;
        }

        internal Thread NativeThread { get; }
        private readonly uint[] _ports = new uint[64];

        internal string ThreadName { get; } = string.Empty;
        internal uint ID { get; } = 0xFFFFFFFF;
        internal uint GroupID { get; } = 0xFFFFFFFF;
        internal SysSpu.SysSPUImage Image { get; }
        internal SysSpu.SysSPUThreadArgument Argument { get; }

        internal Lock Lock { get; } = new();


        // Allocate State
        internal static readonly int StateSize = sizeof(SPUState);
        internal NativeMemoryHandle? StateHandle { get; private set; } = new NativeMemoryHandle((nuint)StateSize);
        internal SPUState* State => (SPUState*)StateHandle!.DangerousGetHandle();
        internal SPURegister* GPR => &State->GPR[0];

        // Allocate local storage
        internal static readonly int LocalStorageSize = 256 * 1024;
        internal NativeMemoryHandle? LSHandle { get; private set; } = new NativeMemoryHandle((nuint)LocalStorageSize);
        internal byte* LS => (byte*)LSHandle!.DangerousGetHandle();

        internal ThreadStatus Status { get; private set; } = ThreadStatus.Stopped;
        internal MemoryManager MemoryManager { get; }

        // TODO: Move to thread manager
        internal SPUThread(string name, uint id, uint groupID, SysSpu.SysSPUImage img, SysSpu.SysSPUThreadArgument arg, MemoryManager memoryManager)
        {
            NativeThread = new Thread(RunSPU)
            {
                Name = $"Native SPU Thread: {ThreadName}"
            };

            MemoryManager = memoryManager;
            ThreadName = name;
            ID = id;
            GroupID = groupID;
            Image = img;
            Argument = arg;

            for (int i = 0; i < _ports.Length; i++)
            {
                _ports[i] = 0xFFFFFFFF;
            }
        }

        internal bool PortIsFree(int port) => _ports[port] == 0xFFFFFFFF;

        internal void SetPort(int port, uint id)
        {
            if (_ports[port] == 0xFFFFFFFF)
            {
                _ports[port] = id;
            }
            else
            {
                throw new Exception($"[SPU] Port is not free");
            }
        }

        internal uint GetPort(int port)
        {
            return _ports[port];
        }

        internal void Reset()
        {
            unsafe
            {
                NativeMemory.Clear(State, (nuint)StateSize);
                NativeMemory.Clear(LS, (nuint)LocalStorageSize);
                GPR[1].UInts[3] = 0x3FFF0;   // Default stack pointer
            }

            Status = ThreadStatus.Stopped;
        }

        internal void Start()
        {
            LoadImage();
            SetArgs();

            Status = ThreadStatus.Running;
            NativeThread.Start();
        }

        internal void LoadImage()
        {
            if (Image.Entry == 0)
            {
                throw new Exception($"[SPU] Image null pointer");
            }

            BigEndianGuestPtr<SysSpu.SysSPUSegment> segmentsPtr = Image.SegmentsPtr;
            uint count = (uint)Image.SegmentsCount;

            ReadOnlySpan<SysSpu.SysSPUSegment> segments = MemoryManager.ReadArray<SysSpu.SysSPUSegment>(segmentsPtr, (int)count);

            for (int i = 0; i < segments.Length; i++)
            {
                LoadSegment(segments[i]);
            }
        }

        private void LoadSegment(SysSpu.SysSPUSegment segment)
        {
            int size = segment.Size;
            uint src = segment.Src;
            uint ls = segment.LSAddress;

            Console.WriteLine($"[SPU] Loading SPU segment at 0x{src:X8} to local address 0x{ls:X8} type = {segment.Type}");

            unsafe
            {
                // Copy from src to SPU local storage buffer
                Span<byte> dest = new(LS + ls, size);

                switch (segment.Type)
                {
                    case SysSpu.SysSpuSegmentTypeCopy:
                        MemoryManager.ReadBufferFromVirtualMemory(src, dest);
                        break;

                    case SysSpu.SysSpuSegmentTypeFill:
                        dest.Fill((byte)src);
                        break;
                }
            }
        }

        internal void SetArgs()
        {
            unsafe
            {
                GPR[3].ULongs[1] = Argument.Arg1;
                GPR[4].ULongs[1] = Argument.Arg2;
                GPR[5].ULongs[1] = Argument.Arg3;
                GPR[6].ULongs[1] = Argument.Arg4;
            }
        }

        internal void RunSPU()
        {
            // TODO: Add shutdown path / cancellation source through thread manager

            try
            {
                while (true)
                {
                    SPUInterpreter.Step(this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        public void Dispose()
        {
            StateHandle?.Dispose();
            StateHandle = null;

            LSHandle?.Dispose();
            LSHandle = null;
        }
    }
}
