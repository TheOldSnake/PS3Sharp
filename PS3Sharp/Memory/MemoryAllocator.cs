using PS3Sharp.Common;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static PS3Sharp.Memory.MemoryConstants;
using static PS3Sharp.Memory.MemoryManager;

namespace PS3Sharp.Memory
{
    internal unsafe class MemoryAllocator : IDisposable
    {
        // Virtual memory safe handle
        private readonly VirtualMemoryHandle _guestMemoryHandle = new(unchecked((nuint)VirtualMemorySize), NativeMethods.WinApiPageNoaccess);

        // List of active allocations
        private readonly List<MemoryBlock> _allocations = [];

        // Memory lock for operations that needs synchronization
        private readonly Lock _memoryLock;

        // Next allocation addresses
        private uint _nextHeapAllocationAddress = UserHeapStart;
        private uint _nextStackAllocationAddress = StackRegionStart;

        // Next ID/Handle 
        private uint _nextHandle = 0x100;

        // Total allocated virtual memory
        internal ulong TotalAllocatedSize { get; private set; }

        // Base address of virtual memory
        internal void* GuestMemoryBase => _guestMemoryHandle.NativePointer;

        internal MemoryAllocator(Lock memoryLock)
        {
            _memoryLock = memoryLock;
        }

        internal uint CreateHandle()
        {
            using (_memoryLock.EnterScope())
            {
                uint ret = _nextHandle;
                _nextHandle++;
                return ret;
            }
        }

        internal MemoryBlock AllocateStackSpace(uint size)
        {
            using (_memoryLock.EnterScope())
            {
                _nextStackAllocationAddress = (uint)Utility.AlignPowerOfTwo(_nextStackAllocationAddress, DefaultAlignment);
                MemoryBlock block = AllocateVirtualAddress(_nextStackAllocationAddress, size);
                _nextStackAllocationAddress += size;
                return block;
            }
        }

        internal MemoryBlock AllocateVirtualAddress(uint virtualAddress, uint size)
        {
            using (_memoryLock.EnterScope())
            {
                if (TryGetBlockByAddress(virtualAddress, out _))
                {
                    throw new Exception($"attempt to allocate an already allocated address");
                }

                // Note: Always call CommitMemory because committing an already committed page is safe;
                // Windows simply leaves it committed.
                uint handle = CreateHandle();
                void* memory = CommitMemory(virtualAddress, size);
                MemoryBlock block = MemoryBlock.Create(handle, virtualAddress, size, memory);

                AddAllocationBlock(ref block);
                TotalAllocatedSize += size;

                Console.WriteLine($"[MemoryAllocator] Allocated at 0x{virtualAddress:X} size = 0x{size:X}");
                return block;
            }
        }

        internal MemoryBlock AllocateVirtualMemory(uint size, uint alignment = DefaultAlignment)
        {
            using (_memoryLock.EnterScope())
            {
                // If the next address to be allocated falls into an already allocated space then skip it
                while (TryGetBlockByAddress(_nextHeapAllocationAddress, out MemoryBlock usedBlock))
                {
                    _nextHeapAllocationAddress += usedBlock.Size;
                }

                // Align the address
                if (alignment > 0)
                {
                    _nextHeapAllocationAddress = Utility.Align(_nextHeapAllocationAddress, alignment);
                }

                // Allocate memory
                MemoryBlock block = AllocateVirtualAddress(_nextHeapAllocationAddress, size);

                // Advance the next allocation address
                _nextHeapAllocationAddress += size;

                return block;
            }
        }

        internal MemoryBlock AllocatePrxMemory(uint size, uint alignment = DefaultAlignment)
        {
            // TODO: Allocate Prx memory in a differnt region
            return AllocateVirtualMemory(size, alignment);
        }

        internal MemoryBlock AllocatePages(int pageCount)
        {
            return AllocateVirtualMemory(VirtualPageSize * (uint)pageCount);
        }

        internal MemoryBlock AllocatePhysicalMemory(uint size)
        {
            using (_memoryLock.EnterScope())
            {
                // Just allocate a virtual address -- we can find it by handle and remap it when requested
                return AllocateVirtualMemory(size);
            }
        }

