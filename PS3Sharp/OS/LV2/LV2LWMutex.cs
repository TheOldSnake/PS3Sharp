using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;

using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{

    internal sealed class LV2LWMutex
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct LWMutex : IPS3Struct
        {
            internal BigEndian<uint> Owner;
            internal BigEndian<uint> Waiter;
            internal BigEndian<uint> Attribute;
            internal BigEndian<uint> RecursiveCount;
            internal BigEndian<uint> SleepQueue;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LWMutexAttrib : IPS3Struct
        {
            internal BigEndian<uint> Protocol;
            internal BigEndian<uint> Recursive;
            internal InlineData.InlineArray8<byte> Name;
        }

        internal const uint SYS_SYNC_RECURSIVE = 0x10;
        internal const uint SYS_SYNC_NOT_RECURSIVE = 0x20;

        internal const uint SYS_SYNC_FIFO = 0x1;                  // First In, First Out Order
        internal const uint SYS_SYNC_PRIORITY = 0x2;              // Priority Order
        internal const uint SYS_SYNC_PRIORITY_INHERIT = 0x3;      // Basic Priority Inheritance Protocol
        internal const uint SYS_SYNC_RETRY = 0x4;                 // Not selected while unlocking

        private readonly object _mutex = new();

        internal readonly GuestPtr<LWMutex> InstancePtr;
        private readonly LWMutexAttrib _attributes;
        private readonly bool _isRecursive;

        internal LV2LWMutex(GuestPtr<LWMutex> ptr, LWMutexAttrib attribute)
        {
            InstancePtr = ptr;
            _attributes = attribute;
            _isRecursive = attribute.Recursive == SYS_SYNC_RECURSIVE;
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

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        internal ulong _Sys_TryLock()
        {
            bool ok = Monitor.TryEnter(_mutex, 0);
            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        internal ulong _Sys_Unlock()
        {
            if (!Monitor.IsEntered(_mutex))
            {
                return CELL_EINVAL;
            }

            Monitor.Exit(_mutex);
            return CELL_OK;
        }


        // functions for sys_lwmutex module:
        internal ulong Lock(uint threadID, ulong us_timeout)
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
                    Monitor.Enter(_mutex);
                    ok = true;
                }

                if (ok)
                {
                    // if acquiring succeeds, set the owner to the current thread
                    SetOwnerFirstTime(threadID);
                    return CELL_OK;

                }
                else
                {
                    return CELL_ETIMEDOUT;
                }
            }
        }

        internal ulong TryLock(uint threadID)
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
                    SetOwnerFirstTime(threadID);
                    return CELL_OK;

                }
                else
                {
                    return CELL_EBUSY;
                }
            }
        }

        private ulong ReEnter()
        {
            if (!_isRecursive)
            {
                return CELL_EDEADLK;
            }

            Monitor.Enter(_mutex);

            using (MemoryManager.AcquireTemporaryLock())
            {
                unsafe
                {
                    LWMutex* mutex = InstancePtr.AsNativePtr();
                    uint currentCount = mutex->RecursiveCount;
                    mutex->RecursiveCount = currentCount + 1;
                }
            }

            return CELL_OK;
        }

        private void SetOwnerFirstTime(uint threadID)
        {
            // For when we get the lock for the first time, not a re-entrance

            using (MemoryManager.AcquireTemporaryLock())
            {
                unsafe
                {
                    LWMutex* mutex = InstancePtr.AsNativePtr();
                    mutex->Owner = threadID;
                    mutex->RecursiveCount = _isRecursive ? 1u : 0u;
                }
            }
        }

        internal ulong Unlock(uint threadID)
        {
            // Error if we did not lock the mutex before
            if (!Monitor.IsEntered(_mutex))
            {
                return CELL_EINVAL;
            }

            // Handle updating the state before unlocking
            bool becameFree;
            using (MemoryManager.AcquireTemporaryLock())
            {
                unsafe
                {
                    LWMutex* mutex = InstancePtr.AsNativePtr();

                    if (_isRecursive && mutex->RecursiveCount > 0)
                    {
                        mutex->RecursiveCount--;

                        if (mutex->RecursiveCount == 0)
                        {
                            mutex->Owner = 0xFFFFFFFF;
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
                        mutex->Owner = 0xFFFFFFFF;
                        mutex->RecursiveCount = 0;
                    }
                }

                Monitor.Exit(_mutex);
            }

            // Sanity check
            if (becameFree && Monitor.IsEntered(_mutex))
            {
                throw new Exception($"[LwMutex] unlock bug");
            }

            return CELL_OK;
        }


        /*private bool CanLock(ref SysLwMutex.LwMutex mutex, uint threadID) {
            bool isFree = mutex.Owner == 0xFFFFFFFF;
            bool ok;

            using (MemoryManager.AcquireTemporaryLock()) {
                if (isFree) {
                    // If the mutex is free then we can lock
                    ok = true;

                } else {

                    if (mutex.Owner == threadID) {
                        // If the same thread tries to lock twice then it depends on "IsRecursive"
                        ok = _isRecursive;

                    } else {
                        // Fially, if a different thread tries to lock a locked mutex it will wait
                        ok = true;
                    }
                }

            }

            return ok;
        }

        private void UpdateMutexState(ref SysLwMutex.LwMutex mutex) {
            MemoryManager.EnsureHoldingMemoryLock(); //caller must be in a memory lock block

            if (_isRecursive && mutex.RecursiveCount > 0) {
                mutex.RecursiveCount--;

                if (mutex.RecursiveCount == 0) {
                    mutex.Owner = 0xFFFFFFFF;
                }

            } else {
                mutex.Owner = 0xFFFFFFFF;
                mutex.RecursiveCount = 0;
            }
        }*/
    }
}
