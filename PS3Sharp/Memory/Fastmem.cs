using System.Runtime.InteropServices;
using static PS3Sharp.Memory.MemoryConstants;
using static PS3Sharp.Memory.MemoryManager;

namespace PS3Sharp.Memory
{
    internal static unsafe class Fastmem
    {
        // Everything from here is mostly unused - this is an old implementation

        //Size (in bytes) of a full page table where each entry is a pointer-sized value
        private static readonly uint _pageTableSizeBytes = (uint)(PageSize * nuint.Size);

        // Page table pointer
        private static byte** _pageTable = null;


        // -- Unused --
        internal static void AllocatePageTable()
        {
            _pageTable = (byte**)NativeMemory.AllocZeroed(_pageTableSizeBytes);
        }

        // -- Unused --
        private static bool TrySetFastmem(ref MemoryBlock memoryBlock, MemoryManager memoryManager)
        {
            uint startPage = memoryBlock.VirtualAddress >> (int)PageShift;
            uint numberOfPages = memoryBlock.Size / PageSize;
            uint endPage = startPage + numberOfPages;

            if (memoryBlock.Size % PageSize != 0)
            {
                throw new Exception($"Size is not aligned"); // Should never happen
                //return false;
            }

            uint pageIndex = 0;
            for (uint i = startPage; i < endPage; i++)
            {
                _pageTable[i] = (byte*)memoryBlock.NativeAddress + (pageIndex * PageSize);
                memoryManager.Allocator.SetMemoryBlock(ref memoryBlock, (int)i);
                pageIndex++;
            }

            return true;
        }

        // -- Unused --
        private static void UnsetFastmem(ref MemoryBlock memoryBlock, MemoryManager memoryManager)
        {
            uint startPage = memoryBlock.VirtualAddress >> (int)PageShift;
            uint numberOfPages = memoryBlock.Size / PageSize;
            uint endPage = startPage + numberOfPages;

            for (uint i = startPage; i < endPage; i++)
            {
                _pageTable[i] = null;
                memoryManager.Allocator.UnsetMemoryBlock((int)i);
            }
        }


        // -- Unused --
        private static void* GetNativeAddress(uint virtualAddress, MemoryManager memoryManager)
        {
            // Get the page from the page table
            uint page = virtualAddress >> (int)PageShift;
            byte* pageStart = _pageTable[page];
            uint inPageOffset = virtualAddress & PageMask;

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
            MemoryBlock block = memoryManager.Allocator.GetAllocationByVirtualAddress(virtualAddress);
            ulong offset = virtualAddress - block.VirtualAddress;
            return (byte*)block.NativeAddress + offset;
        }
    }
}
