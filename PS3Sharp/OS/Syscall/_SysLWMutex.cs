using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2LWMutex;

namespace PS3Sharp.OS.Syscall
{
    // This is for _sys_lwmutex syscalls, not sys_lwmutex (the module)
    // These syscalls gets called when we LLE the module, from liblv2
    internal class _SysLWMutex
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2LWMutex> _lwMutexes = new();

        private _SysLWMutex(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong _SysLWmutexCreate(PPUThread ppu)
        {
            // TODO

            GuestPtr<BigEndian<uint>> id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            uint protocol = ppu.ReadArg<uint>(Arg1);
            GuestPtr<LWMutex> control = ppu.ReadArg<GuestPtr<LWMutex>>(Arg2);
            int hasName = ppu.ReadArg<int>(Arg3);
            ulong name = ppu.ReadArg<ulong>(Arg4);

            uint handle = _memoryManager.GetHandle();
       
            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_create id: 0x{handle:X8}");

            if (protocol != SysSyncFifo && protocol != SysSyncRetry && protocol != SysSyncPriority)
            {
                return CellEInval;
            }

            // Create the lwmutex, attribute not needed for these syscalls i guess
            LV2LWMutex mutex = new(control, attribute: default, _memoryManager);
            AddLWMutex(handle, mutex);

            // Write the handle
            _memoryManager.Write<BigEndian<uint>>(id_ptr, handle);

            return CellOk;
        }

        internal ulong _SysLWmutexDestroy(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_destroy id: 0x{id:X8}");
            RemoveLWMutex(id);

            return CellOk;
        }

        internal ulong _SysLWmutexLock(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_lock id: 0x{id:X8}");

            if (TryGetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex._Sys_Lock(us_timeout);

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong _SysLWmutexTryLock(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_trylock id: 0x{id:X8}");

            if (TryGetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex._Sys_TryLock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong _SysLWmutexUnlock(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_unlock id: 0x{id:X8}");

            if (TryGetLWMutex(id, out LV2LWMutex? lwmutex))
            {
                return lwmutex._Sys_Unlock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal bool TryGetLWMutex(uint id, [NotNullWhen(true)] out LV2LWMutex? lwmutex) => _lwMutexes.TryGetValue(id, out lwmutex);

        private void AddLWMutex(uint id, LV2LWMutex mutex)
        {
            bool success = _lwMutexes.TryAdd(id, mutex);

            if (!success)
            {
                throw new Exception($"[_SysLWMutex] Could not add lwmutex 0x{id:X8} to dictionary");
            }
        }

        private void RemoveLWMutex(uint id)
        {
            bool success = _lwMutexes.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[_SysLWMutex] Could not remove lwmutex 0x{id:X8} from dictionary");
            }
        }
    }
}
