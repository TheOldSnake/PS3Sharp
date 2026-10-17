using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2EventFlag;

namespace PS3Sharp.OS.Syscall
{
    internal class SysEventFlag
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2EventFlag> _eventFlags = new();

        internal SysEventFlag(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysEventFlagCreate(PPUThread ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestVoidPtr attributes_ptr = ppu.ReadArg<GuestVoidPtr>(ARG1);
            ulong initialValue = ppu.ReadArg<ulong>(ARG2);

            LV2EventFlag eventFlag = new(initialValue, _memoryManager);
            AddEventFlag(eventFlag.Handle, eventFlag);

            _memoryManager.Write<BigEndian<uint>>(id_ptr, eventFlag.Handle);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_create id = 0x{eventFlag.Handle:X8}");
            return CELL_OK;
        }

        internal ulong SysEventFlagDestroy(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            RemoveEventFlag(id);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_destroy id = 0x{id:X8}");
            return CELL_OK;
        }

        internal ulong SysEventFlagWait(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong bitPattern = ppu.ReadArg<ulong>(ARG1);
            uint mode = ppu.ReadArg<uint>(ARG2);
            GuestPtr<ulong> result_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG3);
            ulong timeout_us = ppu.ReadArg<ulong>(ARG4);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_wait id = 0x{id:X8} mode = 0x{mode:X8} bp = 0x{bitPattern:X8}");

            if (TryGetEventFlag(id, out LV2EventFlag? eventFlag))
            {
                ulong errorCode = eventFlag.Wait(ppu, (SysEventFlagMode)mode, bitPattern, timeout_us, out ulong result);

                //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_wait: woke up - returned: 0x{result:X8}");

                if (errorCode == CELL_OK)
                {
                    _memoryManager.Write<BigEndian<ulong>>(result_ptr, result);
                }

                return errorCode;

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal ulong SysEventFlagSet(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong bitPattern = ppu.ReadArg<ulong>(ARG1);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_set id = 0x{id:X} bp = 0x{bitPattern:X8}");

            if (TryGetEventFlag(id, out LV2EventFlag? eventFlag))
            {
                eventFlag.Set(bitPattern);

            }
            else
            {
                return CELL_ESRCH;
            }

            return CELL_OK;
        }

        internal ulong SysEventFlagClear(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong bitPattern = ppu.ReadArg<ulong>(ARG1);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_clear id = 0x{id:X} bp = 0x{bitPattern:X8}");

            if (TryGetEventFlag(id, out LV2EventFlag? eventFlag))
            {
                eventFlag.Clear(bitPattern);

            }
            else
            {
                return CELL_ESRCH;
            }

            return CELL_OK;
        }

        internal bool TryGetEventFlag(uint handle, out LV2EventFlag? eventFlag) => _eventFlags.TryGetValue(handle, out eventFlag);

        private void AddEventFlag(uint handle, LV2EventFlag eventFlag)
        {
            bool success = _eventFlags.TryAdd(handle, eventFlag);

            if (!success)
            {
                throw new Exception($"[SysEventFlag] Could not add event flag: 0x{handle:X8} to dictionary");
            }
        }

        private void RemoveEventFlag(uint handle)
        {
            bool success = _eventFlags.TryRemove(handle, out _);

            if (!success)
            {
                throw new Exception($"[SysEventFlag] Could not remove event flag: 0x{handle:X8} from dictionary");
            }
        }
    }
}
