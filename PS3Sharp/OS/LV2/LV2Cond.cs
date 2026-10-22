using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2Cond
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SysCondAttribute : IPS3Struct
        {
            internal BigEndian<uint> PShared;
            internal BigEndian<int> Flags;
            internal BigEndian<ulong> IPC_Key;
            internal InlineData.InlineArray8<byte> Name;
        }

        // Static mapping of ppu -> LV2CondWaiter
        private static readonly ConcurrentDictionary<uint, LV2CondWaiter> _ppuThreadsCondWaiters = []; 

        private readonly MemoryManager _memoryManager;
        private readonly SysCondAttribute _attribute;
        private readonly LV2Mutex _mutex;
        private readonly List<LV2CondWaiter> _waiters = []; // Current waiters list

        internal uint Handle { get; }

        internal LV2Cond(SysCondAttribute attribute, LV2Mutex mutex, MemoryManager memoryManager)
        {
            _attribute = attribute;
            _mutex = mutex;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal ulong Wait(PPUThread ppu, ulong us_timeout = 0)
        {
            if (!_mutex.IsEntered)
            {
                return 0xFFFFFFFFFFFFFFFF; // -1
            }

            // Add the thread to the wait list
            LV2CondWaiter waiter = GetThreadCondWaiter(ppu.ThreadID);

            lock (_waiters)
            {
                _waiters.Add(waiter);
            }

            // Unlock the mutex
            _mutex.Unlock();

            bool ok = false;
            try
            {
                // Wait for a signal
                if (us_timeout > 0)
                {
                    ok = waiter.Event.Wait(TimeSpan.FromMicroseconds(us_timeout));
                }
                else
                {
                    ok = InterruptableWait(ppu, waiter);
                }
            }
            finally
            {
                // Reset the event
                waiter.Event.Reset();

                // Re aquire the lock
                _mutex.Lock(ppu);

                // Remove the waiter from the list
                lock (_waiters)
                {
                    _waiters.Remove(waiter);
                }
            }

            return ok ? CellOk : CellETimedout;
        }

        internal ulong Signal()
        {
            lock (_waiters)
            {
                LV2CondWaiter? waiter = _waiters.FirstOrDefault();
                waiter?.Event.Set();
            }

            return CellOk;
        }

        internal ulong SignalAll()
        {
            lock (_waiters)
            {
                foreach (LV2CondWaiter? waiter in _waiters)
                {
                    waiter.Event.Set();
                }
            }

            return CellOk;
        }

        internal ulong SignalTo(uint waiterID)
        {
            LV2CondWaiter? waiter;

            lock (_waiters)
            {
                waiter = _waiters.FirstOrDefault(w => w.WaiterTheadID == waiterID);
            }

            waiter?.Event.Set();
            return CellOk;
        }

        private bool InterruptableWait(PPUThread ppu, LV2CondWaiter waiter)
        {
            // Return true if the event was signaled, false if the thread was interrupted
            while (true)
            {
                // Wait for 100ms
                bool ok = waiter.Event.Wait(TimeSpan.FromMilliseconds(100));

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

        private static LV2CondWaiter GetThreadCondWaiter(uint threadID)
        {
            // Look up the condWaiter instance in the static dictionary
            if (_ppuThreadsCondWaiters.TryGetValue(threadID, out LV2CondWaiter? waiter))
            {
                return waiter;
            }

            // If it does not exist create the waiter object and add it
            LV2CondWaiter condWaiter = new(threadID);
            bool success = _ppuThreadsCondWaiters.TryAdd(threadID, condWaiter);

            if (!success)
            {
                throw new Exception($"[LV2Cond] Could not add a CondWaiter instance to the dictionary");
            }

            return condWaiter;
        }
    }
}