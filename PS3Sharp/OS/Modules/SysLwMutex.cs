using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2LWMutex;

namespace PS3Sharp.OS.Modules
{

    // This is for sys_lwmutex module, not _sys_lwmutex (the syscalls)

    public static class SysLwMutex
    {
        private static readonly ConcurrentDictionary<uint, LV2LWMutex> _lwMutexes = [];

        public static bool GetLWMutex(uint id, out LV2LWMutex? mutex) => _lwMutexes.TryGetValue(id, out mutex);
        public static void AddLWMutex(uint id, LV2LWMutex mutex)
        {
            bool success = _lwMutexes.TryAdd(id, mutex);

            if (!success)
            {
                throw new Exception($"[SysLwMutex] Could not add lwmutex 0x{id:X8} to dictionary");
            }
        }

        public static void RemoveLWMutex(uint id)
        {
            bool success = _lwMutexes.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[SysLwMutex] Could not remove lwmutex 0x{id:X8} from dictionary");
            }
        }

        public static ulong SysLWmutexCreate(PPU ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);
            GuestPtr<LWMutexAttrib> attribute_ptr = ppu.ReadArg<GuestPtr<LWMutexAttrib>>(ARG1);

            if (lwmutex_ptr.IsNullPtr || attribute_ptr.IsNullPtr)
            {
                Console.WriteLine($"[SysLwMutex] sys_lwmutex_create: got nullptr -> CELL_EAGAIN");
                return CELL_EAGAIN;
            }

            uint id = MemoryManager.GetHandle();

            unsafe
            {
                LWMutexAttrib attribute = attribute_ptr.Read();
                LWMutex* lwmutex = lwmutex_ptr.AsNativePtr();

                lwmutex->Owner = 0xFFFFFFFF; //-1 = free
                lwmutex->Waiter = 0;
                lwmutex->Attribute = attribute.Protocol | attribute.Recursive;
                lwmutex->RecursiveCount = 0;
                lwmutex->SleepQueue = id;    // Store the handle in the "SleepQueue" 

                //Console.WriteLine($"[SysLwMutex] sys_lwmutex_create id: 0x{id:X8}");

                LV2LWMutex mutex = new LV2LWMutex(lwmutex_ptr, attribute);
                AddLWMutex(id, mutex);
            }

            return CELL_OK;
        }

        public static ulong SysLWmutexLock(PPU ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            uint id;
            unsafe
            {
                LWMutex* mutex = lwmutex_ptr.AsNativePtr();
                id = mutex->SleepQueue;
            }

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_lock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex.Lock(ppu.ThreadID, us_timeout);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong SysLWmutexTryLock(PPU ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);

            uint id;
            unsafe
            {
                LWMutex* mutex = lwmutex_ptr.AsNativePtr();
                id = mutex->SleepQueue;
            }

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_trylock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex.TryLock(ppu.ThreadID);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong SysLWmutexUnlock(PPU ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);

            uint id;
            unsafe
            {
                LWMutex* mutex = lwmutex_ptr.AsNativePtr();
                id = mutex->SleepQueue;
            }

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_unlock id: 0x{id:X8}");

            if (GetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex.Unlock(ppu.ThreadID);

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        public static ulong SysLWmutexDestroy(PPU ppu)
        {
            GuestPtr<LWMutex> lwmutex_ptr = ppu.ReadArg<GuestPtr<LWMutex>>(ARG0);

            uint id;
            unsafe
            {
                LWMutex* mutex = lwmutex_ptr.AsNativePtr();
                id = mutex->SleepQueue;
            }

            //Console.WriteLine($"[SysLwMutex] sys_lwmutex_destroy id: 0x{id:X8}");
            RemoveLWMutex(id);

            return CELL_OK;
        }
    }
}
