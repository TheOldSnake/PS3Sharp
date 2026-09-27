using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    public sealed class LV2Semaphore
    {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysSemaphoreAttribute : IPS3Struct
        {
            public BigEndian<uint> Protocol;
            public BigEndian<uint> PShared;
            public BigEndian<ulong> IPC_KEY;
            public BigEndian<int> Flags;
            public BigEndian<uint> Padding;
            public InlineData.InlineArray8<byte> Name;
        }

        private readonly SemaphoreSlim _semaphore;
        private readonly SysSemaphoreAttribute _attribute;
        private readonly int _maxCount;
        public uint Handle { get; } = MemoryManager.GetHandle();

        public LV2Semaphore(int initialCount, int maxCount, SysSemaphoreAttribute attribute)
        {
            _semaphore = new SemaphoreSlim(initialCount, maxCount);
            _attribute = attribute;
            _maxCount = maxCount;
        }

        public ulong Release(int count)
        {
            if (count <= 0)
            {
                return CELL_EINVAL;
            }

            try
            {
                _semaphore.Release(count);
                return CELL_OK;

            }
            catch (SemaphoreFullException e)
            {
                // is this the correct error?
                return CELL_ESRCH;
            }
        }

        public ulong TryWait()
        {
            bool ok = _semaphore.Wait(0);
            return ok ? CELL_OK : CELL_EBUSY;
        }

        public ulong Wait(ulong us_timeout)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = _semaphore.Wait(TimeSpan.FromMicroseconds(us_timeout));
            }
            else
            {
                _semaphore.Wait();
                ok = true;
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }
    }
}
