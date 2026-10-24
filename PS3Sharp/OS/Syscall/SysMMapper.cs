using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Memory.MemoryManager;

namespace PS3Sharp.Syscall
{
    internal class SysMMapper
    {
        // sys_mmapper syscalls
        // not to be confused with sys_mmapper functions in sysPrxForUser

        private readonly MemoryManager _memoryManager;

        internal SysMMapper(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysMMapperAllocateAddress(PPUThread ppu)
        {
            //int sys_mmapper_allocate_address(size_t size, uint64_t flags, size_t alignment, sys_addr_t * alloc_addr); 

            //Allocates virtual memory, writes the virtual address at alloc_addr

            uint size = ppu.ReadArg<uint>(Arg0);
            ulong flags = ppu.ReadArg<ulong>(Arg1);
            uint alignment = ppu.ReadArg<uint>(Arg2);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(Arg3);

            Console.WriteLine($"sys_mmapper_allocate_address: size = {size:X16}, flags = {flags:X16}, alignment = {alignment:X16}, alloc_addr = {alloc_addr:X8}");

            if (size > 0 && !alloc_addr.IsNullPtr)
            {
                MemoryBlock block = _memoryManager.AllocateVirtualMemory(size, alignment);

                //Write the handle at the alloc_addr*
                _memoryManager.Write<BigEndian<uint>>(alloc_addr.Address, block.VirtualAddress);
            }
            else
            {
                Console.WriteLine($"sys_mmapper_allocate_address size == 0");
            }

            return CellOk;
        }

        internal ulong SysMMapperFreeAddress(PPUThread ppu)
        {
            GuestVoidPtr addr = ppu.ReadArg<GuestVoidPtr>(Arg0);
            if (!addr.IsNullPtr)
            {
                _memoryManager.FreeByVirtualAddress(addr.Address);
            }
            return CellOk;
        }

        internal ulong SysMMapperAllocateMemory(PPUThread ppu)
        {
            ulong ipc_key = ppu.ReadArg<ulong>(Arg0);
            ulong size = ppu.ReadArg<ulong>(Arg1);
            ulong flags = ppu.ReadArg<ulong>(Arg2);
            GuestPtr<BigEndian<uint>> handle_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg3);

            // todo: check if we have enough memory

            MemoryBlock block = _memoryManager.AllocatePhysicalMemory((uint)size);
            _memoryManager.Write<BigEndian<uint>>(handle_ptr, block.Handle);

            return CellOk;
        }

        internal ulong SysMMapperSearchAndMap(PPUThread ppu)
        {
            //Search for block by Handle (id) and remap it

            GuestVoidPtr start_addr = ppu.ReadArg<GuestVoidPtr>(Arg0);
            ulong mem_id = ppu.ReadArg<ulong>(Arg1);
            ulong flags = ppu.ReadArg<ulong>(Arg2);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(Arg3);

            uint newAddress = _memoryManager.RemapByHandle((uint)mem_id, start_addr.Address);
            _memoryManager.Write<BigEndian<uint>>(alloc_addr.Address, newAddress);

            Console.WriteLine($"Sys_MMapper_Search_And_Map: start_addr = {start_addr:X8}, mem_id = {mem_id:X8}, flags = {flags:X16}, alloc_addr = {alloc_addr:X8}");
            return CellOk;
        }

        internal ulong SysMMapperMapSharedMemory(PPUThread ppu)
        {
            uint address = ppu.ReadArg<uint>(Arg0);
            uint handle = ppu.ReadArg<uint>(Arg1);
            ulong flags = ppu.ReadArg<ulong>(Arg2);

            // todo

            return CellOk;
        }
    }
}
