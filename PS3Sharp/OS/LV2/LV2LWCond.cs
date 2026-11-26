using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS.Modules;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    // This implementation is under the assumption that this is very similar to LV2Cond,
    // But it uses LWMutex instead of Mutex
    internal sealed class LV2LwCond
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct LWCondAttribute : IPS3Struct
        {
            internal InlineData.InlineArray8<byte> Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LwCond : IPS3Struct
        {
            internal BigEndianGuestPtr<LV2LwMutex.LwMutex> LwMutex;
            internal BigEndian<uint> Id;
        }

        private readonly MemoryManager _memoryManager;
        private readonly SysLwMutex _sysLwMutex;
        private readonly ConcurrentDictionary<uint, LV2CondWaiter> _threadCondWaiters = [];
        private readonly List<LV2CondWaiter> _waiters = [];
        private readonly GuestPtr<LwCond> _instancePtr;
        private readonly GuestPtr<LV2LwMutex.LwMutex> _mutexPtr;
        private readonly LWCondAttribute _attribute;

        internal uint Handle { get; }

        internal LV2LwCond(GuestPtr<LwCond> condPtr, GuestPtr<LV2LwMutex.LwMutex> mutexPtr, LWCondAttribute attribute, SysLwMutex sysLwMutexHandler, MemoryManager memoryManager)
        {
            _instancePtr = condPtr;
            _mutexPtr = mutexPtr;
            _sysLwMutex = sysLwMutexHandler;
            _attribute = attribute;
            _memoryManager = memoryManager;

            Handle = _memoryManager.GetHandle();
        }

        internal bool TryGetLWMutex([NotNullWhen(true)] out LV2LwMutex? lv2lwmutex)
        {
            LwCond cond = _memoryManager.Read<LwCond>(_instancePtr);
            GuestPtr<LV2LwMutex.LwMutex> lwmutexPtr = cond.LwMutex;
            LV2LwMutex.LwMutex lwmutex = _memoryManager.Read<LV2LwMutex.LwMutex>(lwmutexPtr);
            uint id = lwmutex.SleepQueue;

            return _sysLwMutex.GetLWMutex(id, out lv2lwmutex);
        }

        internal ulong Wait(PPUThread ppu, ulong us_timeout = 0)
        {
            if (!TryGetLWMutex(out LV2LwMutex? lv2lwmutex))
            {
                return CellESrch;
            }

            if (!lv2lwmutex.IsEntered())
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
            lv2lwmutex.Unlock();

            bool ok;
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
                lv2lwmutex.Lock(ppu, 0);

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

        private static bool InterruptableWait(PPUThread ppu, LV2CondWaiter waiter)
        {
            // Return true if the event is set, false if the thread was interrupted
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

        private LV2CondWaiter GetThreadCondWaiter(uint threadID)
        {
            // Look up the condWaiter instance in the dictionary
            if (_threadCondWaiters.TryGetValue(threadID, out LV2CondWaiter? waiter))
            {
                return waiter;
            }

            // If it does not exist create one and add it
            LV2CondWaiter condWaiter = new(threadID);
            bool success = _threadCondWaiters.TryAdd(threadID, condWaiter);

            if (!success)
            {
                throw new Exception($"[LV2LwCond] Could not add a CondWaiter instance to the dictionary");
            }

            return condWaiter;
        }
    }
}
