using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2Mutex
    {
        internal const uint SysSyncRecursive = 0x10;
        internal const uint SysSyncNotRecursive = 0x20;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysMutexAttribute : IPS3Struct
        {
            internal BigEndian<uint> protocol;  // SYS_SYNC_FIFO, SYS_SYNC_PRIORITY or SYS_SYNC_PRIORITY_INHERIT
            internal BigEndian<uint> Recursive; // SYS_SYNC_RECURSIVE or SYS_SYNC_NOT_RECURSIVE
            internal BigEndian<uint> PShared;
            internal BigEndian<uint> Adaptive;
            internal BigEndian<ulong> IPC_Key;
            internal BigEndian<int> Flags;
            internal BigEndian<uint> Pad;
            internal InlineData.InlineArray8<byte> Name;
        }

        private readonly MemoryManager _memoryManager;
        private readonly SysMutexAttribute _attribute;
        private readonly object _mutex = new();

        internal uint Handle { get; }
        internal SysMutexAttribute Attribute => _attribute;
        internal bool IsEntered => Monitor.IsEntered(_mutex);
        private bool IsRecursive => _attribute.protocol == SysSyncRecursive;

        internal LV2Mutex(SysMutexAttribute attribute, MemoryManager memoryManager)
        {
            _attribute = attribute;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal ulong Lock(PPUThread ppu, ulong timeout = 0)
        {
            if (Monitor.IsEntered(_mutex))
            {
                return ReEnter(ppu);
            }
            else
            {
                bool ok;

                if (timeout > 0)
                {
                    ok = Monitor.TryEnter(_mutex, TimeSpan.FromMicroseconds(timeout));
                }
                else
                {
                    ok = InterruptableWait(ppu);
                }

                return ok ? CellOk : CellEBusy;
            }
        }

        private ulong ReEnter(PPUThread ppu)
        {
            // Check if we're allowed to re-enter
            if (!IsRecursive)
            {
                return CellEDeadlk;
            }

            bool ok = InterruptableWait(ppu);
            return CellOk;
        }

        internal ulong TryLock()
        {
            bool success = Monitor.TryEnter(_mutex, 0);
            return success ? CellOk : CellEBusy;
        }

        internal ulong Unlock()
        {
            if (!Monitor.IsEntered(_mutex))
            {
                return CellEPerm;
            }

            Monitor.Exit(_mutex);
            return CellOk;
        }

        private bool InterruptableWait(PPUThread ppu)
        {
            // Return true if the lock is acquired, false if the thread was interrupted
            while (true)
            {
                // Wait for 100ms
                bool ok = Monitor.TryEnter(_mutex, TimeSpan.FromMilliseconds(100));

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