        internal MemoryBlock ResizeBlock(MemoryBlock oldBlcok, uint newSize)
        {
            using (_memoryLock.EnterScope())
            {
                uint virtualAddress = oldBlcok.VirtualAddress;
                uint oldSize = oldBlcok.Size;

                Console.WriteLine($"Realloc at 0x{oldBlcok.VirtualAddress:X8} old size: 0x{oldBlcok.Size:X} new size: {newSize:X}");

                // No difference
                if (newSize == oldSize)
                {
                    return oldBlcok;
                }

                // Do not call free, just remove the old block info
                RemoveAllocationBlock(ref oldBlcok);

                void* nativePtr;

                // if new size is larger we commit more pages, otherwise decommit to shrink
                if (newSize > oldSize)
                {
                    uint difference = newSize - oldSize;
                    nativePtr = CommitMemory(virtualAddress + oldSize, difference);
                    NativeMemory.Clear(nativePtr, difference);  // Clear extra memory
                }
                else
                {
                    uint difference = oldSize - newSize;
                    nativePtr = DecommitMemory(virtualAddress + newSize, difference);
                }

                // Create a new block with the new size and native address
                MemoryBlock newBlock = MemoryBlock.Create(oldBlcok.Handle, oldBlcok.VirtualAddress, newSize, nativePtr);
                AddAllocationBlock(ref newBlock);

                // Return the new block - in case the caller needs to use it
                return newBlock;
            }
        }

        internal void FreeByVirtualAddress(uint virtualAddress)
        {
            using (_memoryLock.EnterScope())
            {
                if (TryGetBlockByAddress(virtualAddress, out MemoryBlock block))
                {
                    RemoveAllocationBlock(ref block);
                    FreeVirtualMemory(block.VirtualAddress, block.Size);
                }
                else
                {
                    throw new Exception($"Attemp to free unallocated address: {virtualAddress:X8}");
                }
            }
        }

        internal void FreeByHandle(uint handle)
        {
            using (_memoryLock.EnterScope())
            {
                if (TryGetBlockByHandle(handle, out MemoryBlock block))
                {
                    RemoveAllocationBlock(ref block);
                    FreeVirtualMemory(block.VirtualAddress, block.Size);

                    Console.WriteLine($"Freed: {block.VirtualAddress:X8}");
                }
                else
                {
                    throw new Exception($"Attemp to free unallocated handle: {handle:X8}");
                }
            }
        }

        internal uint FindAllocatableAddress(uint targetAddress, uint size)
        {
            using (_memoryLock.EnterScope())
            {
                uint currentAddress = targetAddress;

                // The list is already sorted
                for (int i = 0; i < _allocations.Count; i++)
                {
                    if (currentAddress + size <= _allocations[i].VirtualAddress)
                    {
                        return currentAddress;
                    }

                    if (currentAddress < _allocations[i].End)
                    {
                        currentAddress = _allocations[i].End;
                    }
                }

                return currentAddress;
            }
        }

        internal bool TryGetBlockByAddress(uint virtualAddress, out MemoryBlock memoryBlock)
        {
            using (_memoryLock.EnterScope())
            {
                // Search in the allocation list
                foreach (MemoryBlock block in _allocations)
                {
                    if (virtualAddress >= block.VirtualAddress && virtualAddress < block.End)
                    {
                        memoryBlock = block;
                        return true;
                    }
                }

                memoryBlock = default;
                return false;
            }
        }

        internal bool TryGetBlockByHandle(ulong handle, out MemoryBlock memoryBlock)
        {
            using (_memoryLock.EnterScope())
            {
                foreach (MemoryBlock block in _allocations)
                {
                    if (block.Handle == handle)
                    {
                        memoryBlock = block;
                        return true;
                    }
                }

                memoryBlock = default;
                return false;
            }
        }

        internal uint RemapByHandle(uint handle, uint destinationStart)
        {
            using (_memoryLock.EnterScope())
            {
                if (TryGetBlockByHandle(handle, out MemoryBlock oldBlock))
                {
                    MemoryBlock remapped = RemapBlock(ref oldBlock, destinationStart);
                    return remapped.VirtualAddress;
                }
                else
                {
                    throw new InvalidOperationException($"Attempt to remap a block that does not exist");
                }
            }
        }

