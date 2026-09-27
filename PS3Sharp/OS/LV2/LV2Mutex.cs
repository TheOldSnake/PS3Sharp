using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2Mutex
    {
        internal const uint SYS_SYNC_RECURSIVE = 0x10;
        internal const uint SYS_SYNC_NOT_RECURSIVE = 0x20;

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

        private readonly SysMutexAttribute _attribute;
        private readonly object _mutex = new();
        internal uint Handle { get; } = MemoryManager.GetHandle();

        internal SysMutexAttribute Attribute => _attribute;
        private bool _isRecursive => _attribute.protocol == SYS_SYNC_RECURSIVE;
        internal bool IsEntered => Monitor.IsEntered(_mutex);

        internal LV2Mutex(SysMutexAttribute attribute)
        {
            _attribute = attribute;
        }

        internal ulong Lock(ulong timeout = 0)
        {
            if (Monitor.IsEntered(_mutex))
            {
                return ReEnter();
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
                    Monitor.Enter(_mutex);
                    ok = true;
                }

                return ok ? CELL_OK : CELL_EBUSY;
            }
        }

        private ulong ReEnter()
        {
            // Check if we're allowed to re-enter
            if (!_isRecursive)
            {
                return CELL_EDEADLK;
            }

            Monitor.Enter(_mutex);
            return CELL_OK;
        }

        internal ulong TryLock()
        {
            bool success = Monitor.TryEnter(_mutex, 0);
            return success ? CELL_OK : CELL_EBUSY;
        }

        internal ulong Unlock()
        {
            if (!Monitor.IsEntered(_mutex))
            {
                return CELL_EPERM;
            }

            Monitor.Exit(_mutex);
            return CELL_OK;
        }
    }
}
