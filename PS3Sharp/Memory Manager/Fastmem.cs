using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Memory_Manager.MemoryManager;

namespace PS3Sharp.Memory_Manager
{
    public static unsafe class Fastmem
    {
        public const uint PAGE_BITS = 16;
        public const uint PAGE_SHIFT = (32 - PAGE_BITS);
        public const uint PAGE_COUNT = 1 << (int)PAGE_BITS;
        public const uint PAGE_SIZE = 1 << (int)PAGE_SHIFT;
        public const uint PAGE_MASK = PAGE_SIZE - 1;

        // Everything from here is mostly unused 

        //Size (in bytes) of a full page table where each entry is a pointer-sized value
        private static readonly uint PageTableSizeBytes = (uint)(PAGE_SIZE * nuint.Size);

        // Page table pointer
        private static byte** PageTable = null;


        // -- Unused --
        public static void AllocatePageTable()
        {
            PageTable = (byte**)NativeMemory.AllocZeroed(PageTableSizeBytes);
        }

        // -- Unused --
        private static bool TrySetFastmem(ref MemoryBlock memoryBlock)
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
                MemoryManager.SetMemoryBlock(ref memoryBlock, (int)i);
                pageIndex++;
            }

            return true;
        }

        // -- Unused --
        private static void UnsetFastmem(ref MemoryBlock memoryBlock)
        {
            uint startPage = memoryBlock.VirtualAddress >> (int)PAGE_SHIFT;
            uint numberOfPages = memoryBlock.Size / PAGE_SIZE;
            uint endPage = startPage + numberOfPages;

            for (uint i = startPage; i < endPage; i++)
            {
                PageTable[i] = null;
                MemoryManager.UnsetMemoryBlock((int)i);
            }
        }


        // -- Unused --
        private static void* GetNativeAddress(uint virtualAddress)
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
            MemoryBlock block = GetAllocationByVirtualAddress(virtualAddress);
            ulong offset = virtualAddress - block.VirtualAddress;
            return (byte*)block.NativeAddress + offset;
        }
    }
}
