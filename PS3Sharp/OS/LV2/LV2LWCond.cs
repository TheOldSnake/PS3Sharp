using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.Modules;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{

    // This implementation is under the assumption that this is very similar to LV2Cond,
    // But it uses LWMutex instead of Mutex

    // This is incomplete 
    internal sealed class LV2LWCond
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SysLWCondAttribute : IPS3Struct
        {
            internal InlineData.InlineArray8<byte> Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysLWCond : IPS3Struct
        {
            internal BigEndianGuestPtr<LV2LWMutex.LWMutex> LWMutex;   // why is this needed ?
            internal BigEndian<uint> LWCondQueue;
        }

        private readonly MemoryManager _memoryManager;
        private readonly SysLwMutex _sysLwMutex;
        private readonly ConcurrentDictionary<uint, LV2CondWaiter> _threadCondWaiters = [];
        private readonly List<LV2CondWaiter> _waiters = new();
        private readonly GuestPtr<SysLWCond> _instancePtr;
        private readonly SysLWCondAttribute _attribute;

        internal LV2LWCond(GuestPtr<SysLWCond> ptr, SysLWCondAttribute attribute, MemoryManager memoryManager, SysLwMutex sysLwMutex)
        {
            _memoryManager = memoryManager;
            _sysLwMutex = sysLwMutex;
            _instancePtr = ptr;
            _attribute = attribute;
        }

        internal LV2LWMutex GetLWMutex()
        {
            // this is assuming that the mutex is stored here
            // but we already get a mutex ID in like every syscall of this sealed class.. ?

            uint id;
            unsafe
            {
                SysLWCond* cond = _memoryManager.GetPtr(_instancePtr);
                GuestPtr<LV2LWMutex.LWMutex> lwmutexPtr = cond->LWMutex;
                LV2LWMutex.LWMutex* lwmutex = _memoryManager.GetPtr(lwmutexPtr);
                id = lwmutex->SleepQueue;
            }

            if (_sysLwMutex.GetLWMutex(id, out LV2LWMutex? mutex))
            {
                return mutex;

            }
            else
            {
                throw new Exception($"[SysLWCond] Can't fine LWMutex");
            }
        }

        internal ulong Wait(PPUThread ppu, ulong us_timeout = 0)
        {
            LV2LWMutex lwmutex = GetLWMutex();

            if (!lwmutex.IsEntered())
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
            lwmutex.Unlock();

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
                lwmutex.Lock(ppu, 0);

                // Remove the waiter from the list
                lock (_waiters)
                {
                    _waiters.Remove(waiter);
                }
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;


        }

        internal ulong Signal()
        {
            lock (_waiters)
            {
                LV2CondWaiter? waiter = _waiters.FirstOrDefault();
                waiter?.Event.Set();
            }

            return CELL_OK;
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

            return CELL_OK;
        }

        private bool InterruptableWait(PPUThread ppu, LV2CondWaiter waiter)
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
            if (_threadCondWaiters.TryGetValue(threadID, out LV2CondWaiter waiter))
            {
                return waiter;
            }

            // If it does not exist create one and add it
            LV2CondWaiter condWaiter = new(threadID);
            bool success = _threadCondWaiters.TryAdd(threadID, condWaiter);

            if (!success)
            {
                throw new Exception($"[LV2Cond] Could not add a CondWaiter instance to the dictionary");
            }

            return condWaiter;
        }
    }
}