        internal uint RemapByVirtualAddress(uint virtualAddress, uint destinationStart)
        {
            using (_memoryLock.EnterScope())
            {
                if (TryGetBlockByAddress(virtualAddress, out MemoryBlock oldBlock))
                {
                    MemoryBlock remapped = RemapBlock(ref oldBlock, destinationStart);
                    return remapped.VirtualAddress;
                }
                else
                {
                    throw new InvalidOperationException($"Attempt to remap a block that does not exist");
                }
            }
        }

        private MemoryBlock RemapBlock(ref MemoryBlock oldBlock, uint destinationStart)
        {
            RemoveAllocationBlock(ref oldBlock);

            uint newAddress = FindAllocatableAddress(destinationStart, oldBlock.Size);
            MemoryBlock newBlock = MemoryBlock.Create(oldBlock.Handle, newAddress, oldBlock.Size, oldBlock.NativeAddress);

            AddAllocationBlock(ref newBlock);
            return newBlock;
        }

        private void FreeVirtualMemory(uint virtualAddress, uint size)
        {
            // We need to check if the pages spanned by this range can be  
            // safely decommitted without affecting existing allocations.
            using (_memoryLock.EnterScope())
            {
                int pageShift = NativePageShift;
                uint pageSize = NativePageSize;

                uint startPage = virtualAddress >> pageShift;
                uint endPage = (virtualAddress + size + pageSize - 1) >> pageShift; // Ceiling

                for (uint page = startPage; page < endPage; page++)
                {
                    uint virtualPageAddress = page * pageSize;

                    if (CanFreeNativePage(page))
                    {
                        DecommitMemory(virtualPageAddress, pageSize);
                    }
                }
            }
        }

        private void AddAllocationBlock(ref MemoryBlock block)
        {
            using (_memoryLock.EnterScope())
            {
                _allocations.Add(block);
                _allocations.Sort((a, b) => a.VirtualAddress.CompareTo(b.VirtualAddress));
            }
        }

        private void RemoveAllocationBlock(ref MemoryBlock block)
        {
            using (_memoryLock.EnterScope())
            {
                _allocations.Remove(block);
            }
        }

        private unsafe void* CommitMemory(uint virtualAddress, uint size)
        {
            unsafe
            {
                if (GuestMemoryBase == null)
                {
                    throw new Exception($"[MemoryAllocator] Guest memory was not reserved");
                }

                // Note: VirtualAlloc will round size to 4 KB pages,
                // and will return the input address rounded down to nearest page boundary
                byte* address = (byte*)GuestMemoryBase + virtualAddress;
                void* result = (void*)NativeMethods.VirtualAlloc((nint)address, size, NativeMethods.WinApiMemCommit, NativeMethods.WinApiPageReadwrite);

                if (result == null)
                {
                    int error = Marshal.GetLastPInvokeError();
                    Console.WriteLine($"[MemoryAllocator] Could not commit memory - error: 0x{error:X8}");
                    throw new Win32Exception(error);
                }

                NativeMemory.Clear(address, size);
                return address;
            }
        }

        private unsafe void* DecommitMemory(uint virtualAddress, uint size)
        {
            unsafe
            {
                if (GuestMemoryBase == null)
                {
                    throw new Exception($"[MemoryAllocator] Guest memory was not reserved");
                }

                // Note: VirtualFree will round size to 4 KB pages
                nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
                int ok = NativeMethods.VirtualFree(address, size, NativeMethods.WinApiMemDecommit);

                if (ok == 0)
                {
                    int error = Marshal.GetLastPInvokeError();
                    Console.WriteLine($"[MemoryAllocator] Could not release decommit memory - error: 0x{error:X8}");
                    throw new Win32Exception(error);
                }

                return (void*)address;
            }
        }

        private bool CanFreeNativePage(uint targetPage)
        {
            // Check if the native page can be decommitted
            // Ensure that no existing allocation falls into the page
            using (_memoryLock.EnterScope())
            {
                int shift = NativePageShift;

                foreach (MemoryBlock allocation in _allocations)
                {
                    uint allocationPageStart = allocation.VirtualAddress >> shift;
                    uint allocationPageEnd = allocation.End >> shift;

                    if (targetPage >= allocationPageStart && targetPage < allocationPageEnd)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"[MemoryAllocator] Freeing guest memory");
            _guestMemoryHandle.Dispose();
        }
    }
}