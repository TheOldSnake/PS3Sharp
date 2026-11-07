using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Runtime.InteropServices;

using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{

    internal sealed class LV2LwMutex
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct LwMutex : IPS3Struct
        {
            internal BigEndian<uint> Owner;
            internal BigEndian<uint> Waiter;
            internal BigEndian<uint> Attribute;
            internal BigEndian<uint> RecursiveCount;
            internal BigEndian<uint> SleepQueue;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LwMutexAttrib : IPS3Struct
        {
            internal BigEndian<uint> Protocol;
            internal BigEndian<uint> Recursive;
            internal InlineData.InlineArray8<byte> Name;
        }

        internal const uint SysSyncRecursive = 0x10;
        internal const uint SysSyncNotRecursive = 0x20;

        internal const uint SysSyncFifo = 0x1;                  // First In, First Out Order
        internal const uint SysSyncPriority = 0x2;              // Priority Order
        internal const uint SysSyncPriorityInherit = 0x3;      // Basic Priority Inheritance Protocol
        internal const uint SysSyncRetry = 0x4;                 // Not selected while unlocking

        private readonly MemoryManager _memoryManager;
        private readonly object _mutex = new();
        private readonly LwMutexAttrib _attributes;
        private readonly bool _isRecursive;
        private readonly GuestPtr<LwMutex> _instancePtr;

        internal LV2LwMutex(GuestPtr<LwMutex> ptr, LwMutexAttrib attribute, MemoryManager memoryManager)
        {
            _instancePtr = ptr;
            _attributes = attribute;
            _isRecursive = attribute.Recursive == SysSyncRecursive;
            _memoryManager = memoryManager;
        }

        internal bool IsEntered()
        {
            return Monitor.IsEntered(_mutex);
        }

        // functions for _sys_lwmutex syscalls:
        internal ulong _Sys_Lock(ulong us_timeout)
        {
            bool ok;
            if (us_timeout > 0)
            {
                ok = Monitor.TryEnter(_mutex, TimeSpan.FromMicroseconds(us_timeout));
            }
            else
            {
                Monitor.Enter(_mutex);
                ok = true;
            }

            return ok ? CellOk : CellETimedout;
        }

        internal ulong _Sys_TryLock()
        {
            bool ok = Monitor.TryEnter(_mutex, 0);
            return ok ? CellOk : CellETimedout;
        }

        internal ulong _Sys_Unlock()
        {
            if (!Monitor.IsEntered(_mutex))
            {
                return CellEInval;
            }

            Monitor.Exit(_mutex);
            return CellOk;
        }


        // functions for sys_lwmutex module:
        internal ulong Lock(PPUThread ppu, ulong us_timeout)
        {
            //bool canLock = CanLock(ref mutex, threadID);

            if (Monitor.IsEntered(_mutex))
            {
                return ReEnter();

            }
            else
            {
                // Acquire the Mutex, with timeout if set
                bool ok;

                if (us_timeout > 0)
                {
                    ok = Monitor.TryEnter(_mutex, TimeSpan.FromMicroseconds(us_timeout));
                }
                else
                {
                    ok = InterruptableWait(ppu);
                }

                if (ok)
                {
                    // if acquiring succeeds, set the owner to the current thread
                    SetOwnerFirstTime(ppu.ThreadID);
                    return CellOk;
                }
                else
                {
                    return CellETimedout;
                }
            }
        }

        internal ulong TryLock(PPUThread ppu)
        {

            if (Monitor.IsEntered(_mutex))
            {
                return ReEnter();

            }
            else
            {
                // Try to acquire the Mutex
                bool ok = Monitor.TryEnter(_mutex, 0);

                if (ok)
                {
                    // if acquiring succeeds, set the owner to the current thread
                    SetOwnerFirstTime(ppu.ThreadID);
                    return CellOk;

                }
                else
                {
                    return CellEBusy;
                }
            }
        }

        private ulong ReEnter()
        {
            if (!_isRecursive)
            {
                return CellEDeadlk;
            }

            Monitor.Enter(_mutex);

            using (_memoryManager.AcquireTemporaryLock())
            {
                LwMutex mutex = _memoryManager.Read<LwMutex>(_instancePtr);
                uint currentCount = mutex.RecursiveCount;
                mutex.RecursiveCount = currentCount + 1;
                _memoryManager.Write(_instancePtr, mutex);
            }

            return CellOk;
        }

        private void SetOwnerFirstTime(uint threadID)
        {
            // For when we get the lock for the first time, not a re-entrance

            using (_memoryManager.AcquireTemporaryLock())
            {
                LwMutex mutex = _memoryManager.Read<LwMutex>(_instancePtr);
                mutex.Owner = threadID;
                mutex.RecursiveCount = _isRecursive ? 1u : 0u;
                _memoryManager.Write(_instancePtr, mutex);
            }
        }

        internal ulong Unlock()
        {
            // Error if we did not lock the mutex before
            if (!Monitor.IsEntered(_mutex))
            {
                return CellEInval;
            }

            // Handle updating the state before unlocking
            bool becameFree;
            using (_memoryManager.AcquireTemporaryLock())
            {
                LwMutex mutex = _memoryManager.Read<LwMutex>(_instancePtr);

                if (_isRecursive && mutex.RecursiveCount > 0)
                {
                    mutex.RecursiveCount--;

                    if (mutex.RecursiveCount == 0)
                    {
                        mutex.Owner = 0xFFFFFFFF;
                        becameFree = true;
                    }
                    else
                    {
                        becameFree = false;
                    }

                }
                else
                {
                    becameFree = true;
                    mutex.Owner = 0xFFFFFFFF;
                    mutex.RecursiveCount = 0;
                }

                _memoryManager.Write(_instancePtr, mutex);
                Monitor.Exit(_mutex);
            }

            // Sanity check
            if (becameFree && Monitor.IsEntered(_mutex))
            {
                throw new Exception($"[LwMutex] unlock bug");
            }

            return CellOk;
        }

        private bool InterruptableWait(PPUThread ppu)
        {
            // Return true if the lock is acquired, false if the thread was interrupted
            while (true)
            {

                // Wait for 100ms
                bool ok = Monitor.TryEnter(_mutex, TimeSpan.FromMilliseconds(100));

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
