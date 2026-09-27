using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2LWMutex;

namespace PS3Sharp.OS.Syscall
{

    // This is for _sys_lwmutex syscalls, not sys_lwmutex (the module)
    // These syscalls gets called when we LLE the module, from liblv2

    public static class _SysLWMutex
    {
        private static readonly ConcurrentDictionary<uint, LV2LWMutex> _lwMutexes = new ConcurrentDictionary<uint, LV2LWMutex>();

        public static bool GetLWMutex(uint id, out LV2LWMutex lwmutex) => _lwMutexes.TryGetValue(id, out lwmutex);

        public static void AddLWMutex(uint id, LV2LWMutex mutex)
        {
            bool success = _lwMutexes.TryAdd(id, mutex);

            if (!success)
            {
                throw new Exception($"[_SysLWMutex] Could not add lwmutex 0x{id:X8} to dictionary");
            }
        }

        public static void RemoveLWMutex(uint id)
        {
            bool success = _lwMutexes.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[_SysLWMutex] Could not remove lwmutex 0x{id:X8} from dictionary");
            }
        }

        public static ulong _SysLWmutexCreate(PPU ppu)
        {
            // TODO

            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            uint protocol = ppu.ReadArg<uint>(ARG1);
            GuestPtr<LWMutex> control = ppu.ReadArg<GuestPtr<LWMutex>>(ARG2);
            int hasName = ppu.ReadArg<int>(ARG3);
            ulong name = ppu.ReadArg<ulong>(ARG4);

            uint handle = MemoryManager.GetHandle();
            LWMutexAttrib attribute = default;     // not needed for these syscalls i guess

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_create id: 0x{handle:X8}");

            if (protocol != SYS_SYNC_FIFO && protocol != SYS_SYNC_RETRY && protocol != SYS_SYNC_PRIORITY)
            {
                return CELL_EINVAL;
            }

            // Create the lwmutex
            LV2LWMutex mutex = new LV2LWMutex(control, attribute);
            AddLWMutex(handle, mutex);

            // Write the handle
            MemoryManager.Write<uint>(id_ptr, handle);

            return CELL_OK;
        }

        public static ulong _SysLWmutexDestroy(PPU ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(ARG0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_destroy id: 0x{id:X8}");
            RemoveLWMutex(id);

            return CELL_OK;
        }

        public static ulong _SysLWmutexLock(PPU ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_lock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex lwmutex))
            {
                return lwmutex._Sys_Lock(us_timeout);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong _SysLWmutexTryLock(PPU ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(ARG0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_trylock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex lwmutex))
            {
                return lwmutex._Sys_TryLock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong _SysLWmutexUnlock(PPU ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(ARG0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_unlock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex lwmutex))
            {
                return lwmutex._Sys_Unlock();

            }
            else
            {
                return CELL_ESRCH;
            }
        }
    }
}
