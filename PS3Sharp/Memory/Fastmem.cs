using System.Runtime.InteropServices;
using static PS3Sharp.Memory.MemoryManager;

namespace PS3Sharp.Memory
{
    internal static unsafe class Fastmem
    {
        internal const uint PAGE_BITS = 16;
        internal const uint PAGE_SHIFT = (32 - PAGE_BITS);
        internal const uint PAGE_COUNT = 1 << (int)PAGE_BITS;
        internal const uint PAGE_SIZE = 1 << (int)PAGE_SHIFT;
        internal const uint PAGE_MASK = PAGE_SIZE - 1;

        // Everything from here is mostly unused 

        //Size (in bytes) of a full page table where each entry is a pointer-sized value
        private static readonly uint PageTableSizeBytes = (uint)(PAGE_SIZE * nuint.Size);

        // Page table pointer
        private static byte** PageTable = null;


        // -- Unused --
        internal static void AllocatePageTable()
        {
            PageTable = (byte**)NativeMemory.AllocZeroed(PageTableSizeBytes);
        }

        // -- Unused --
        private static bool TrySetFastmem(ref MemoryBlock memoryBlock, MemoryManager memoryManager)
        {
            uint startPage = memoryBlock.VirtualAddress >> (int)PAGE_SHIFT;
            uint numberOfPages = memoryBlock.Size / PAGE_SIZE;
            uint endPage = startPage + numberOfPages;

            if (memoryBlock.Size % PAGE_SIZE != 0)
            {
                throw new Exception($"Size is not aligned"); // Should never happen
                //return false;
            }

            uint pageIndex = 0;
            for (uint i = startPage; i < endPage; i++)
            {
                PageTable[i] = (byte*)memoryBlock.NativeAddress + (pageIndex * PAGE_SIZE);
                memoryManager.SetMemoryBlock(ref memoryBlock, (int)i);
                pageIndex++;
            }

            return true;
        }

        // -- Unused --
        private static void UnsetFastmem(ref MemoryBlock memoryBlock, MemoryManager memoryManager)
        {
            uint startPage = memoryBlock.VirtualAddress >> (int)PAGE_SHIFT;
            uint numberOfPages = memoryBlock.Size / PAGE_SIZE;
            uint endPage = startPage + numberOfPages;

            for (uint i = startPage; i < endPage; i++)
            {
                PageTable[i] = null;
                memoryManager.UnsetMemoryBlock((int)i);
            }
        }


        // -- Unused --
        private static void* GetNativeAddress(uint virtualAddress, MemoryManager memoryManager)
        {
            // Get the page from the page table
            uint page = virtualAddress >> (int)PAGE_SHIFT;
            byte* pageStart = PageTable[page];
            uint inPageOffset = virtualAddress & PAGE_MASK;

            if (pageStart != null)
            {
                return pageStart + inPageOffset;
            }
            else
            {
                // if the page address is null then crash
                throw new PageFaultException(virtualAddress);
            }

            // slow path (should not happen because we align allocations to page size)
            MemoryBlock block = memoryManager.GetAllocationByVirtualAddress(virtualAddress);
            ulong offset = virtualAddress - block.VirtualAddress;
            return (byte*)block.NativeAddress + offset;
        }
    }
}
