using PS3Sharp.Cell.PPU;
using System;
using System.Collections.Generic;
using System.Text;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Cell.Arguments;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;

namespace PS3Sharp.OS.Syscall
{
    public static class SysVm
    {
        public static ulong SysVmMemoryMap(PPU ppu)
        {
            // TODO implement properly 
            // For now we just do a normal allocation

            ulong vsize = ppu.ReadArg<ulong>(ARG0);
            ulong psize = ppu.ReadArg<ulong>(ARG1);
            GuestPtr<uint> addr_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG5);

            MemoryManager.MemoryBlock block = MemoryManager.AllocateVirtualMemory((uint)psize);
            MemoryManager.Write<uint>(addr_ptr, block.VirtualAddress);

            return CELL_OK;
        }
    }
}
