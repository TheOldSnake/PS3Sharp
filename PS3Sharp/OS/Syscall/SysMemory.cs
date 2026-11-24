using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysMemory
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysMemoryInfo : IPS3Struct
        {
            internal BigEndian<uint> TotalUserMemory;
            internal BigEndian<uint> AvailableUserMemory;
        }

        private readonly MemoryManager _memoryManager;

        internal SysMemory(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysMemoryAllocate(PPUThread ppu)
        {
            //int sys_memory_allocate(size_t size, uint64_t flags, sys_addr_t * alloc_addr); 

            uint size = ppu.ReadArg<uint>(Arg0);
            ulong flags = ppu.ReadArg<ulong>(Arg1);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(Arg2);

            uint alignment = (uint)(flags == 0x400 ? 1 * 1024 * 1024 : 64 * 1024);

            Console.WriteLine($"[SysMemory] sys_memory_allocate: size = {size:X16}, flags = {flags:X16}, alloc_addr = {alloc_addr:X8}");

            if (size > 0 && !alloc_addr.IsNullPtr)
            {
                MemoryManager.MemoryBlock block = _memoryManager.Allocator.AllocateVirtualMemory(size, 0);
                Console.WriteLine($"[SysMemory] sys_memory_allocate: [0x{block.VirtualAddress:X8} - 0x{block.End:X8}]");

                //Write the handle at the alloc_addr*
                _memoryManager.Write<BigEndian<uint>>(alloc_addr, block.VirtualAddress);
            }
            else
            {
                Console.WriteLine($"[SysMemory] sys_memory_allocate size == 0");
            }

            return CellOk;
        }

        internal ulong SysMemoryFree(PPUThread ppu)
        {
            GuestVoidPtr ptr = ppu.ReadArg<GuestVoidPtr>(Arg0);

            if (!ptr.IsNullPtr)
            {
                _memoryManager.Allocator.FreeByVirtualAddress(ptr);
            }

            Console.WriteLine($"[SysMemory] sys_Memory_Free");
            return CellOk;
        }

        internal ulong SysMemoryGetUserMemorySize(PPUThread ppu)
        {
            GuestPtr<SysMemoryInfo> memory_info_ptr = ppu.ReadArg<GuestPtr<SysMemoryInfo>>(Arg0);

            uint total_size = MemoryConstants.RamSize;

            SysMemoryInfo memory_info = new()
            {
                TotalUserMemory = total_size,
                AvailableUserMemory = total_size //stub
            };

            _memoryManager.Write(memory_info_ptr, memory_info);
            Console.WriteLine($"[SysMemory] sys_memory_get_user_memory_size");
            return CellOk;
        }
    }
}