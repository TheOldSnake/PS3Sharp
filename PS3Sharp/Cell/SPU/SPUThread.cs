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

        private readonly uint[] _ports = new uint[64];

        internal string Name { get; } = string.Empty;
        internal uint ID { get; } = 0xFFFFFFFF;
        internal uint GroupID { get; } = 0xFFFFFFFF;
        internal SysSpu.SysSPUImage Image { get; }
        internal SysSpu.SysSPUThreadArgument Argument { get; }

        internal Lock Lock { get; } = new();

        internal static readonly int GPRByteSize = 128 * sizeof(SPURegister);
        internal static readonly int LocalStorageSize = 256 * 1024;

        // Allocate GPR
        internal NativeMemoryHandle? GPRHandle { get; private set; } = new NativeMemoryHandle((nuint)GPRByteSize);
        internal SPURegister* GPR => (SPURegister*)GPRHandle!.DangerousGetHandle();

        // Allocate local storage
        internal NativeMemoryHandle? LSHandle { get; private set; } = new NativeMemoryHandle((nuint)LocalStorageSize);
        internal byte* LS => (byte*)LSHandle!.DangerousGetHandle();

        internal uint PC { get; private set; } = 0;

        internal ThreadStatus Status { get; private set; } = ThreadStatus.Stopped;
        private readonly MemoryManager _memoryManager;

        // TODO: Move to thread manager
        internal SPUThread(string name, uint id, uint groupID, SysSpu.SysSPUImage img, SysSpu.SysSPUThreadArgument arg, MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
            Name = name;
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
            NativeMemory.Clear(GPR, (nuint)GPRByteSize);
            NativeMemory.Clear(LS, (nuint)LocalStorageSize);
            PC = 0;
            GPR[1].UInts[3] = 0x3FFF0;   // Default stack pointer
            Status = ThreadStatus.Stopped;
        }

        internal void Start()
        {
            LoadImage();
            SetArgs();

            Status = ThreadStatus.Running;

            //...
        }

        internal void LoadImage()
        {
            GuestPtr<uint> entry = Image.Entry;
            uint count = (uint)Image.SegmentsCount;

            if (entry.IsNullPtr)
            {
                throw new Exception($"[SPU] Image null pointer");
            }

            ReadOnlySpan<SysSpu.SysSPUSegment> segments = _memoryManager.ReadArray<SysSpu.SysSPUSegment>(entry, (int)count);

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

            // Copy from src to SPU local storage buffer
            Span<byte> dest = new(LS + ls, size);

            switch (segment.Type)
            {
                case SysSpu.SysSpuSegmentTypeCopy:
                    _memoryManager.ReadBufferFromVirtualMemory(src, dest);
                    break;

                case SysSpu.SysSpuSegmentTypeFill:
                    dest.Fill((byte)src);
                    break;
            }
        }

        internal void SetArgs()
        {
            GPR[3].ULongs[1] = Argument.Arg1;
            GPR[4].ULongs[1] = Argument.Arg2;
            GPR[5].ULongs[1] = Argument.Arg3;
            GPR[6].ULongs[1] = Argument.Arg4;
        }

        public void Dispose()
        {
            GPRHandle?.Dispose();
            GPRHandle = null;

            LSHandle?.Dispose();
            LSHandle = null;
        }
    }
}
