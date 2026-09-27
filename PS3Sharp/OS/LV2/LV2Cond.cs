using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    public sealed class LV2Cond
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct SysCondAttribute : IPS3Struct
        {
            public BigEndian<uint> PShared;
            public BigEndian<int> Flags;
            public BigEndian<ulong> IPC_Key;
            public InlineData.InlineArray8<byte> Name;
        }

        public uint Handle { get; } = MemoryManager.GetHandle();
        private readonly SysCondAttribute _attribute;
        private readonly LV2Mutex _mutex;
        private static readonly ConcurrentDictionary<uint, LV2CondWaiter> _threadCondWaiters = [];
        private readonly List<LV2CondWaiter> Waiters = new();

        public LV2Cond(SysCondAttribute attribute, LV2Mutex mutex)
        {
            _attribute = attribute;
            _mutex = mutex;
        }

        public ulong Wait(uint callerThreadID, ulong us_timeout = 0)
        {
            if (!_mutex.IsEntered)
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
            _mutex.Unlock();

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
                _mutex.Lock();

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

        public ulong SignalTo(uint waiterID)
        {
            LV2CondWaiter? waiter;

            lock (Waiters)
            {
                waiter = Waiters.FirstOrDefault(w => w.WaiterTheadID == waiterID);
            }

            waiter?.Event.Set();
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
