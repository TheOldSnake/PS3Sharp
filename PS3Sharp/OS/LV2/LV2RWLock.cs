using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
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

        private readonly MemoryManager _memoryManager;
        private readonly object _rlock = new(); // Read Lock
        private readonly object _wlock = new(); // Write Lock
        private readonly SysRWLockAttribute _attribute;

        internal uint Handle { get; }

        internal LV2RWLock(SysRWLockAttribute attribute, MemoryManager memoryManager)
        {
            _attribute = attribute;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal ulong RLock(PPUThread ppu, ulong us_timeout)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = Monitor.TryEnter(_rlock, TimeSpan.FromMicroseconds(us_timeout));
            }
            else
            {
                ok = InterruptableWait(ppu, _rlock);
            }

            return ok ? CellOk : CellETimedout;
        }

        internal ulong TryRLock()
        {
            bool ok = Monitor.TryEnter(_rlock, 0);
            return ok ? CellOk : CellEBusy;
        }

        internal ulong RUnlock()
        {
            if (!Monitor.IsEntered(_rlock))
            {
                return CellEInval;
            }

            Monitor.Exit(_rlock);
            return CellOk;
        }

        internal ulong WLock(PPUThread ppu, ulong us_timeout)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = Monitor.TryEnter(_wlock, TimeSpan.FromMicroseconds(us_timeout));
            }
            else
            {
                ok = InterruptableWait(ppu, _wlock);
            }

            return ok ? CellOk : CellETimedout;
        }

        internal ulong TryWLock()
        {
            bool ok = Monitor.TryEnter(_wlock, 0);
            return ok ? CellOk : CellEBusy;
        }

        internal ulong WUnlock()
        {
            if (!Monitor.IsEntered(_wlock))
            {
                return CellEInval;
            }

            Monitor.Exit(_wlock);
            return CellOk;
        }

        private bool InterruptableWait(PPUThread ppu, object lockObject)
        {
            // Return true if the lock is acquired, false if the thread was interrupted
            while (true)
            {
                // Wait for 100ms
                bool ok = Monitor.TryEnter(lockObject, TimeSpan.FromMilliseconds(100));

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
