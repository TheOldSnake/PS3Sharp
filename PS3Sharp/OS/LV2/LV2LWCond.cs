using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.OS.Modules;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{

    // This implementation is under the assumption that this is very similar to LV2Cond,
    // But it uses LWMutex instead of Mutex

    // This is incomplete 
    public sealed class LV2LWCond
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SysLWCondAttribute : IPS3Struct
        {
            public InlineData.InlineArray8<byte> Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SysLWCond : IPS3Struct
        {
            public BigEndianGuestPtr<LV2LWMutex.LWMutex> LWMutex;   // why is this needed ?
            public BigEndian<uint> LWCondQueue;
        }

        private static readonly ConcurrentDictionary<uint, LV2CondWaiter> _threadCondWaiters = [];
        private readonly List<LV2CondWaiter> Waiters = new();

        private readonly GuestPtr<SysLWCond> _instancePtr;
        private readonly SysLWCondAttribute _attribute;

        public LV2LWCond(GuestPtr<SysLWCond> ptr, SysLWCondAttribute attribute)
        {
            _instancePtr = ptr;
            _attribute = attribute;
        }

        public LV2LWMutex GetLWMutex()
        {
            // this is assuming that the mutex is stored here
            // but we already get a mutex ID in like every syscall of this sealed class.. ?

            uint id;
            unsafe
            {
                SysLWCond* cond = _instancePtr.AsNativePtr();
                GuestPtr<LV2LWMutex.LWMutex> lwmutexPtr = cond->LWMutex;
                LV2LWMutex.LWMutex* lwmutex = lwmutexPtr.AsNativePtr();
                id = lwmutex->SleepQueue;
            }

            if (SysLwMutex.GetLWMutex(id, out LV2LWMutex? mutex))
            {
                return mutex;

            }
            else
            {
                throw new Exception($"[SysLWCond] Can't fine LWMutex");
            }
        }

        public ulong Wait(uint callerThreadID, ulong us_timeout = 0)
        {
            LV2LWMutex lwmutex = GetLWMutex();

            if (!lwmutex.IsEntered())
            {
                return 0xFFFFFFFFFFFFFFFF; // -1
            }

            // Add the thread to the wait list
            LV2CondWaiter waiter = GetThreadCondWaiter(callerThreadID);

            lock (Waiters)
            {
                Waiters.Add(waiter);
            }

            // Unlock the mutex
            lwmutex.Unlock(callerThreadID);

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
                    waiter.Event.Wait();
                    ok = true;
                }

            }
            finally
            {

                // Reset the event
                waiter.Event.Reset();

                // Re aquire the lock
                lwmutex.Lock(callerThreadID, 0);

                // Remove the waiter from the list
                lock (Waiters)
                {
                    Waiters.Remove(waiter);
                }
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;


        }

        public ulong Signal()
        {
            lock (Waiters)
            {
                LV2CondWaiter? waiter = Waiters.FirstOrDefault();
                waiter?.Event.Set();
            }

            return CELL_OK;
        }

        public ulong SignalAll()
        {
            lock (Waiters)
            {
                foreach (LV2CondWaiter? waiter in Waiters)
                {
                    waiter.Event.Set();
                }
            }

            return CELL_OK;
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
