using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.SPU
{
    public unsafe sealed class SPU : IDisposable
    {
        private bool disposedValue;

        public string Name { get; }
        public uint ID { get; } = 0xFFFFFFFF;
        public uint GroupID { get; } = 0xFFFFFFFF;
        public SysSPU.SysSPUImage Image { get; }
        public SysSPU.SysSPUThreadArgument Argument { get; }

        private uint[] _ports { get; } = new uint[64];
        private object _lock { get; } = new object();

        public bool PortIsFree(int port) => _ports[port] == 0xFFFFFFFF;

        public static readonly int GPRByteSize = 128 * sizeof(SPURegister);
        public static readonly int LocalStorageSize = 256 * 1024;

        // Allocate GPR
        public SPURegister* GPR { get; private set; } = (SPURegister*)NativeMemory.AllocZeroed((nuint)GPRByteSize);

        // Allocate local storage
        public byte* LS { get; private set; } = (byte*)NativeMemory.AllocZeroed((nuint)LocalStorageSize);

        public uint PC { get; private set; } = 0;

        public ThreadStatus Status { get; private set; } = ThreadStatus.Stopped;

        public enum ThreadStatus
        {
            Stopped = 0,
            Running = 1,
        }

        public SPU(string name, uint id, uint groupID, SysSPU.SysSPUImage img, SysSPU.SysSPUThreadArgument arg)
        {
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

        public void SetPort(int port, uint id)
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

        public void Reset()
        {
            NativeMemory.Clear(GPR, (nuint)GPRByteSize);
            NativeMemory.Clear(LS, (nuint)LocalStorageSize);
            PC = 0;
            GPR[1].UInts[3] = 0x3FFF0;   // Default stack pointer
            Status = ThreadStatus.Stopped;
        }

        public void Start()
        {
            LoadImage();
            SetArgs();

            Status = ThreadStatus.Running;

            //...
        }

        public void LoadImage()
        {
            GuestPtr<uint> entry = Image.Entry;
            uint count = (uint)Image.SegmentsCount;

            if (entry.IsNullPtr)
            {
                throw new Exception($"[SPU] Image null pointer");
            }

            unsafe
            {
                ReadOnlySpan<SysSPU.SysSPUSegment> segments = MemoryManager.CreateSpan<SysSPU.SysSPUSegment>(entry, (int)count);

                for (int i = 0; i < segments.Length; i++)
                {
                    LoadSegment(segments[i]);
                }
            }
        }

        private void LoadSegment(SysSPU.SysSPUSegment segment)
        {
            int size = segment.Size;
            uint src_value = segment.Src;
            uint ls_offset = segment.LSAddress;

            Span<byte> dest = new(LS + ls_offset, size);

            switch (segment.Type)
            {
                case SysSPU.SYS_SPU_SEGMENT_TYPE_COPY:
                    MemoryManager.ReadBufferFromVirtualMemory(src_value, dest);
                    break;

                case SysSPU.SYS_SPU_SEGMENT_TYPE_FILL:
                    dest.Fill((byte)src_value);
                    break;
            }
        }

        public void SetArgs()
        {
            GPR[3].ULongs[1] = Argument.Arg1;
            GPR[4].ULongs[1] = Argument.Arg2;
            GPR[5].ULongs[1] = Argument.Arg3;
            GPR[6].ULongs[1] = Argument.Arg4;
        }

        public void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                NativeMemory.Free(GPR);
                NativeMemory.Free(LS);
                GPR = null;
                LS = null;

                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~SPU()
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
