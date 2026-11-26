using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Cond;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysCond
    {
        private readonly MemoryManager _memoryManager;
        private readonly SysMutex _sysMutex;
        private readonly ConcurrentDictionary<uint, LV2Cond> _conds = [];

        internal SysCond(MemoryManager memoryManager, SysMutex sysMutex)
        {
            _memoryManager = memoryManager;
            _sysMutex = sysMutex;
        }

        internal ulong SysCondCreate(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> cond_id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            uint mutex_id = ppu.ReadArg<uint>(Arg1);
            GuestPtr<SysCondAttribute> attribute_ptr = ppu.ReadArg<GuestPtr<SysCondAttribute>>(Arg2);

            // Get the requested sys_mutex 
            if (!_sysMutex.TryGetMutex(mutex_id, out LV2Mutex? mutex))
            {
                return CellESrch;
            }

            // Get the attribute
            SysCondAttribute attribute = _memoryManager.Read<SysCondAttribute>(attribute_ptr);

            // Create and add the cond
            LV2Cond cond = new(attribute, mutex, _memoryManager);
            AddCond(cond);

            // Write the cond id
            _memoryManager.Write<BigEndian<uint>>(cond_id_ptr, cond.Handle);

            return CellOk;
        }

        internal ulong SysCondDestroy(PPUThread ppu)
        {
            uint cond_id = ppu.ReadArg<uint>(Arg0);

            RemoveCond(cond_id);
            return CellOk;
        }

        internal ulong SysCondWait(PPUThread ppu)
        {
            uint cond_id = ppu.ReadArg<uint>(Arg0);
            ulong us_timeout = ppu.ReadArg<ulong>(Arg1);

            if (TryGetCond(cond_id, out LV2Cond? cond))
            {
                return cond.Wait(ppu, us_timeout);
            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysCondSignal(PPUThread ppu)
        {
            uint cond_id = ppu.ReadArg<uint>(Arg0);


            if (TryGetCond(cond_id, out LV2Cond? cond))
            {
                return cond.Signal();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysCondSignalAll(PPUThread ppu)
        {
            uint cond_id = ppu.ReadArg<uint>(Arg0);

            if (TryGetCond(cond_id, out LV2Cond? cond))
            {
                return cond.SignalAll();

            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysCondSignalTo(PPUThread ppu)
        {
            uint cond_id = ppu.ReadArg<uint>(Arg0);
            uint thread_id = ppu.ReadArg<uint>(Arg1);

            if (TryGetCond(cond_id, out LV2Cond? cond))
            {
                return cond.SignalTo(thread_id);

            }
            else
            {
                return CellESrch;
            }
        }

        internal bool TryGetCond(uint cond_id, [NotNullWhen(true)] out LV2Cond? cond) => _conds.TryGetValue(cond_id, out cond);

        private void AddCond(LV2Cond cond)
        {
            bool success = _conds.TryAdd(cond.Handle, cond);
            if (!success)
            {
                throw new Exception($"[SysCond] Could not add cond 0x{cond.Handle:X8} to dictionary");
            }
        }

        private void RemoveCond(uint cond_id)
        {
            bool success = _conds.TryRemove(cond_id, out _);
            if (!success)
            {
                throw new Exception($"[SysCond] Could not remove cond 0x{cond_id:X8} from dictionary");
            }
        }
    }
}
