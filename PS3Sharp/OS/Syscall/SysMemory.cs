using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall {
    public static class SysMemory {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysMemoryInfo : IPS3Struct {
            public BigEndian<uint> TotalUserMemory;
            public BigEndian<uint> AvailableUserMemory;
        }

        public static ulong SysMemoryAllocate(PPU ppu) {
            //int sys_memory_allocate(size_t size, uint64_t flags, sys_addr_t * alloc_addr); 

            size_t size = ppu.ReadArg<size_t>(ARG0);
            ulong flags = ppu.ReadArg<ulong>(ARG1);
            GuestVoidPtr alloc_addr = ppu.ReadArg<GuestVoidPtr>(ARG2);

            uint alignment = (uint)(flags == 0x400 ? 1 * 1024 * 1024 : 64 * 1024);

            Console.WriteLine($"[SysMemory] sys_memory_allocate: size = {size:X16}, flags = {flags:X16}, alloc_addr = {alloc_addr:X8}");
            
            if (size > 0 && !alloc_addr.IsNullPtr) {
                MemoryManager.MemoryBlock block = MemoryManager.AllocateVirtualMemory(size, 0);
                Console.WriteLine($"[SysMemory] sys_memory_allocate: [0x{block.VirtualAddress:X8} - 0x{block.End:X8}]");

                //Write the handle at the alloc_addr*
                MemoryManager.Write<uint>(alloc_addr, (uint)block.VirtualAddress);
            } else {
                Console.WriteLine($"[SysMemory] sys_memory_allocate size == 0");
            }

            return CELL_OK;
        }

        public static ulong SysMemoryFree(PPU ppu) {
            GuestVoidPtr ptr = ppu.ReadArg<GuestVoidPtr>(ARG0);

            if(!ptr.IsNullPtr) {
                MemoryManager.FreeByVirtualAddress(ptr);
            }

            Console.WriteLine($"[SysMemory] sys_Memory_Free");
            return CELL_OK;
        }

        public static ulong SysMemoryGetUserMemorySize(PPU ppu) {
            GuestPtr<SysMemoryInfo> memory_info_ptr = ppu.ReadArg<GuestPtr<SysMemoryInfo>>(ARG0);

            uint total_size = MemoryManager.RAM_SIZE;

            unsafe {
                SysMemoryInfo* memory_info = memory_info_ptr.AsNativePtr();
                memory_info->TotalUserMemory = total_size;
                memory_info->AvailableUserMemory = total_size; //stub
            }

            Console.WriteLine($"[SysMemory] sys_memory_get_user_memory_size");
            return CELL_OK;
        }
    }
}
