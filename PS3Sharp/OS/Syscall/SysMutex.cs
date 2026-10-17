using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Mutex;

namespace PS3Sharp.OS.Syscall
{
    internal class SysMutex
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2Mutex> _mutexes = new();

        internal SysMutex(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysMutexCreate(PPUThread ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<SysMutexAttribute> attrib_ptr = ppu.ReadArg<GuestPtr<SysMutexAttribute>>(ARG1);

            // Create and add the mutex to the dictionary
            SysMutexAttribute attribute = _memoryManager.ReadStruct<SysMutexAttribute>(attrib_ptr);
            LV2Mutex mutex = new(attribute, _memoryManager);
            AddMutex(mutex);

            // Write the handle
            _memoryManager.Write<BigEndian<uint>>(id_ptr, mutex.Handle);

            return CELL_OK;
        }

        internal ulong SysMutexDestroy(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            _mutexes.TryRemove(id, out _);
            return CELL_OK;
        }

        internal ulong SysMutexLock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (TryGetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.Lock(ppu);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysMutexTryLock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (TryGetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.TryLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysMutexUnlock(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (TryGetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.Unlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal bool TryGetMutex(uint id, out LV2Mutex? mutex) => _mutexes.TryGetValue(id, out mutex);

        private void AddMutex(LV2Mutex mutex)
        {
            bool success = _mutexes.TryAdd(mutex.Handle, mutex);
            if (!success)
            {
                throw new Exception($"[SysMutex] Could not add mutex 0x{mutex.Handle:X8} to dictionary");
            }
        }

        private void RemoveMutex(uint mutex_id)
        {
            bool success = _mutexes.TryRemove(mutex_id, out _);
            if (!success)
            {
                throw new Exception($"[SysMutex] Could not remove mutex 0x{mutex_id:X8} from dictionary");
            }
        }
    }
}
