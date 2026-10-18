using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2LWMutex;

namespace PS3Sharp.OS.Modules
{

    // This is for sys_lwmutex module, not _sys_lwmutex (the syscalls)
    internal class SysLwMutex
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2LWMutex> _lwMutexes = [];

        internal SysLwMutex(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysLWmutexCreate(PPUThread ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);
            GuestPtr<LWMutexAttrib> attribute_ptr = ppu.ReadArg<GuestPtr<LWMutexAttrib>>(ARG1);

            if (lwmutex_ptr.IsNullPtr || attribute_ptr.IsNullPtr)
            {
                Console.WriteLine($"[SysLwMutex] sys_lwmutex_create: got nullptr -> CELL_EAGAIN");
                return CELL_EAGAIN;
            }

            uint id = _memoryManager.GetHandle();

            LWMutexAttrib attribute = _memoryManager.Read<LWMutexAttrib>(attribute_ptr);
            LWMutex lwmutex = _memoryManager.Read<LWMutex>(lwmutex_ptr);

            lwmutex.Owner = 0xFFFFFFFF; //-1 = free
            lwmutex.Waiter = 0;
            lwmutex.Attribute = attribute.Protocol | attribute.Recursive;
            lwmutex.RecursiveCount = 0;
            lwmutex.SleepQueue = id;    // Store the handle in the "SleepQueue" 

            _memoryManager.Write<LWMutex>(lwmutex_ptr, lwmutex);

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_create id: 0x{id:X8}");

            LV2LWMutex mutex = new(lwmutex_ptr, attribute, _memoryManager);
            AddLWMutex(id, mutex);

            return CELL_OK;
        }

        internal ulong SysLWmutexLock(PPUThread ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            LWMutex mutex = _memoryManager.Read<LWMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_lock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex.Lock(ppu, us_timeout);
            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysLWmutexTryLock(PPUThread ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);

            LWMutex mutex = _memoryManager.Read<LWMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_trylock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex.TryLock(ppu);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysLWmutexUnlock(PPUThread ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);

            LWMutex mutex = _memoryManager.Read<LWMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_unlock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex.Unlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysLWmutexDestroy(PPUThread ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);

            LWMutex mutex = _memoryManager.Read<LWMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_destroy id: 0x{id:X8}");
            RemoveLWMutex(id);

            return CELL_OK;
        }

        internal bool GetLWMutex(uint id, out LV2LWMutex? mutex) => _lwMutexes.TryGetValue(id, out mutex);

        private void AddLWMutex(uint id, LV2LWMutex mutex)
        {
            bool success = _lwMutexes.TryAdd(id, mutex);

            if (!success)
            {
                throw new Exception($"[SysLwMutex] Could not add lwmutex 0x{id:X8} to dictionary");
            }
        }

        private void RemoveLWMutex(uint id)
        {
            bool success = _lwMutexes.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[SysLwMutex] Could not remove lwmutex 0x{id:X8} from dictionary");
            }
        }
    }
}
