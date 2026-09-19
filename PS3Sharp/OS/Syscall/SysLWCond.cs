using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall {
    public static class SysLWCond {     // This is incomplete 


        private static readonly ConcurrentDictionary<uint, LV2LWCond> _conds = [];
        public static bool GetLWCond(uint cond_id, out LV2LWCond cond) => _conds.TryGetValue(cond_id, out cond);

        public static void AddLWCond(uint id, LV2LWCond cond) {
            bool success = _conds.TryAdd(id, cond);
            if (!success) {
                throw new Exception($"[SysLWCond] Could not add cond 0x{id:X8} to dictionary");
            }
        }

        public static void RemoveLWCond(uint cond_id) {
            bool success = _conds.TryRemove(cond_id, out _);
            if (!success) {
                throw new Exception($"[SysLWCond] Could not remove cond 0x{cond_id:X8} from dictionary");
            }
        }

        public static ulong SysLWCondCreate(PPU ppu) {
            uint handle = MemoryManager.GetHandle();

            GuestPtr<uint> id_ptr = ppu.ReadArg<uint>(ARG0);
            //uint lwmutexId = ppu.ReadArg<uint>(ARG1);           // do we use this to bind ??

            //GuestPtr<LV2LWCond.SysLWCond> control_ptr = ppu.ReadArg<GuestPtr<LV2LWCond.SysLWCond>>(ARG2);
            //ulong name = ppu.ReadArg<ulong>(ARG3);


            //// Copy name
            //LV2LWCond.SysLWCondAttribute attribute = new LV2LWCond.SysLWCondAttribute();
            //Span<byte> nameDest = attribute.Name;
            //Span<byte> nameSource = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref name, 1));
            //nameSource.CopyTo(nameDest);

            //// Create the object
            //ref LV2LWCond.SysLWCond control = ref control_ptr.AsRef();
            //control.LWCondQueue = handle;

            //LV2LWCond lv2lwcond = new LV2LWCond(control_ptr, attribute);
            //AddLWCond(handle, lv2lwcond);

            MemoryManager.Write<uint>(id_ptr, handle);
            return CELL_OK;
        }


    }
}
