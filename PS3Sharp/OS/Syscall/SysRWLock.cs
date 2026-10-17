using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2RWLock;

namespace PS3Sharp.OS.Syscall
{
    internal class SysRWLock
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2RWLock> _readWtrieLocks = [];

        internal SysRWLock(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysRWLockCreate(PPUThread ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<SysRWLockAttribute> attribut_ptr = ppu.ReadArg<GuestPtr<SysRWLockAttribute>>(ARG1);

            SysRWLockAttribute attribute = _memoryManager.ReadStruct<SysRWLockAttribute>(attribut_ptr);
            LV2RWLock rwlock = new(attribute, _memoryManager);
            AddRWLock(rwlock.Handle, rwlock);

            _memoryManager.Write<BigEndian<uint>>(id_ptr, rwlock.Handle);
            return CELL_OK;
        }

        internal ulong SysRWLockDestroy(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            RemoveRWLock(id);

            return CELL_OK;
        }

        internal ulong SysRWLockRLock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (TryGetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.RLock(ppu, us_timeout);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysRWLockTryRLock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (TryGetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.TryRLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysRWLockRUnlock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            if (TryGetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.RUnlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysRWLockWLock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (TryGetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.WLock(ppu, us_timeout);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysRWLockTryWLock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (TryGetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.TryWLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysRWLockWUnlock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (TryGetRWLock(id, out LV2RWLock rwlock))
            {
                return rwlock.WUnlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal bool TryGetRWLock(uint id, out LV2RWLock rwlock) => _readWtrieLocks.TryGetValue(id, out rwlock);

        private void AddRWLock(uint id, LV2RWLock rwlock)
        {
            bool success = _readWtrieLocks.TryAdd(id, rwlock);

            if (!success)
            {
                throw new Exception($"[SysRWLock] Could not add rwlock to dictionary");
            }
        }

        private void RemoveRWLock(uint id)
        {
            bool success = _readWtrieLocks.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[SysRWLock] Could not remove rwlock from dictionary");
            }
        }
    }
}
