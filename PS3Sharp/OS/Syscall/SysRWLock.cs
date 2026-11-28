using PS3Sharp.Cell.Ppu;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2RWLock;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysRWLock
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2RWLock> _readWtrieLocks = [];

        internal SysRWLock(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysRWLockCreate(PpuThread ppu)
        {
            GuestPtr<BigEndian<uint>> id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<SysRWLockAttribute> attribut_ptr = ppu.ReadArg<GuestPtr<SysRWLockAttribute>>(Arg1);

            SysRWLockAttribute attribute = _memoryManager.Read<SysRWLockAttribute>(attribut_ptr);
            LV2RWLock rwlock = new(attribute, _memoryManager);
            AddRWLock(rwlock.Handle, rwlock);

            _memoryManager.Write<BigEndian<uint>>(id_ptr, rwlock.Handle);
            return CellOk;
        }

        internal ulong SysRWLockDestroy(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);

            RemoveRWLock(id);

            return CellOk;
        }

        internal ulong SysRWLockRLock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            if (TryGetRWLock(id, out LV2RWLock? rwlock))
            {
                return rwlock.RLock(ppu, us_timeout);

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysRWLockTryRLock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);

            if (TryGetRWLock(id, out LV2RWLock? rwlock))
            {
                return rwlock.TryRLock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysRWLockRUnlock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            if (TryGetRWLock(id, out LV2RWLock? rwlock))
            {
                return rwlock.RUnlock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysRWLockWLock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            if (TryGetRWLock(id, out LV2RWLock? rwlock))
            {
                return rwlock.WLock(ppu, us_timeout);

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysRWLockTryWLock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);

            if (TryGetRWLock(id, out LV2RWLock? rwlock))
            {
                return rwlock.TryWLock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysRWLockWUnlock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);

            if (TryGetRWLock(id, out LV2RWLock? rwlock))
            {
                return rwlock.WUnlock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal bool TryGetRWLock(uint id, [NotNullWhen(true)] out LV2RWLock? rwlock) => _readWtrieLocks.TryGetValue(id, out rwlock);

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
