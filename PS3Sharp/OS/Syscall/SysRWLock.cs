using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2RWLock;

namespace PS3Sharp.OS.Syscall
{
    internal static class SysRWLock
    {
        private static readonly ConcurrentDictionary<uint, LV2RWLock> _rwLocks = [];

        internal static bool GetRWLock(uint id, out LV2RWLock rwlock) => _rwLocks.TryGetValue(id, out rwlock);

        internal static void AddRWLock(uint id, LV2RWLock rwlock)
        {
            bool success = _rwLocks.TryAdd(id, rwlock);

            if (!success)
            {
                throw new Exception($"[SysRWLock] Could not add rwlock to dictionary");
            }
        }

        internal static void RemoveRWLock(uint id)
        {
            bool success = _rwLocks.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[SysRWLock] Could not remove rwlock from dictionary");
            }
        }

        internal static ulong SysRWLockCreate(PPU ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<SysRWLockAttribute> attribut_ptr = ppu.ReadArg<GuestPtr<SysRWLockAttribute>>(ARG1);

            SysRWLockAttribute attribute = attribut_ptr.Read();
            LV2RWLock rwlock = new(attribute);
            AddRWLock(rwlock.Handle, rwlock);

            MemoryManager.Write<uint>(id_ptr, rwlock.Handle);
            return CELL_OK;
        }

        internal static ulong SysRWLockDestroy(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            RemoveRWLock(id);

            return CELL_OK;
        }

        internal static ulong SysRWLockRLock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (GetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.RLock(us_timeout);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysRWLockTryRLock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (GetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.TryRLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysRWLockRUnlock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            if (GetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.RUnlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysRWLockWLock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (GetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.WLock(us_timeout);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysRWLockTryWLock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (GetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.TryWLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysRWLockWUnlock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (GetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.WUnlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }
    }
}
