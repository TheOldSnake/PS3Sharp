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

            uint size = ppu.ReadArg<uint>(ARG0);
            ulong flags = ppu.ReadArg<ulong>(ARG1);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(ARG2);

            uint alignment = (uint)(flags == 0x400 ? 1 * 1024 * 1024 : 64 * 1024);

            Console.WriteLine($"[SysMemory] sys_memory_allocate: size = {size:X16}, flags = {flags:X16}, alloc_addr = {alloc_addr:X8}");

            if (size > 0 && !alloc_addr.IsNullPtr)
            {
                MemoryManager.MemoryBlock block = _memoryManager.AllocateVirtualMemory(size, 0);
                Console.WriteLine($"[SysMemory] sys_memory_allocate: [0x{block.VirtualAddress:X8} - 0x{block.End:X8}]");

                //Write the handle at the alloc_addr*
                _memoryManager.Write<uint>(alloc_addr, (uint)block.VirtualAddress);
            }
            else
            {
                Console.WriteLine($"[SysMemory] sys_memory_allocate size == 0");
            }

            return CELL_OK;
        }

        internal ulong SysMemoryFree(PPUThread ppu)
        {
            GuestVoidPtr ptr = ppu.ReadArg<GuestVoidPtr>(ARG0);

            if (!ptr.IsNullPtr)
            {
                _memoryManager.FreeByVirtualAddress(ptr);
            }

            Console.WriteLine($"[SysMemory] sys_Memory_Free");
            return CELL_OK;
        }

        internal ulong SysMemoryGetUserMemorySize(PPUThread ppu)
        {
            GuestPtr<SysMemoryInfo> memory_info_ptr = ppu.ReadArg<GuestPtr<SysMemoryInfo>>(ARG0);

            uint total_size = MemoryManager.RAM_SIZE;

            unsafe
            {
                SysMemoryInfo* memory_info = _memoryManager.GetPtr(memory_info_ptr);
                memory_info->TotalUserMemory = total_size;
                memory_info->AvailableUserMemory = total_size; //stub
            }

            Console.WriteLine($"[SysMemory] sys_memory_get_user_memory_size");
            return CELL_OK;
        }
    }
}
