using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Memory_Manager.MemoryManager;

namespace PS3Sharp.Syscall
{
    public static class SysMMapper
    {
        //sys_mmapper syscalls
        //not to be confused with sys_mmapper functions in sysPrxForUser

        public static ulong SysMMapperAllocateAddress(PPU ppu)
        {
            //int sys_mmapper_allocate_address(size_t size, uint64_t flags, size_t alignment, sys_addr_t * alloc_addr); 

            //Allocates virtual memory, writes the virtual address at alloc_addr

            size_t size = ppu.ReadArg<size_t>(ARG0);
            ulong flags = ppu.ReadArg<ulong>(ARG1);
            size_t alignment = ppu.ReadArg<size_t>(ARG2);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(ARG3);

            Console.WriteLine($"sys_mmapper_allocate_address: size = {size:X16}, flags = {flags:X16}, alignment = {alignment:X16}, alloc_addr = {alloc_addr:X8}");

            if (size > 0 && !alloc_addr.IsNullPtr)
            {
                MemoryBlock block = MemoryManager.AllocateVirtualMemory(size, alignment);

                //Write the handle at the alloc_addr*
                MemoryManager.Write<uint>(alloc_addr.Address, (uint)block.VirtualAddress);
            }
            else
            {
                Console.WriteLine($"sys_mmapper_allocate_address size == 0");
            }

            return CELL_OK;
        }

        public static ulong SysMMapperFreeAddress(PPU ppu)
        {
            GuestVoidPtr addr = ppu.ReadArg<GuestVoidPtr>(ARG0);
            if (!addr.IsNullPtr)
            {
                MemoryManager.FreeByVirtualAddress(addr.Address);
            }
            return CELL_OK;
        }

        public static ulong SysMMapperAllocateMemory(PPU ppu)
        {
            ulong ipc_key = ppu.ReadArg<ulong>(ARG0);
            ulong size = ppu.ReadArg<ulong>(ARG1);
            ulong flags = ppu.ReadArg<ulong>(ARG2);
            GuestPtr<uint> handle_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG3);

            // todo: check if we have enough memory

            MemoryBlock block = MemoryManager.AllocatePhysicalMemory((uint)size);
            MemoryManager.Write<uint>(handle_ptr, block.Handle);

            return CELL_OK;
        }

        public static ulong SysMMapperSearchAndMap(PPU ppu)
        {
            //Search for block by Handle (id) and remap it

            GuestVoidPtr start_addr = ppu.ReadArg<GuestVoidPtr>(ARG0);
            ulong mem_id = ppu.ReadArg<ulong>(ARG1);
            ulong flags = ppu.ReadArg<ulong>(ARG2);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(ARG3);

            uint newAddress = MemoryManager.RemapByHandle((uint)mem_id, start_addr.Address);
            MemoryManager.Write<uint>(alloc_addr.Address, newAddress);

            Console.WriteLine($"Sys_MMapper_Search_And_Map: start_addr = {start_addr:X8}, mem_id = {mem_id:X8}, flags = {flags:X16}, alloc_addr = {alloc_addr:X8}");
            return CELL_OK;
        }

        public static ulong SysMMapperMapSharedMemory(PPU ppu)
        {
            uint address = ppu.ReadArg<uint>(ARG0);
            uint handle = ppu.ReadArg<uint>(ARG1);
            ulong flags = ppu.ReadArg<ulong>(ARG2);

            // todo

            return CELL_OK;
        }
    }
}
