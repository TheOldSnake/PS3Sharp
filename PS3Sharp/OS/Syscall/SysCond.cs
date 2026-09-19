using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Cond;

namespace PS3Sharp.OS.Syscall {
    public static class SysCond {
        private static readonly ConcurrentDictionary<uint, LV2Cond> _conds = [];
        public static bool GetCond(uint cond_id, out LV2Cond cond) => _conds.TryGetValue(cond_id, out cond);

        public static void AddCond(LV2Cond cond) {
            bool success = _conds.TryAdd(cond.Handle, cond);
            if (!success) {
                throw new Exception($"[SysCond] Could not add cond 0x{cond.Handle:X8} to dictionary");
            }
        }

        public static void RemoveCond(uint cond_id) {
            bool success = _conds.TryRemove(cond_id, out _);
            if (!success) {
                throw new Exception($"[SysCond] Could not remove cond 0x{cond_id:X8} from dictionary");
            }
        }

        public static ulong SysCondCreate(PPU ppu) {
            GuestPtr<uint> cond_id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            uint mutex_id = ppu.ReadArg<uint>(ARG1);
            GuestPtr<SysCondAttribute> attribute_ptr = ppu.ReadArg<GuestPtr<SysCondAttribute>>(ARG2);

            // Get the requested sys_mutex 
            if (!SysMutex.GetMutex(mutex_id, out LV2Mutex? mutex)) {
                return CELL_ESRCH;
            }

            // Get the attribute
            SysCondAttribute attribute = attribute_ptr.Read();

            // Create and add the cond
            LV2Cond cond = new LV2Cond(attribute, mutex);
            AddCond(cond);

            // Write the cond id
            MemoryManager.Write<uint>(cond_id_ptr, cond.Handle);

            return CELL_OK;
        }

        public static ulong SysCondDestroy(PPU ppu) { 
            uint cond_id = ppu.ReadArg<uint>(ARG0);

            RemoveCond(cond_id);
            return CELL_OK;
        }

        public static ulong SysCondWait(PPU ppu) { 
            uint cond_id = ppu.ReadArg<uint>(ARG0);
            ulong us_timeout = ppu.ReadArg<ulong>(ARG1);

            if (GetCond(cond_id, out LV2Cond cond)) {
                return cond.Wait(ppu.ThreadID, us_timeout);

            } else {
                return CELL_ESRCH;
            }
        }

        public static ulong SysCondSignal(PPU ppu) { 
            uint cond_id = ppu.ReadArg<uint>(ARG0);


            if (GetCond(cond_id, out LV2Cond cond)) {
                return cond.Signal();

            } else {
                return CELL_ESRCH;
            }
        }

        public static ulong SysCondSignalAll(PPU ppu) { 
            uint cond_id = ppu.ReadArg<uint>(ARG0);

            if (GetCond(cond_id, out LV2Cond cond)) {
                return cond.SignalAll();

            } else {
                return CELL_ESRCH;
            }
        }

        public static ulong SysCondSignalTo(PPU ppu) { 
            uint cond_id = ppu.ReadArg<uint>(ARG0);
            uint thread_id = ppu.ReadArg<uint>(ARG1);

            if (GetCond(cond_id, out LV2Cond cond)) {
                return cond.SignalTo(thread_id);

            } else {
                return CELL_ESRCH;
            }
        }
    }
}
