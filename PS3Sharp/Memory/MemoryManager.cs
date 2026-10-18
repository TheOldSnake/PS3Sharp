using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.Memory
{
    internal sealed unsafe partial class MemoryManager : IDisposable
    {

        // Constants
        internal const ulong VIRTUAL_MEMORY_SIZE = 0x1_0000_0000; // 32-bit - 4 GiB
        internal const uint RAM_SIZE = 256 * 1024 * 1024;
        internal const uint RAM_START = 0x00010000;
        internal const uint RAM_END = RAM_START + RAM_SIZE;
        internal const uint STACK_REGION_START = 0xD0000000;
        internal const uint STACK_REGION_SIZE = 0x10000000;
        internal const uint STACK_REGION_BASE = STACK_REGION_START + STACK_REGION_SIZE - 16; //must be 16-byte aligned?
        internal const uint DEFAULT_STACK_SIZE = 0x40000;
        internal const uint RSX_VIDEO_MEM_SIZE = 256 * 1024 * 1024;
        internal const uint RSX_VIDEO_MEM_START = 0xC0000000;
        internal const uint RSX_VIDEO_MEM_END = RSX_VIDEO_MEM_START + RSX_VIDEO_MEM_SIZE;
        internal const uint SPU_LS_SIZE = 256 * 1024;
        internal const uint RAW_SPU_MEM_START = 0xE0000000;
        internal const uint RAW_SPU_OFFSET = 0x100000;
        internal const uint RAW_SPU_PROBLEM_OFFSET = 0x40000;

        // MemoryBlock table (managed array)
        private readonly MemoryBlock[] _memoryBlockTable = new MemoryBlock[Fastmem.PAGE_SIZE];

        // Virtual memory safe handle
        private VirtualMemoryHandle? _guestMemoryHandle = new((nuint)VIRTUAL_MEMORY_SIZE, NativeMethods.WIN_API_PAGE_NOACCESS);

        // Base address of virtual memory
        private void* GuestMemoryBase => (void*)_guestMemoryHandle!.DangerousGetHandle();

        // Some operations require a lock
        private readonly object _managerLock = new();

        private readonly List<MemoryBlock> _allocations = [];
        private readonly Dictionary<uint, MemoryReservation> _reservations = [];

        private uint _nextAllocationAddress = 0x30000000; // I don't know where to start
        private uint _nextStackAllocationAddress = STACK_REGION_START;
        private uint _nextHandle = 0x100;

        private GuestVoidPtr _tlsImagePtr;
        private uint _tlsImageSize;

        internal ulong TotalAllocatedSize { get; private set; }

        internal MemoryLock AcquireTemporaryLock()
        {
            // Return a new instance of MemoryLock to be used with "using" scope
            // (without "using" scope we need to call Dispose() manually)
            return new MemoryLock(_managerLock);
        }

        internal uint GetHandle()
        {
            lock (_managerLock)
            {
                uint ret = _nextHandle;
                _nextHandle++;
                return ret;
            }
        }

        private void* CommitMemory(uint virtualAddress, uint size)
        {
            if (GuestMemoryBase == null)
            {
                throw new Exception($"[MemoryManager] Guest memory was not reserved");
            }

            nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
            void* result = (void*)NativeMethods.VirtualAlloc(address, size, NativeMethods.WIN_API_MEM_COMMIT, NativeMethods.WIN_API_PAGE_READWRITE);

            if (result == null)
            {
                int error = Marshal.GetLastPInvokeError();
                Console.WriteLine($"[MemoryManager] Could not release decommit memory - error: 0x{error:X8}");
                throw new Win32Exception(error);
            }

            NativeMemory.Clear(result, size);
            return result;
        }

        private void* DecommitMemory(uint virtualAddress, uint size)
        {
            if (GuestMemoryBase == null)
            {
                throw new Exception($"[MemoryManager] Guest memory was not reserved");
            }

            nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
            int ok = NativeMethods.VirtualFree(address, size, NativeMethods.WIN_API_MEM_DECOMMIT);

            if (ok == 0)
            {
                int error = Marshal.GetLastPInvokeError();
                Console.WriteLine($"[MemoryManager] Could not release decommit memory - error: 0x{error:X8}");
                throw new Win32Exception(error);
            }

            return (void*)address;
        }

        private void FreeVirtualMemory(uint virtualAddress, uint size)
        {
            DecommitMemory(virtualAddress, size);
        }

        internal void* GetGuestMemoryBase()
        {
            return GuestMemoryBase;
        }

        internal MemoryBlock GetBlockInfo(uint virtualAddress)
        {
            // Lookup the block struct in the block table
            uint page = virtualAddress >> (int)Fastmem.PAGE_SHIFT;
            return _memoryBlockTable[page];
        }

        internal bool IsAllocated(uint virtualAddress)
        {
            return GetBlockInfo(virtualAddress).NativeAddress != null;
        }

        private uint PageAlign(uint size)
        {
            // Aligns to page size
            return (size + Fastmem.PAGE_SIZE - 1) & ~Fastmem.PAGE_MASK;
        }

        internal static uint AlignPowerOfTwo(uint value, uint alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        internal static uint Align(uint value, uint alignment)
        {
            uint remainder = value % alignment;
            return remainder == 0 ? value : (value + alignment - remainder);
        }

        internal uint ReadPPUInstruction(uint virtualAddress)
        {
            BigEndian<uint> instruction = Read<BigEndian<uint>>(virtualAddress);
            return instruction;
        }

        internal uint ReadRSXInstruction(uint virtualAddress)
        {
            BigEndian<uint> instruction = Read<BigEndian<uint>>(virtualAddress);
            return instruction;
        }

        internal void Write<T>(uint virtualAddress, in T value) where T : unmanaged
        {
            unsafe
            {
                void* address = GetNativeAddress(virtualAddress);
                Unsafe.WriteUnaligned(address, value);
            }
        }

        internal T Read<T>(uint virtualAddress) where T : unmanaged
        {
            unsafe
            {
                void* address = GetNativeAddress(virtualAddress);
                return Unsafe.ReadUnaligned<T>(address);
            }
        }

        internal T LoadAndReserve<T>(uint virtualAddress, uint threadId, PPUThread ppu) where T : unmanaged
        {
            lock (_managerLock)
            {
                ReserveAddress(virtualAddress, sizeof(T), threadId);
                T value = Read<T>(virtualAddress);
                return value;
            }
        }

        internal bool StoreConditional<T>(uint virtualAddress, uint threadId, T value, PPUThread ppu) where T : unmanaged
        {
            lock (_managerLock)
            {
                // Acquire reservation and only write if successful 
                bool ok = TryAccess(virtualAddress, sizeof(T), threadId);

                if (ok)
                {
                    Write(virtualAddress, value);
                }

                return ok;
            }
        }

        internal void ReserveAddress(uint virtualAddress, int size, uint threadId)
        {
            lock (_managerLock)
            {
                // one reservation per thread, apprearently
                _reservations[threadId] = new MemoryReservation
                {
                    VirtualAddress = virtualAddress,
                    Size = (uint)size,
                    ThreadID = threadId
                };

                //Console.WriteLine($"[{ThreadManager.GetThreadName(threadId)}] Reserved addr = 0x{virtualAddress:X8}");
            }
        }

        internal bool TryAccess(uint virtualAddress, int size, uint threadId)
        {
            ulong accessStart = virtualAddress;
            ulong accessEnd = (ulong)(virtualAddress + size);

            lock (_managerLock)
            {
                if (!_reservations.TryGetValue(threadId, out MemoryReservation? memoryReservation))
                {
                    return false;
                }

                ulong reservationStart = memoryReservation.VirtualAddress;
                ulong reservationEnd = memoryReservation.VirtualAddress + memoryReservation.Size;
                bool ok = accessStart >= reservationStart && accessEnd <= reservationEnd;

                _reservations.Remove(threadId);
                InvalidateReservations(virtualAddress, size);
                return ok;
            }
        }

        internal void InvalidateReservations(uint virtualAddress, int size)
        {
            ulong accessStart = virtualAddress;
            ulong accessEnd = (ulong)(virtualAddress + size);

            foreach (KeyValuePair<uint, MemoryReservation> reservation_thread_pair in _reservations.ToList())
            {
                MemoryReservation reservation = reservation_thread_pair.Value;

                ulong reservationStart = reservation.VirtualAddress;
                ulong reservationEnd = reservation.VirtualAddress + reservation.Size;
                bool overlap = accessStart < reservationEnd && accessEnd > reservationStart; // on any overlap?

                if (overlap)
                {
                    _reservations.Remove(reservation_thread_pair.Key);
                }
            }
        }

        // Returns a native pointer to the struct in the -unmanaged- guest memory
        internal T* GetPtr<T>(GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        {
            void* native_address = GetNativeAddress(ptr.Address);
            return (T*)native_address;
        }

        // Returns a managed reference to a struct in the -unmanaged- guest memory
        internal ref T GetRef<T>(GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        {
            void* native_address = GetNativeAddress(ptr.Address);
            return ref *(T*)native_address;
        }

        // Returns a managed reference to a struct in the -unmanaged- guest memory        
        internal ref T GetRef<T>(GuestVoidPtr ptr) where T : unmanaged, IPS3Struct
        {    
            void* native_address = GetNativeAddress(ptr.Address);
            return ref *(T*)native_address;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void* GetNativeAddress(uint virtualAddress)
        {
            // Debug
            //if (!IsAllocated(virtualAddress))
            //{
            //    throw new PageFaultException(virtualAddress);
            //}

            return (byte*)GuestMemoryBase + virtualAddress;
        }

        private void AddAllocationBlock(ref MemoryBlock block)
        {     // Does not allocate
            lock (_managerLock)
            {
                uint startPage = block.VirtualAddress >> (int)Fastmem.PAGE_SHIFT;
                uint numberOfPages = block.Size / Fastmem.PAGE_SIZE;
                uint endPage = startPage + numberOfPages;
                SetMemoryBlockTable((int)startPage, (int)endPage, ref block);

                _allocations.Add(block);
                _allocations.Sort((a, b) => a.VirtualAddress.CompareTo(b.VirtualAddress));

                // bool fast = TrySetFastmem(ref block);
                block.IsFastmem = true;
            }
        }

        private void RemoveAllocationBlock(ref MemoryBlock block)
        {   // Does not free
            lock (_managerLock)
            {
                uint startPage = block.VirtualAddress >> (int)Fastmem.PAGE_SHIFT;
                uint numberOfPages = block.Size / Fastmem.PAGE_SIZE;
                uint endPage = startPage + numberOfPages;
                MemoryBlock defaultBlock = default;
                SetMemoryBlockTable((int)startPage, (int)endPage, ref defaultBlock);

                _allocations.Remove(block);

                //if (block.IsFastmem) {
                //    UnsetFastmem(ref block);
                //}
            }
        }

        internal void SetMemoryBlock(ref MemoryBlock block, int index)
        {
            lock (_managerLock)
            {
                _memoryBlockTable[index] = block;
            }
        }

        internal void UnsetMemoryBlock(int index)
        {
            lock (_managerLock)
            {
                _memoryBlockTable[index] = default;
            }
        }

        internal void SetMemoryBlockTable(int start, int end, ref MemoryBlock block)
        {
            lock (_managerLock)
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

            lock (_managerLock)
            {
                if (IsVirtualAddressAllocated((uint)virtualAddress))
                {
                    throw new Exception($"attempt to allocate an already allocated address");
                }

                // void* memory = NativeMemory.AllocZeroed((nuint)size);
                void* memory = CommitMemory(virtualAddress, size);

                MemoryBlock block = new()
                {
                    VirtualAddress = virtualAddress,
                    Size = size,
                    Handle = GetHandle(),
                    NativeAddress = memory,
                };

                AddAllocationBlock(ref block);
                TotalAllocatedSize += size;

                Console.WriteLine($"[MemoryManager] Allocated at 0x{virtualAddress:X} size = 0x{size:X} {(block.IsFastmem ? "fast" : "slow")}");
                return block;
            }
        }

        internal MemoryBlock AllocateVirtualMemory(uint size, uint alignment = 0)
        {
            size = PageAlign(size);  // Ensure size is page aligned

            lock (_managerLock)
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
                    _nextAllocationAddress = isPowerOf2 ? AlignPowerOfTwo(_nextAllocationAddress, alignment) : Align(_nextAllocationAddress, alignment);
                }

                //Allocate memory
                MemoryBlock emptyBlock = AllocateVirtualAddress(_nextAllocationAddress, size);

                //Advance the next allocation address
                _nextAllocationAddress += (uint)size;

                return emptyBlock;
            }
        }

        internal MemoryBlock AllocatePhysicalMemory(uint size)
        {
            lock (_managerLock)
            {
                // Just allocate a virtual address -- we can find it by handle and remap it when requested
                return AllocateVirtualMemory(size);
            }
        }

        internal MemoryBlock ResizeBlock(MemoryBlock block, uint newSize)
        {
            lock (_managerLock)
            {
                uint virtualAddress = block.VirtualAddress;
                newSize = PageAlign(newSize);
                uint oldSize = PageAlign(block.Size);

                Console.WriteLine($"Realloc at 0x{block.VirtualAddress:X8} old size: 0x{block.Size:X} new size: {newSize:X}");

                // No difference
                if (newSize == oldSize)
                {
                    return block;
                }

                // Do not call free, just remove the old block info
                RemoveAllocationBlock(ref block);

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

                // Set the pointer for the block (realloc might return a new address)
                block.NativeAddress = nativePtr;

                // Set the new block size
                block.Size = newSize;

                // Add it again 
                AddAllocationBlock(ref block);

                // Return the modified block - in case the caller needs to use it
                return block;
            }
        }

        internal void FreeByVirtualAddress(uint virtualAddress)
        {
            lock (_managerLock)
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
            lock (_managerLock)
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

        internal MemoryBlock AllocateTLS()
        {
            lock (_managerLock)
            {
                if (!_tlsImagePtr.IsNullPtr)
                {
                    //Copy tls image to a newly allocated area per thread
                    MemoryBlock thread_tls = AllocateVirtualMemory(_tlsImageSize + 0x30);
                    void* src = GetNativeAddress(_tlsImagePtr);
                    NativeMemory.Copy(src, (byte*)thread_tls.NativeAddress + 0x30, (nuint)_tlsImageSize);
                    return thread_tls;

                }
                else
                {
                    //This means the ELF did not have a TLS segment
                    throw new Exception($"TLSImage was not found during elf loading");
                }
            }
        }

        internal void SetTLS(uint virtualAddress, uint size)
        {
            lock (_managerLock)
            {
                //Save the address of the TLS image (which can be in the middle of a block)
                _tlsImagePtr = virtualAddress;
                _tlsImageSize = size;
            }
        }

        internal bool IsVirtualAddressAllocated(uint virtualAddress)
        {
            lock (_managerLock)
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
            lock (_managerLock)
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
            lock (_managerLock)
            {
                uint currentAddress = targetAddress;

                //The list is already sorted
                for (int i = 0; i < _allocations.Count; i++)
                {
                    if (currentAddress + size <= _allocations[i].VirtualAddress)
                    {
                        return currentAddress;
                    }

                    if (currentAddress < _allocations[i].End)
                    {
                        currentAddress = (uint)_allocations[i].End;
                    }
                }

                return currentAddress;
            }
        }

        internal MemoryBlock GetAllocationByVirtualAddress(uint virtualAddress)
        {
            lock (_managerLock)
            {
                //Search in the allocation list
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
            lock (_managerLock)
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
            lock (_managerLock)
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
            lock (_managerLock)
            {
                MemoryBlock block = GetAllocationByHandle(handle);
                _allocations.Remove(block);

                //if (block.IsFastmem) {
                //    UnsetFastmem(ref block);
                //}

                uint newAddress = FindAllocatableAddress(destinationStart, (uint)block.Size);
                block.VirtualAddress = newAddress;
                AddAllocationBlock(ref block);
                return newAddress;
            }
        }

        internal uint RemapByVirtualAddress(uint virtualAddress, uint destinationStart)
        {
            lock (_managerLock)
            {
                MemoryBlock block = GetAllocationByVirtualAddress(virtualAddress);
                RemoveAllocationBlock(ref block);

                uint newAddress = FindAllocatableAddress(destinationStart, (uint)block.Size);
                block.VirtualAddress = newAddress;
                AddAllocationBlock(ref block);
                return newAddress;
            }
        }

        internal void CopyBufferToVirtualMemory(ReadOnlySpan<byte> source, uint destinationVirtualAddress)
        {
            // Direct copy
            byte* nativeDestination = (byte*)GetNativeAddress(destinationVirtualAddress);
            Span<byte> destSpan = new(nativeDestination, source.Length);
            source.CopyTo(destSpan);
        }

        internal void ReadBufferFromVirtualMemory(uint sourceVirtualAddress, Span<byte> destination)
        {
            // Direct copy
            byte* nativeSource = (byte*)GetNativeAddress(sourceVirtualAddress);
            Span<byte> sourceSpan = new(nativeSource, destination.Length);
            sourceSpan.CopyTo(destination);
        }

        internal void CopyVirtualMemory(uint sourceVirtualAddress, uint destinationVirtualAddress, int byteCount)
        {
            if (byteCount == 0)
            {
                return;
            }

            // Direct copy
            byte* nativeSource = (byte*)GetNativeAddress(sourceVirtualAddress);
            byte* nativeDestination = (byte*)GetNativeAddress(destinationVirtualAddress);

            Span<byte> sourceSpan = new(nativeSource, byteCount);
            Span<byte> destSpan = new(nativeDestination, byteCount);

            sourceSpan.CopyTo(destSpan);
        }

        internal void ClearMemory(uint virtualAddress, uint byteCount, byte value = 0)
        {
            if (byteCount == 0)
            {
                return;
            }

            void* dest = GetNativeAddress((uint)virtualAddress);
            NativeMemory.Fill(dest, byteCount, value);
        }

        internal string ReadString(uint virtualAddress)
        {
            byte* native = (byte*)GetNativeAddress(virtualAddress);
            return Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(native));
        }

        // Must be in unsafe context
        internal unsafe Span<byte> CreateByteSpan(uint virtualAddress, int length)
        {
            byte* ptr = (byte*)GetNativeAddress(virtualAddress);
            return new Span<byte>(ptr, length);
        }

        internal T[] ReadArray<T>(uint virtualAddress, int length) where T : unmanaged
        {
            uint size = (uint)Unsafe.SizeOf<T>();
            uint currentAddress = virtualAddress;
            T[] values = new T[length];

            for (int i = 0; i < length; i++)
            {
                values[i] = Read<T>(currentAddress);
                currentAddress += size;
            }

            return values;
        }

        // Returns a ref of a value without flipping it -- Use carefully !
        internal unsafe ref T DangerousBigEndianRef<T>(uint virtualAddress)
        {
            T* ptr = (T*)GetNativeAddress(virtualAddress);
            return ref *ptr;
        }

        public void Dispose()
        {
            Console.WriteLine($"[MemoryManager] Freeing guest memory");
            _guestMemoryHandle?.Dispose();
            _guestMemoryHandle = null;
        }
    }
}