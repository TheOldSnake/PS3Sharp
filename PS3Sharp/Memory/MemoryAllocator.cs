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

        private readonly MemoryBlock[] _memoryBlockTable = new MemoryBlock[PageSize];
        private readonly List<MemoryBlock> _allocations = [];
        private readonly Lock _memoryLock;

        private uint _nextAllocationAddress = 0x30000000; // I don't know where to start
        private uint _nextStackAllocationAddress = StackRegionStart;
        private uint _nextHandle = 0x100;

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

        internal MemoryBlock GetBlockInfo(uint virtualAddress)
        {
            // Lookup the block struct in the block table
            uint page = virtualAddress >> (int)PageShift;
            return _memoryBlockTable[page];
        }

        internal bool IsAllocated(uint virtualAddress)
        {
            return GetBlockInfo(virtualAddress).NativeAddress != null;
        }

        internal void SetMemoryBlock(ref MemoryBlock block, int index)
        {
            using (_memoryLock.EnterScope())
            {
                _memoryBlockTable[index] = block;
            }
        }

        internal void UnsetMemoryBlock(int index)
        {
            using (_memoryLock.EnterScope())
            {
                _memoryBlockTable[index] = default;
            }
        }

        internal void SetMemoryBlockTable(int start, int end, ref MemoryBlock block)
        {
            using (_memoryLock.EnterScope())
            {
                for (int i = start; i < end; i++)
                {
                    _memoryBlockTable[i] = block;
                }
            }
        }

        internal MemoryBlock AllocateStackSpace(uint size)
        {
            size = PageAlign(size); // Ensure size is page aligned
            MemoryBlock block = AllocateVirtualAddress(_nextStackAllocationAddress, size);
            _nextStackAllocationAddress += size;
            return block;
        }

        internal MemoryBlock AllocateVirtualAddress(uint virtualAddress, uint size)
        {
            size = PageAlign(size); // Ensure size is page aligned

            using (_memoryLock.EnterScope())
            {
                if (IsVirtualAddressAllocated((uint)virtualAddress))
                {
                    throw new Exception($"attempt to allocate an already allocated address");
                }

                void* memory = CommitMemory(virtualAddress, size);
                uint handle = CreateHandle();
                MemoryBlock block = MemoryBlock.Create(handle, virtualAddress, size, memory);

                AddAllocationBlock(ref block);
                TotalAllocatedSize += size;

                // Console.WriteLine($"[MemoryManager] Allocated at 0x{virtualAddress:X} size = 0x{size:X}");
                return block;
            }
        }

        internal MemoryBlock AllocateVirtualMemory(uint size, uint alignment = 0)
        {
            size = PageAlign(size);  // Ensure size is page aligned

            using (_memoryLock.EnterScope())
            {
                //If the next address to be allocated falls into an already allocated space then skip it
                while (IsVirtualAddressAllocated(_nextAllocationAddress))
                {
                    MemoryBlock usedBlock = GetAllocationByVirtualAddress(_nextAllocationAddress);
                    _nextAllocationAddress += (uint)usedBlock.Size;
                }

                if (alignment > 0)
                {
                    bool isPowerOf2 = (alignment & (alignment - 1)) == 0;
                    ulong alignedAddress = isPowerOf2 ? Utility.AlignPowerOfTwo(_nextAllocationAddress, alignment) : Utility.Align(_nextAllocationAddress, alignment);
                    _nextAllocationAddress = (uint)alignedAddress;
                }

                //Allocate memory
                MemoryBlock emptyBlock = AllocateVirtualAddress(_nextAllocationAddress, size);

                //Advance the next allocation address
                _nextAllocationAddress += size;

                return emptyBlock;
            }
        }

        internal MemoryBlock AllocatePages(int pageCount)
        {
            return AllocateVirtualMemory(PageSize * (uint)pageCount);
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
                newSize = PageAlign(newSize);
                uint oldSize = PageAlign(oldBlcok.Size);

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

                // Create a new block with the new size and native address (realloc might return a new address)
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
                if (IsVirtualAddressAllocated(virtualAddress))
                {
                    MemoryBlock block = GetAllocationByVirtualAddress(virtualAddress);
                    RemoveAllocationBlock(ref block);

                    //NativeMemory.Free(block.NativeAddress);
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
                if (IsVirtualHandleAllocated(handle))
                {
                    MemoryBlock block = GetAllocationByHandle(handle);
                    RemoveAllocationBlock(ref block);

                    //NativeMemory.Free(block.NativeAddress);
                    FreeVirtualMemory(block.VirtualAddress, block.Size);

                    Console.WriteLine($"Freed: {block.VirtualAddress:X8}");
                }
                else
                {
                    throw new Exception($"Attemp to free unallocated handle: {handle:X8}");
                }
            }
        }

        internal bool IsVirtualAddressAllocated(uint virtualAddress)
        {
            using (_memoryLock.EnterScope())
            {
                for (int i = 0; i < _allocations.Count; i++)
                {
                    MemoryBlock block = _allocations[i];
                    if (virtualAddress >= block.VirtualAddress && virtualAddress < block.End)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal bool IsVirtualHandleAllocated(uint handle)
        {
            using (_memoryLock.EnterScope())
            {
                for (int i = 0; i < _allocations.Count; i++)
                {
                    MemoryBlock block = _allocations[i];
                    if (block.Handle == handle)
                    {
                        return true;
                    }
                }

                return false;
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

        internal MemoryBlock GetAllocationByVirtualAddress(uint virtualAddress)
        {
            using (_memoryLock.EnterScope())
            {
                // Search in the allocation list
                for (int i = 0; i < _allocations.Count; i++)
                {
                    MemoryBlock block = _allocations[i];
                    if (virtualAddress >= block.VirtualAddress && virtualAddress < block.End)
                    {
                        return block;
                    }
                }

                throw new PageFaultException(virtualAddress);
            }
        }

        internal MemoryBlock GetAllocationByHandle(ulong handle)
        {
            using (_memoryLock.EnterScope())
            {
                for (int i = 0; i < _allocations.Count; i++)
                {
                    MemoryBlock block = _allocations[i];
                    if (block.Handle == handle)
                    {
                        return block;
                    }
                }

                throw new Exception($"Can't find handle: {handle}");
            }
        }

        internal bool IsHandleAllocated(ulong handle)
        {
            using (_memoryLock.EnterScope())
            {
                for (int i = 0; i < _allocations.Count; i++)
                {
                    MemoryBlock block = _allocations[i];
                    if (block.Handle == handle)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal uint RemapByHandle(uint handle, uint destinationStart)
        {
            using (_memoryLock.EnterScope())
            {
                MemoryBlock oldBlock = GetAllocationByHandle(handle);
                _allocations.Remove(oldBlock);

                uint newAddress = FindAllocatableAddress(destinationStart, oldBlock.Size);
                MemoryBlock newBlock = MemoryBlock.Create(oldBlock.Handle, newAddress, oldBlock.Size, oldBlock.NativeAddress);
                AddAllocationBlock(ref newBlock);

                return newAddress;
            }
        }

        internal uint RemapByVirtualAddress(uint virtualAddress, uint destinationStart)
        {
            using (_memoryLock.EnterScope())
            {
                MemoryBlock oldBlock = GetAllocationByVirtualAddress(virtualAddress);
                RemoveAllocationBlock(ref oldBlock);

                uint newAddress = FindAllocatableAddress(destinationStart, oldBlock.Size);
                MemoryBlock newBlock = MemoryBlock.Create(oldBlock.Handle, newAddress, oldBlock.Size, oldBlock.NativeAddress);
                AddAllocationBlock(ref newBlock);

                return newAddress;
            }
        }

        private unsafe void* CommitMemory(uint virtualAddress, uint size)
        {
            unsafe
            {
                if (GuestMemoryBase == null)
                {
                    throw new Exception($"[MemoryManager] Guest memory was not reserved");
                }

                nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
                void* result = (void*)NativeMethods.VirtualAlloc(address, size, NativeMethods.WinApiMemCommit, NativeMethods.WinApiPageReadwrite);

                if (result == null)
                {
                    int error = Marshal.GetLastPInvokeError();
                    Console.WriteLine($"[MemoryManager] Could not commit memory - error: 0x{error:X8}");
                    throw new Win32Exception(error);
                }

                NativeMemory.Clear(result, size);
                return result;
            }
        }

        private unsafe void* DecommitMemory(uint virtualAddress, uint size)
        {
            unsafe
            {
                if (GuestMemoryBase == null)
                {
                    throw new Exception($"[MemoryManager] Guest memory was not reserved");
                }

                nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
                int ok = NativeMethods.VirtualFree(address, size, NativeMethods.WinApiMemDecommit);

                if (ok == 0)
                {
                    int error = Marshal.GetLastPInvokeError();
                    Console.WriteLine($"[MemoryManager] Could not release decommit memory - error: 0x{error:X8}");
                    throw new Win32Exception(error);
                }

                return (void*)address;
            }
        }

        private unsafe void FreeVirtualMemory(uint virtualAddress, uint size)
        {
            DecommitMemory(virtualAddress, size);
        }

        private static uint PageAlign(uint size)
        {
            // Aligns to page size
            return (uint)Utility.AlignPowerOfTwo(size, PageSize);
        }

        private void AddAllocationBlock(ref MemoryBlock block)
        {
            using (_memoryLock.EnterScope())
            {
                uint startPage = block.VirtualAddress >> (int)PageShift;
                uint numberOfPages = block.Size / PageSize;
                uint endPage = startPage + numberOfPages;
                SetMemoryBlockTable((int)startPage, (int)endPage, ref block);

                _allocations.Add(block);
                _allocations.Sort((a, b) => a.VirtualAddress.CompareTo(b.VirtualAddress));
            }
        }

        private void RemoveAllocationBlock(ref MemoryBlock block)
        {
            using (_memoryLock.EnterScope())
            {
                uint startPage = block.VirtualAddress >> (int)PageShift;
                uint numberOfPages = block.Size / PageSize;
                uint endPage = startPage + numberOfPages;
                MemoryBlock defaultBlock = default;
                SetMemoryBlockTable((int)startPage, (int)endPage, ref defaultBlock);

                _allocations.Remove(block);
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"[MemoryAllocator] Freeing guest memory");
            _guestMemoryHandle.Dispose();
        }
    }
}