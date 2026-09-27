using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2EventFlag;

namespace PS3Sharp.OS.Syscall
{
    internal static class SysEventFlag
    {
        private static readonly ConcurrentDictionary<uint, LV2EventFlag> _eventFlags = [];

        internal static bool GetEventFlag(uint handle, out LV2EventFlag? eventFlag) => _eventFlags.TryGetValue(handle, out eventFlag);

        internal static void AddEventFlag(uint handle, LV2EventFlag eventFlag)
        {
            bool success = _eventFlags.TryAdd(handle, eventFlag);

            if (!success)
            {
                throw new Exception($"[SysEventFlag] Could not add event flag: 0x{handle:X8} to dictionary");
            }
        }

        internal static void RemoveEventFlag(uint handle)
        {
            bool success = _eventFlags.TryRemove(handle, out _);

            if (!success)
            {
                throw new Exception($"[SysEventFlag] Could not remove event flag: 0x{handle:X8} from dictionary");
            }
        }

        internal static ulong SysEventFlagCreate(PPU ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestVoidPtr attributes_ptr = ppu.ReadArg<GuestVoidPtr>(ARG1);
            ulong initialValue = ppu.ReadArg<ulong>(ARG2);

            LV2EventFlag eventFlag = new(initialValue);
            AddEventFlag(eventFlag.Handle, eventFlag);

            MemoryManager.Write<uint>(id_ptr, eventFlag.Handle);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_create id = 0x{eventFlag.Handle:X8}");
            return CELL_OK;
        }

        internal static ulong SysEventFlagDestroy(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            RemoveEventFlag(id);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_destroy id = 0x{id:X8}");
            return CELL_OK;
        }

        internal static ulong SysEventFlagWait(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong bitPattern = ppu.ReadArg<ulong>(ARG1);
            uint mode = ppu.ReadArg<uint>(ARG2);
            GuestPtr<ulong> result_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG3);
            ulong timeout_us = ppu.ReadArg<ulong>(ARG4);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_wait id = 0x{id:X8} mode = 0x{mode:X8} bp = 0x{bitPattern:X8}");

            if (GetEventFlag(id, out LV2EventFlag? eventFlag))
            {
                ulong errorCode = eventFlag.Wait((SysEventFlagMode)mode, bitPattern, timeout_us, out ulong result);

                //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_wait: woke up - returned: 0x{result:X8}");

                if (errorCode == CELL_OK)
                {
                    MemoryManager.Write<ulong>(result_ptr, result);
                }

                return errorCode;

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysEventFlagSet(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong bitPattern = ppu.ReadArg<ulong>(ARG1);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_set id = 0x{id:X} bp = 0x{bitPattern:X8}");

            if (GetEventFlag(id, out LV2EventFlag? eventFlag))
            {
                eventFlag.Set(bitPattern);

            }
            else
            {
                return CELL_ESRCH;
            }

            return CELL_OK;
        }

        internal static ulong SysEventFlagClear(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            ulong bitPattern = ppu.ReadArg<ulong>(ARG1);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_flag_clear id = 0x{id:X} bp = 0x{bitPattern:X8}");

            if (GetEventFlag(id, out LV2EventFlag? eventFlag))
            {
                eventFlag.Clear(bitPattern);

            }
            else
            {
                return CELL_ESRCH;
            }

            return CELL_OK;
        }
    }
}
