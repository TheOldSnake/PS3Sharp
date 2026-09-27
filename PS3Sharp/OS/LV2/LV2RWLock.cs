using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2 {
    public sealed class LV2RWLock {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysRWLockAttribute : IPS3Struct {
            public BigEndian<uint> Protocol; 
            public BigEndian<uint> PShared;
            public BigEndian<ulong> IPC_Key;
            public BigEndian<int> Flags;
            public BigEndian<uint> Pad;
            public InlineData.InlineArray8<byte> Name;
        }

        private readonly object _rlock = new object(); // Read Lock
        private readonly object _wlock = new object(); // Write Lock

        private readonly SysRWLockAttribute _attribute;
        public uint Handle { get; } = MemoryManager.GetHandle();

        public LV2RWLock(SysRWLockAttribute attribute) {
            _attribute = attribute;
        }

        public ulong RLock(ulong us_timeout) {
            bool ok;

            if (us_timeout > 0) {
                ok = Monitor.TryEnter(_rlock, TimeSpan.FromMicroseconds(us_timeout));

            } else {
                Monitor.Enter(_rlock);
                ok = true;
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        public ulong TryRLock() {
            bool ok = Monitor.TryEnter(_rlock, 0);
            return ok ? CELL_OK : CELL_EBUSY;
        }

        public ulong RUnlock() {
            if (!Monitor.IsEntered(_rlock)) {
                return CELL_EINVAL;
            }

            Monitor.Exit(_rlock);
            return CELL_OK;
        }

        public ulong WLock(ulong us_timeout) {
            bool ok;

            if (us_timeout > 0) {
                ok = Monitor.TryEnter(_wlock, TimeSpan.FromMicroseconds(us_timeout));

            } else {
                Monitor.Enter(_wlock);
                ok = true;
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        public ulong TryWLock() {
            bool ok = Monitor.TryEnter(_wlock, 0);
            return ok ? CELL_OK : CELL_EBUSY;
        }

        public ulong WUnlock() {
            if (!Monitor.IsEntered(_wlock)) {
                return CELL_EINVAL;
            }

            Monitor.Exit(_wlock);
            return CELL_OK;
        }
    }
}
