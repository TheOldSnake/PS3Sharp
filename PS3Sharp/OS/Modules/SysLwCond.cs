using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class SysLwCond
    { 
        // HLE implementation for the module 
        // TODO: Implement the syscalls

        private readonly MemoryManager _memoryManager;
        private readonly SysLwMutex _sysLwMutex;
        private readonly ConcurrentDictionary<uint, LV2LwCond> _conds = [];

        internal SysLwCond(MemoryManager memoryManager, SysLwMutex lwMutexHandler)
        {
            _memoryManager = memoryManager;
            _sysLwMutex = lwMutexHandler;
        }

        internal ulong SysLwCondCreate(PPUThread ppu)
        {
            GuestPtr<LV2LwCond.LwCond> lwcondPtr = ppu.ReadArg<GuestPtr<LV2LwCond.LwCond>>(Arg0);
            GuestPtr<LV2LwMutex.LwMutex> lwmutexPtr = ppu.ReadArg<GuestPtr<LV2LwMutex.LwMutex>>(Arg1);
            GuestPtr<LV2LwCond.LWCondAttribute> attributePtr = ppu.ReadArg<GuestPtr<LV2LwCond.LWCondAttribute>>(Arg2);

            LV2LwCond.LWCondAttribute attribute = _memoryManager.Read<LV2LwCond.LWCondAttribute>(attributePtr);
            LV2LwCond lv2lwcond = new(lwcondPtr, lwmutexPtr, attribute, _sysLwMutex, _memoryManager);

            // Lv2 struct for the lwcond
            LV2LwCond.LwCond lwcond = new()
            {
                LwMutex = lwmutexPtr,
                Id = lv2lwcond.Handle
            };

            AddLwCond(lv2lwcond);
            _memoryManager.Write(lwcondPtr, lwcond);

            return CellOk;
        }

        internal ulong SysLwCondWait(PPUThread ppu)
        {
            GuestPtr<LV2LwCond.LwCond> lwcondPtr = ppu.ReadArg<GuestPtr<LV2LwCond.LwCond>>(Arg0);
            ulong timeout = _memoryManager.Read<ulong>(Arg1);

            LV2LwCond.LwCond lwcond = _memoryManager.Read<LV2LwCond.LwCond>(lwcondPtr);
            uint id = lwcond.Id;

            if (TryGetLwCond(id, out LV2LwCond? cond))
            {
                return cond.Wait(ppu, timeout);
            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysLwCondSignal(PPUThread ppu)
        {
            GuestPtr<LV2LwCond.LwCond> lwcondPtr = ppu.ReadArg<GuestPtr<LV2LwCond.LwCond>>(Arg0);

            LV2LwCond.LwCond lwcond = _memoryManager.Read<LV2LwCond.LwCond>(lwcondPtr);
            uint id = lwcond.Id;

            if (TryGetLwCond(id, out LV2LwCond? cond))
            {
                return cond.Signal();
            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysLwCondSignalAll(PPUThread ppu)
        {
            GuestPtr<LV2LwCond.LwCond> lwcondPtr = ppu.ReadArg<GuestPtr<LV2LwCond.LwCond>>(Arg0);

            LV2LwCond.LwCond lwcond = _memoryManager.Read<LV2LwCond.LwCond>(lwcondPtr);
            uint id = lwcond.Id;

            if (TryGetLwCond(id, out LV2LwCond? cond))
            {
                return cond.SignalAll();
            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysLwCondDestroy(PPUThread ppu)
        {
            GuestPtr<LV2LwCond.LwCond> lwcondPtr = ppu.ReadArg<GuestPtr<LV2LwCond.LwCond>>(Arg0);

            LV2LwCond.LwCond lwcond = _memoryManager.Read<LV2LwCond.LwCond>(lwcondPtr);
            uint id = lwcond.Id;

            if (!TryGetLwCond(id, out LV2LwCond? cond))
            {
                return CellESrch;
            }

            RemoveLwCond(id);
            _memoryManager.Write<LV2LwCond.LwCond>(lwcondPtr, default);

            return CellOk;
        }

        internal bool TryGetLwCond(uint id, [NotNullWhen(true)] out LV2LwCond? cond) => _conds.TryGetValue(id, out cond);

        private void AddLwCond(LV2LwCond cond)
        {
            if (!_conds.TryAdd(cond.Handle, cond))
            {
                throw new Exception($"[SysLWCond] Could not add cond 0x{cond.Handle:X8} to dictionary");
            }
        }

        private void RemoveLwCond(uint cond_id)
        {
            if (!_conds.TryRemove(cond_id, out _))
            {
                throw new Exception($"[SysLWCond] Could not remove cond 0x{cond_id:X8} from dictionary");
            }
        }
    }
}
