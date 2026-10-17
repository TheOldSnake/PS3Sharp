using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysVm
    {
        private readonly MemoryManager _memoryManager;

        internal SysVm(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysVmMemoryMap(PPUThread ppu)
        {
            // TODO implement properly 
            // For now we just do a normal allocation

            ulong vsize = ppu.ReadArg<ulong>(ARG0);
            ulong psize = ppu.ReadArg<ulong>(ARG1);
            GuestPtr<uint> addr_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG5);

            MemoryManager.MemoryBlock block = _memoryManager.AllocateVirtualMemory((uint)psize);
            _memoryManager.Write<BigEndian<uint>>(addr_ptr, block.VirtualAddress);

            return CELL_OK;
        }
    }
}
