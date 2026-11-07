using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2LwMutex;

namespace PS3Sharp.OS.Syscall
{
    // This is for _sys_lwmutex syscalls, not sys_lwmutex (the module)
    // These syscalls gets called when we LLE the module, from liblv2
    internal class _SysLwMutex
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2LwMutex> _lwMutexes = new();

        private _SysLwMutex(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong _SysLwmutexCreate(PPUThread ppu)
        {
            // TODO

            GuestPtr<BigEndian<uint>> id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            uint protocol = ppu.ReadArg<uint>(Arg1);
            GuestPtr<LwMutex> control = ppu.ReadArg<GuestPtr<LwMutex>>(Arg2);
            int hasName = ppu.ReadArg<int>(Arg3);
            ulong name = ppu.ReadArg<ulong>(Arg4);

            uint handle = _memoryManager.GetHandle();
       
            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_create id: 0x{handle:X8}");

            if (protocol != SysSyncFifo && protocol != SysSyncRetry && protocol != SysSyncPriority)
            {
                return CellEInval;
            }

            // Create the lwmutex, attribute not needed for these syscalls i guess
            LV2LwMutex mutex = new(control, attribute: default, _memoryManager);
            AddLwMutex(handle, mutex);

            // Write the handle
            _memoryManager.Write<BigEndian<uint>>(id_ptr, handle);

            return CellOk;
        }

        internal ulong _SysLwmutexDestroy(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_destroy id: 0x{id:X8}");
            RemoveLwMutex(id);

            return CellOk;
        }

        internal ulong _SysLwmutexLock(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_lock id: 0x{id:X8}");

            if (TryGetLwMutex(id, out LV2LwMutex? lwmutex))
            {
                return lwmutex._Sys_Lock(us_timeout);

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong _SysLwmutexTryLock(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_trylock id: 0x{id:X8}");

            if (TryGetLwMutex(id, out LV2LwMutex? lwmutex))
            {
                return lwmutex._Sys_TryLock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong _SysLwmutexUnlock(PPUThread ppu)
        {
            // TODO
            uint id = ppu.ReadArg<uint>(Arg0);

            Console.WriteLine($"[_SysLWMutex] _sys_lwmutex_unlock id: 0x{id:X8}");

            if (TryGetLwMutex(id, out LV2LwMutex? lwmutex))
            {
                return lwmutex._Sys_Unlock();

            }
            else
            {
                return CellESrch;
            }
        }

        internal bool TryGetLwMutex(uint id, [NotNullWhen(true)] out LV2LwMutex? lwmutex) => _lwMutexes.TryGetValue(id, out lwmutex);

        private void AddLwMutex(uint id, LV2LwMutex mutex)
        {
            bool success = _lwMutexes.TryAdd(id, mutex);

            if (!success)
            {
                throw new Exception($"[_SysLWMutex] Could not add lwmutex 0x{id:X8} to dictionary");
            }
        }

        private void RemoveLwMutex(uint id)
        {
            bool success = _lwMutexes.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[_SysLWMutex] Could not remove lwmutex 0x{id:X8} from dictionary");
            }
        }
    }
}
