using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysLWCond
    {     // This is incomplete 


        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2LWCond> _conds;

        internal SysLWCond(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysLWCondCreate(PPUThread ppu)
        {
            uint handle = _memoryManager.GetHandle();

            GuestPtr<uint> id_ptr = ppu.ReadArg<uint>(Arg0);
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

            _memoryManager.Write<BigEndian<uint>>(id_ptr, handle);
            return CellOk;
        }

        internal bool TryGetLWCond(uint cond_id, [NotNullWhen(true)] out LV2LWCond? cond) => _conds.TryGetValue(cond_id, out cond);

        private void AddLWCond(uint id, LV2LWCond cond)
        {
            bool success = _conds.TryAdd(id, cond);
            if (!success)
            {
                throw new Exception($"[SysLWCond] Could not add cond 0x{id:X8} to dictionary");
            }
        }

        private void RemoveLWCond(uint cond_id)
        {
            bool success = _conds.TryRemove(cond_id, out _);
            if (!success)
            {
                throw new Exception($"[SysLWCond] Could not remove cond 0x{cond_id:X8} from dictionary");
            }
        }
    }
}
