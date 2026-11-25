using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2LwMutex;

namespace PS3Sharp.OS.Modules
{

    // This is for sys_lwmutex module, not _sys_lwmutex (the syscalls)
    internal sealed class SysLwMutex
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2LwMutex> _lwMutexes = [];

        internal SysLwMutex(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysLWmutexCreate(PPUThread ppu)
        {
            GuestPtr<LwMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LwMutex>>(Arg0);
            GuestPtr<LwMutexAttrib> attribute_ptr = ppu.ReadArg<GuestPtr<LwMutexAttrib>>(Arg1);

            if (lwmutex_ptr.IsNullPtr || attribute_ptr.IsNullPtr)
            {
                Console.WriteLine($"[SysLwMutex] sys_lwmutex_create: got nullptr -> CELL_EAGAIN");
                return CellEAgain;
            }

            uint id = _memoryManager.GetHandle();

            LwMutexAttrib attribute = _memoryManager.Read<LwMutexAttrib>(attribute_ptr);
            LwMutex lwmutex = _memoryManager.Read<LwMutex>(lwmutex_ptr);

            lwmutex.Owner = 0xFFFFFFFF; //-1 = free
            lwmutex.Waiter = 0;
            lwmutex.Attribute = attribute.Protocol | attribute.Recursive;
            lwmutex.RecursiveCount = 0;
            lwmutex.SleepQueue = id;    // Store the handle in the "SleepQueue" 

            _memoryManager.Write<LwMutex>(lwmutex_ptr, lwmutex);

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_create id: 0x{id:X8}");

            LV2LwMutex mutex = new(lwmutex_ptr, attribute, _memoryManager);
            AddLWMutex(id, mutex);

            return CellOk;
        }

        internal ulong SysLWmutexLock(PPUThread ppu)
        {
            GuestPtr<LwMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LwMutex>>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            LwMutex mutex = _memoryManager.Read<LwMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_lock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LwMutex? lwmutex))
            {
                return lwmutex.Lock(ppu, us_timeout);
            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysLWmutexTryLock(PPUThread ppu)
        {
            GuestPtr<LwMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LwMutex>>(Arg0);

            LwMutex mutex = _memoryManager.Read<LwMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_trylock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LwMutex? lwmutex))
            {
                return lwmutex.TryLock(ppu);

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysLWmutexUnlock(PPUThread ppu)
        {
            GuestPtr<LwMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LwMutex>>(Arg0);

            LwMutex mutex = _memoryManager.Read<LwMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_unlock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LwMutex? lwmutex))
            {
                return lwmutex.Unlock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysLWmutexDestroy(PPUThread ppu)
        {
            GuestPtr<LwMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LwMutex>>(Arg0);

            LwMutex mutex = _memoryManager.Read<LwMutex>(lwmutex_ptr);
            uint id = mutex.SleepQueue;

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_destroy id: 0x{id:X8}");
            RemoveLWMutex(id);

            return CellOk;
        }

        internal bool GetLWMutex(uint id, out LV2LwMutex? mutex) => _lwMutexes.TryGetValue(id, out mutex);

        private void AddLWMutex(uint id, LV2LwMutex mutex)
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
