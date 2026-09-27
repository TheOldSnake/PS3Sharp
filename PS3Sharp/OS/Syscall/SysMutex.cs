using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Mutex;

namespace PS3Sharp.OS.Syscall
{
    public static class SysMutex
    {

        private static readonly ConcurrentDictionary<uint, LV2Mutex> _mutexes = new();

        public static bool GetMutex(uint id, out LV2Mutex? mutex) => _mutexes.TryGetValue(id, out mutex);

        public static void AddMutex(LV2Mutex mutex)
        {
            bool success = _mutexes.TryAdd(mutex.Handle, mutex);
            if (!success)
            {
                throw new Exception($"[SysMutex] Could not add mutex 0x{mutex.Handle:X8} to dictionary");
            }
        }

        public static void RemoveMutex(uint mutex_id)
        {
            bool success = _mutexes.TryRemove(mutex_id, out _);
            if (!success)
            {
                throw new Exception($"[SysMutex] Could not remove mutex 0x{mutex_id:X8} from dictionary");
            }
        }

        public static ulong SysMutexCreate(PPU ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<SysMutexAttribute> attrib_ptr = ppu.ReadArg<GuestPtr<SysMutexAttribute>>(ARG1);

            // Create and add the mutex to the dictionary
            SysMutexAttribute attribute = attrib_ptr.Read();
            LV2Mutex mutex = new(attribute);
            AddMutex(mutex);

            // Write the handle
            MemoryManager.Write<uint>(id_ptr, mutex.Handle);

            return CELL_OK;
        }

        public static ulong SysMutexDestroy(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            _mutexes.TryRemove(id, out _);
            return CELL_OK;
        }

        public static ulong SysMutexLock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (GetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.Lock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong SysMutexTryLock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (GetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.TryLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong SysMutexUnlock(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);

            if (GetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.Unlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }
    }
}
