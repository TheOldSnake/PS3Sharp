using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using PS3Sharp.Common;
using PS3Sharp.Cell;

using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2 {
    public class LV2Mutex {
        public const uint SYS_SYNC_RECURSIVE = 0x10;
        public const uint SYS_SYNC_NOT_RECURSIVE = 0x20;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysMutexAttribute : IPS3Struct {
            public BigEndian<uint> protocol;  // SYS_SYNC_FIFO, SYS_SYNC_PRIORITY or SYS_SYNC_PRIORITY_INHERIT
            public BigEndian<uint> Recursive; // SYS_SYNC_RECURSIVE or SYS_SYNC_NOT_RECURSIVE
            public BigEndian<uint> PShared;
            public BigEndian<uint> Adaptive;
            public BigEndian<ulong> IPC_Key;
            public BigEndian<int> Flags;
            public BigEndian<uint> Pad;
            public InlineData.InlineArray8<byte> Name;
        }

        private readonly SysMutexAttribute _attribute;
        private readonly object _mutex = new object();
        public uint Handle { get; } = MemoryManager.GetHandle();

        public SysMutexAttribute Attribute => _attribute;
        private bool _isRecursive => _attribute.protocol == SYS_SYNC_RECURSIVE;
        public bool IsEntered => Monitor.IsEntered(_mutex);

        public LV2Mutex(SysMutexAttribute attribute) {
            _attribute = attribute;
        }

        public ulong Lock(ulong timeout = 0) {         
            if (Monitor.IsEntered(_mutex)) {
                return ReEnter();
            } else {
                bool ok;

                if (timeout > 0) {
                    ok = Monitor.TryEnter(_mutex, TimeSpan.FromMicroseconds(timeout));
                } else {
                    Monitor.Enter(_mutex);
                    ok = true;
                }

               return ok ? CELL_OK : CELL_EBUSY;
            }
        }

        private ulong ReEnter() {
            // Check if we're allowed to re-enter
            if (!_isRecursive) {
                return CELL_EDEADLK;
            }

            Monitor.Enter(_mutex);
            return CELL_OK;
        }

        public ulong TryLock() {
            bool success = Monitor.TryEnter(_mutex, 0);
            return success ? CELL_OK : CELL_EBUSY;
        }

        public ulong Unlock() {
            if (!Monitor.IsEntered(_mutex)) {
                return CELL_EPERM;
            }

            Monitor.Exit(_mutex);
            return CELL_OK;
        }
    }
}
