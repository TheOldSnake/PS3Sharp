using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2RWLock
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysRWLockAttribute : IPS3Struct
        {
            internal BigEndian<uint> Protocol;
            internal BigEndian<uint> PShared;
            internal BigEndian<ulong> IPC_Key;
            internal BigEndian<int> Flags;
            internal BigEndian<uint> Pad;
            internal InlineData.InlineArray8<byte> Name;
        }

        private readonly object _rlock = new(); // Read Lock
        private readonly object _wlock = new(); // Write Lock

        private readonly SysRWLockAttribute _attribute;
        internal uint Handle { get; } = MemoryManager.GetHandle();

        internal LV2RWLock(SysRWLockAttribute attribute)
        {
            _attribute = attribute;
        }

        internal ulong RLock(ulong us_timeout)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = Monitor.TryEnter(_rlock, TimeSpan.FromMicroseconds(us_timeout));

            }
            else
            {
                Monitor.Enter(_rlock);
                ok = true;
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        internal ulong TryRLock()
        {
            bool ok = Monitor.TryEnter(_rlock, 0);
            return ok ? CELL_OK : CELL_EBUSY;
        }

        internal ulong RUnlock()
        {
            if (!Monitor.IsEntered(_rlock))
            {
                return CELL_EINVAL;
            }

            Monitor.Exit(_rlock);
            return CELL_OK;
        }

        internal ulong WLock(ulong us_timeout)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = Monitor.TryEnter(_wlock, TimeSpan.FromMicroseconds(us_timeout));

            }
            else
            {
                Monitor.Enter(_wlock);
                ok = true;
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        internal ulong TryWLock()
        {
            bool ok = Monitor.TryEnter(_wlock, 0);
            return ok ? CELL_OK : CELL_EBUSY;
        }

        internal ulong WUnlock()
        {
            if (!Monitor.IsEntered(_wlock))
            {
                return CELL_EINVAL;
            }

            Monitor.Exit(_wlock);
            return CELL_OK;
        }
    }
}
