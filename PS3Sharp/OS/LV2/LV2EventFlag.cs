using PS3Sharp.Memory_Manager;

using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2EventFlag
    {
        internal enum SysEventFlagMode : uint
        {
            SYS_EVENT_FLAG_WAIT_AND = 0x01,
            SYS_EVENT_FLAG_WAIT_OR = 0x02,
            SYS_EVENT_FLAG_WAIT_CLEAR = 0x10,
            SYS_EVENT_FLAG_WAIT_CLEAR_ALL = 0x20
        }


        private readonly object _lock = new();
        private ulong _value;
        internal uint Handle { get; } = MemoryManager.GetHandle();

        internal LV2EventFlag(ulong initialValue)
        {
            _value = initialValue;
        }

        internal void Set(ulong value)
        {
            lock (_lock)
            {
                _value |= value;
                Monitor.PulseAll(_lock);
            }
        }

        internal void Clear(ulong value)
        {
            lock (_lock)
            {
                _value &= value;
                Monitor.PulseAll(_lock);
            }
        }

        internal ulong Wait(SysEventFlagMode mode, ulong pattern, ulong us_timeout, out ulong resultValue)
        {

            lock (_lock)
            {

                bool firstIteration = true;
                bool isUsingTimeOut = us_timeout > 0;
                DateTime deadline = DateTime.UtcNow + TimeSpan.FromMicroseconds(us_timeout);

                while (!IsSatisfied(mode, pattern))
                {
                    firstIteration = false;

                    // Monitor.Wait() releases the lock and waits until the thread is notified by Monitor.Pulse

                    if (isUsingTimeOut)
                    {
                        TimeSpan remaining_time = deadline - DateTime.UtcNow;

                        if (remaining_time <= TimeSpan.Zero)
                        {
                            // timeout
                            resultValue = 0;
                            return CELL_ETIMEDOUT;
                        }

                        if (!Monitor.Wait(_lock, remaining_time))
                        {
                            // timeout
                            resultValue = 0;
                            return CELL_ETIMEDOUT;
                        }

                    }
                    else
                    {
                        Monitor.Wait(_lock);
                    }
                }

                resultValue = _value;   // this is not the error code
                Clear(mode, pattern);
                Monitor.PulseAll(_lock);

                return CELL_OK;
            }
        }

        private bool IsSatisfied(SysEventFlagMode mode, ulong pattern)
        {
            bool is_And = (mode & SysEventFlagMode.SYS_EVENT_FLAG_WAIT_AND) != 0;
            bool is_or = (mode & SysEventFlagMode.SYS_EVENT_FLAG_WAIT_OR) != 0;

            if (is_And)
            {
                return (_value & pattern) == pattern;

            }
            else if (is_or)
            {
                return (_value & pattern) != 0;

            }
            else
            {
                throw new Exception($"[SysEventFlagMode] Unknown wait mode: 0x{(uint)mode:X8}");
            }
        }

        private void Clear(SysEventFlagMode mode, ulong pattern)
        {
            bool clearPattern = (mode & SysEventFlagMode.SYS_EVENT_FLAG_WAIT_CLEAR) != 0;
            bool clearAll = (mode & SysEventFlagMode.SYS_EVENT_FLAG_WAIT_CLEAR_ALL) != 0;

            if (clearPattern)
            {
                _value &= ~pattern;

            }
            else if (clearAll)
            {
                _value = 0;
            }
        }
    }
}
