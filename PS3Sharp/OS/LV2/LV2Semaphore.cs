using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2Semaphore
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSemaphoreAttribute : IPS3Struct
        {
            internal BigEndian<uint> Protocol;
            internal BigEndian<uint> PShared;
            internal BigEndian<ulong> IPC_KEY;
            internal BigEndian<int> Flags;
            internal BigEndian<uint> Padding;
            internal InlineData.InlineArray8<byte> Name;
        }

        private readonly SemaphoreSlim _semaphore;
        private readonly SysSemaphoreAttribute _attribute;
        private readonly int _maxCount;
        internal uint Handle { get; } = MemoryManager.GetHandle();

        internal LV2Semaphore(int initialCount, int maxCount, SysSemaphoreAttribute attribute)
        {
            _semaphore = new SemaphoreSlim(initialCount, maxCount);
            _attribute = attribute;
            _maxCount = maxCount;
        }

        internal ulong Release(int count)
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

        internal ulong TryWait()
        {
            bool ok = _semaphore.Wait(0);
            return ok ? CELL_OK : CELL_EBUSY;
        }

        internal ulong Wait(ulong us_timeout)
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
