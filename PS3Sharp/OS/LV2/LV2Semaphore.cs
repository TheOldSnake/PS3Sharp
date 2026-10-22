using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
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

        private readonly MemoryManager _memoryManager;
        private readonly SemaphoreSlim _semaphore;
        private readonly SysSemaphoreAttribute _attribute;
        private readonly int _maxCount;

        internal uint Handle { get; }

        internal LV2Semaphore(int initialCount, int maxCount, SysSemaphoreAttribute attribute, MemoryManager memoryManager)
        {
            _semaphore = new SemaphoreSlim(initialCount, maxCount);
            _attribute = attribute;
            _maxCount = maxCount;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal ulong Release(int count)
        {
            if (count <= 0)
            {
                return CellEInval;
            }

            try
            {
                _semaphore.Release(count);
                return CellOk;

            }
            catch (SemaphoreFullException e)
            {
                // is this the correct error?
                return CellESrch;
            }
        }

        internal ulong TryWait()
        {
            bool ok = _semaphore.Wait(0);
            return ok ? CellOk : CellEBusy;
        }

        internal ulong Wait(PPUThread ppu, ulong us_timeout)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = _semaphore.Wait(TimeSpan.FromMicroseconds(us_timeout));
            }
            else
            {
                ok = InterruptableWait(ppu);
            }

            return ok ? CellOk : CellETimedout;
        }

        private bool InterruptableWait(PPUThread ppu)
        {
            while (true)
            {
                // Wait for 100ms
                bool ok = _semaphore.Wait(TimeSpan.FromMilliseconds(100));

                if (ok)
                {
                    return true;
                }

                // Check if the thread is interrupted by shutdown signal and must return immediately
                if (ppu.ShouldExit)
                {
                    return false;
                }
            }
        }
    }
}
