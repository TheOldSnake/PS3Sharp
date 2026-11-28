using PS3Sharp.Cell.Ppu;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Mutex;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysMutex
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2Mutex> _mutexes = new();

        internal SysMutex(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysMutexCreate(PpuThread ppu)
        {
            GuestPtr<BigEndian<uint>> id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<SysMutexAttribute> attrib_ptr = ppu.ReadArg<GuestPtr<SysMutexAttribute>>(Arg1);

            // Create and add the mutex to the dictionary
            SysMutexAttribute attribute = _memoryManager.Read<SysMutexAttribute>(attrib_ptr);
            LV2Mutex mutex = new(attribute, _memoryManager);
            AddMutex(mutex);

            // Write the handle
            _memoryManager.Write<BigEndian<uint>>(id_ptr, mutex.Handle);

            return CellOk;
        }

        internal ulong SysMutexDestroy(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            _mutexes.TryRemove(id, out _);
            return CellOk;
        }

        internal ulong SysMutexLock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            if (TryGetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.Lock(ppu);

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysMutexTryLock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);

            if (TryGetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.TryLock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysMutexUnlock(PpuThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);

            if (TryGetMutex(id, out LV2Mutex? mutex))
            {
                return mutex.Unlock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal bool TryGetMutex(uint id, [NotNullWhen(true)] out LV2Mutex? mutex) => _mutexes.TryGetValue(id, out mutex);

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
