using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.Memory_Manager
{
    internal static unsafe partial class MemoryManager
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
        private static readonly MemoryBlock[] MemoryBlockTable = new MemoryBlock[Fastmem.PAGE_SIZE];

        // Base address of virtual memory
        private static void* GuestMemoryBase;

        // Some operations require a lock
        private static readonly object _managerLock = new();

        private static readonly List<MemoryBlock> Allocations = [];
        private static readonly Dictionary<uint, MemoryReservation> Reservations = [];

        internal static ulong TotalAllocatedSize { get; private set; } = 0;

        internal static uint NextAllocationAddress = 0x30000000; // I don't know where to start
        internal static uint NextStackAllocationAddress = MemoryManager.STACK_REGION_START;
        internal static uint NextHandle = 0x100;

        //internal MemoryBlock TLSImage;
        internal static GuestVoidPtr TLS_Image;
        internal static uint TLS_Image_Size;

        internal static MemoryLock AcquireTemporaryLock()
        {
            // Return a new instance of MemoryLock to be used with "using" scope
            // (without "using" scope we need to call Dispose() manually)
            return new MemoryLock();
        }

        internal static void Initialize()
        {
            ReserveVirtualMemory();
        }

        private static void ReserveVirtualMemory()
        {
            if (GuestMemoryBase != null)
            {
                throw new Exception($"[MemoryManager] Guest memory already reserved");
            }

            GuestMemoryBase = (void*)WinAPI.VirtualAlloc(0, (nuint)VIRTUAL_MEMORY_SIZE, WinAPI.MEM_RESERVE, WinAPI.PAGE_NOACCESS);

            if (GuestMemoryBase == null)
            {
                Console.WriteLine("[MemoryManager] Could not reserve guest memory space");
                throw new Win32Exception();
            }
        }

        private static void* CommitMemory(uint virtualAddress, uint size)
        {
            if (GuestMemoryBase == null)
            {
                throw new Exception($"[MemoryManager] Guest memory was not reserved");
            }

            nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
            void* result = (void*)WinAPI.VirtualAlloc(address, size, WinAPI.MEM_COMMIT, WinAPI.PAGE_READWRITE);

            if (result == null)
            {
                Console.WriteLine($"[MemoryManager] Could not release decommit memory");
                throw new Win32Exception();
            }

            NativeMemory.Clear(result, size);
            return result;
        }

        private static void* DecommitMemory(uint virtualAddress, uint size)
        {
            if (GuestMemoryBase == null)
            {
                throw new Exception($"[MemoryManager] Guest memory was not reserved");
            }

            nint address = (nint)((byte*)GuestMemoryBase + (nint)virtualAddress);
            int ok = WinAPI.VirtualFree(address, size, WinAPI.MEM_DECOMMIT);

            if (ok == 0)
            {
                Console.WriteLine($"[MemoryManager] Could not release decommit memory");
                throw new Win32Exception();
            }

            return (void*)address;
        }

        private static void FreeVirtualMemory(uint virtualAddress, uint size)
        {
            DecommitMemory(virtualAddress, size);
        }

        private static void FreeVirtualMemory()
        {
            if (GuestMemoryBase != null)
            {
                int ok = WinAPI.VirtualFree((nint)GuestMemoryBase, 0, WinAPI.MEM_RELEASE);

                if (ok == 0)
                {
                    throw new Win32Exception();
                }

                GuestMemoryBase = null;
            }
        }

        internal static uint GetHandle()
        {
            lock (_managerLock)
            {
                uint ret = NextHandle;
                NextHandle++;
                return ret;
            }
        }

        internal static void* GetGuestMemoryBase()
        {
            return GuestMemoryBase;
        }

        internal static MemoryBlock GetBlockInfo(uint virtualAddress)
        {
            // Lookup the block struct in the block table
            uint page = virtualAddress >> (int)Fastmem.PAGE_SHIFT;
            return MemoryBlockTable[page];
        }

        internal static bool IsAllocated(uint virtualAddress)
        {
            return GetBlockInfo(virtualAddress).NativeAddress != null;
        }

        private static uint PageAlign(uint size)
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

        internal static uint ReadPPUInstruction(uint virtualAddress)
        {
            // PPU does not have self modifying code 
            return *(BigEndian<uint>*)GetNativeAddress(virtualAddress);
        }

        internal static uint ReadRSXInstruction(uint virtualAddress)
        {
            return *(BigEndian<uint>*)GetNativeAddress(virtualAddress);
        }

        // Writes the little endian data to the big endian memory
        internal static void Write<T>(uint virtualAddress, T value) where T : unmanaged
        {
            BigEndian<T>* native_address = (BigEndian<T>*)GetNativeAddress(virtualAddress);
            *native_address = value;

            // Console.WriteLine($"[MemoryManager] Memory Write: [{virtualAddress:X}] = {value:X}");
        }

        // Writes the data to the big endian memory without any conversion
        internal static void WriteBE<T>(uint virtualAddress, T value) where T : unmanaged
        {
            void* native_address = GetNativeAddress(virtualAddress);
            *(T*)native_address = value;
        }

        // Reads from the big endian memory and swaps data to little endian
        internal static T Read<T>(uint virtualAddress) where T : unmanaged
        {
            void* native_address = GetNativeAddress(virtualAddress);
            T value = *(BigEndian<T>*)native_address;
            return value;
        }

        // Reads from the big endian memory without any conversion
        internal static T ReadBE<T>(uint virtualAddress) where T : unmanaged
        {
            void* native_address = GetNativeAddress(virtualAddress);
            return *(T*)native_address;
        }

        internal static T LoadAndReserve<T>(uint virtualAddress, uint threadId, PPU ppu) where T : unmanaged
        {
            lock (_managerLock)
            {
                ReserveAddress(virtualAddress, sizeof(T), threadId);
                T value = Read<T>(virtualAddress);
                return value;
            }
        }

        internal static bool StoreConditional<T>(uint virtualAddress, uint threadId, T value, PPU ppu) where T : unmanaged
        {
            lock (_managerLock)
            {

                // Acquire reservation and only write if successful 
                bool ok = TryAccess(virtualAddress, sizeof(T), threadId);

                if (ok)
                {
                    Write<T>(virtualAddress, value);
                }

                return ok;
            }
        }

        internal static void ReserveAddress(uint virtualAddress, int size, uint threadId)
        {

            lock (_managerLock)
            {
                // one reservation per thread, apprearently
                Reservations[threadId] = new MemoryReservation
                {
                    VirtualAddress = virtualAddress,
                    Size = (uint)size,
                    ThreadID = threadId
                };

                //Console.WriteLine($"[{ThreadManager.GetThreadName(threadId)}] Reserved addr = 0x{virtualAddress:X8}");
            }
        }

        internal static bool TryAccess(uint virtualAddress, int size, uint threadId)
        {

            ulong accessStart = virtualAddress;
            ulong accessEnd = (ulong)(virtualAddress + size);

            lock (_managerLock)
            {

                if (!Reservations.TryGetValue(threadId, out MemoryReservation? memoryReservation))
                {
                    return false;
                }

                ulong reservationStart = memoryReservation.VirtualAddress;
                ulong reservationEnd = memoryReservation.VirtualAddress + memoryReservation.Size;
                bool ok = accessStart >= reservationStart && accessEnd <= reservationEnd;

                Reservations.Remove(threadId);
                InvalidateReservations(virtualAddress, size);
                return ok;
            }
        }

        internal static void InvalidateReservations(uint virtualAddress, int size)
        {
            ulong accessStart = virtualAddress;
            ulong accessEnd = (ulong)(virtualAddress + size);

            foreach (KeyValuePair<uint, MemoryReservation> reservation_thread_pair in Reservations.ToList())
            {
                MemoryReservation reservation = reservation_thread_pair.Value;

                ulong reservationStart = reservation.VirtualAddress;
                ulong reservationEnd = reservation.VirtualAddress + reservation.Size;
                bool overlap = accessStart < reservationEnd && accessEnd > reservationStart; // on any overlap?

                if (overlap)
                {
                    Reservations.Remove(reservation_thread_pair.Key);
                }
            }
        }

        // Returns a native pointer to the struct in the -unmanaged- guest memory
        internal static T* GetPtr<T>(GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        {
            void* native_address = GetNativeAddress(ptr.Address);
            return (T*)native_address;
        }

        // Returns a managed reference to a struct in the -unmanaged- guest memory

        internal static ref T GetRef<T>(GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        {
            void* native_address = GetNativeAddress(ptr.Address);
            return ref *(T*)native_address;
        }

        // Returns a managed reference to a struct in the -unmanaged- guest memory        
        internal static ref T GetRef<T>(GuestVoidPtr ptr) where T : unmanaged, IPS3Struct
        {    // Unused
            void* native_address = GetNativeAddress(ptr.Address);
            return ref *(T*)native_address;
        }

        // writes a struct atomically

        internal static void WriteStruct<T>(uint virtualAddress, T value) where T : unmanaged, IPS3Struct
        {
            lock (_managerLock)
            {
                void* native_address = GetNativeAddress(virtualAddress);
                *(T*)native_address = value;
            }
        }

        // Reads a struct atomically

        internal static T ReadStruct<T>(uint virtualAddress) where T : unmanaged, IPS3Struct
        {
            lock (_managerLock)
            {
                void* native_address = GetNativeAddress(virtualAddress);
                return *(T*)native_address;
            }
        }

        // Updates a struct atomically
        internal delegate void RefAction<T>(ref T value);
        internal static void UpdateStruct<T>(uint addr, RefAction<T> updater) where T : unmanaged, IPS3Struct
        {
            lock (_managerLock)
            {
                T value = *(T*)GetNativeAddress(addr);
                updater(ref value);
                *(T*)GetNativeAddress(addr) = value;
            }
        }

        // Initializes a struct atomically
        internal static void InitializeStruct<T>(uint virtualAddress, T value) where T : unmanaged, IPS3Struct
        {      // Unused
            UpdateStruct<T>(virtualAddress, (ref T val) => val = value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* GetNativeAddress(uint virtualAddress)
        {

            // Degbug
            //if (!IsAllocated(virtualAddress)) {
            //    throw new PageFaultException(virtualAddress);
            //}

            return (byte*)GuestMemoryBase + virtualAddress;
        }

        private static void AddAllocationBlock(ref MemoryBlock block)
        {     // Does not allocate
            lock (_managerLock)
            {

                uint startPage = block.VirtualAddress >> (int)Fastmem.PAGE_SHIFT;
                uint numberOfPages = block.Size / Fastmem.PAGE_SIZE;
                uint endPage = startPage + numberOfPages;
                SetMemoryBlockTable((int)startPage, (int)endPage, ref block);

                Allocations.Add(block);
                Allocations.Sort((a, b) => a.VirtualAddress.CompareTo(b.VirtualAddress));

                // bool fast = TrySetFastmem(ref block);
                block.IsFastmem = true;
            }
        }

        private static void RemoveAllocationBlock(ref MemoryBlock block)
        {   // Does not free
            lock (_managerLock)
            {

                uint startPage = block.VirtualAddress >> (int)Fastmem.PAGE_SHIFT;
                uint numberOfPages = block.Size / Fastmem.PAGE_SIZE;
                uint endPage = startPage + numberOfPages;
                MemoryBlock defaultBlock = default;
                SetMemoryBlockTable((int)startPage, (int)endPage, ref defaultBlock);

                Allocations.Remove(block);

                //if (block.IsFastmem) {
                //    UnsetFastmem(ref block);
                //}
            }
        }

        internal static void SetMemoryBlock(ref MemoryBlock block, int index)
        {
            lock (_managerLock)
            {
                MemoryBlockTable[index] = block;
            }
        }

        internal static void UnsetMemoryBlock(int index)
        {
            lock (_managerLock)
            {
                MemoryBlockTable[index] = default;
            }
        }

        internal static void SetMemoryBlockTable(int start, int end, ref MemoryBlock block)
        {
            lock (_managerLock)
            {
                for (int i = start; i < end; i++)
                {
                    MemoryBlockTable[i] = block;
                }
            }
        }

        internal static MemoryBlock AllocateStackSpace(uint size)
        {
            size = PageAlign(size); // Ensure size is page aligned
            MemoryBlock block = AllocateVirtualAddress(NextStackAllocationAddress, size);
            NextStackAllocationAddress += size;
            return block;
        }

        internal static MemoryBlock AllocateVirtualAddress(uint virtualAddress, uint size)
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

        internal static MemoryBlock AllocateVirtualMemory(uint size, uint alignment = 0)
        {
            size = PageAlign(size);  // Ensure size is page aligned

            lock (_managerLock)
            {
                //If the next address to be allocated falls into an already allocated space then skip it
                while (IsVirtualAddressAllocated(NextAllocationAddress))
                {
                    MemoryBlock usedBlock = GetAllocationByVirtualAddress(NextAllocationAddress);
                    NextAllocationAddress += (uint)usedBlock.Size;
                }

                if (alignment > 0)
                {
                    bool isPowerOf2 = (alignment & (alignment - 1)) == 0;
                    NextAllocationAddress = isPowerOf2 ? AlignPowerOfTwo(NextAllocationAddress, alignment) : Align(NextAllocationAddress, alignment);
                }

                //Allocate memory
                MemoryBlock emptyBlock = AllocateVirtualAddress(NextAllocationAddress, size);

                //Advance the next allocation address
                NextAllocationAddress += (uint)size;

                return emptyBlock;
            }
        }

        internal static MemoryBlock AllocatePhysicalMemory(uint size)
        {
            lock (_managerLock)
            {
                // Just allocate a virtual address -- we can find it by handle and remap it when requested
                return AllocateVirtualMemory(size);
            }
        }

        internal static MemoryBlock ResizeBlock(MemoryBlock block, uint newSize)
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

        internal static void FreeByVirtualAddress(uint virtualAddress)
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

        internal static void FreeByHandle(uint handle)
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

        internal static MemoryBlock AllocateTLS()
        {
            lock (_managerLock)
            {
                if (!TLS_Image.IsNullPtr)
                {
                    //Copy tls image to a newly allocated area per thread
                    MemoryBlock thread_tls = AllocateVirtualMemory(TLS_Image_Size + 0x30);
                    void* src = GetNativeAddress(TLS_Image);
                    NativeMemory.Copy(src, (byte*)thread_tls.NativeAddress + 0x30, (nuint)TLS_Image_Size);
                    return thread_tls;

                }
                else
                {
                    //This means the ELF did not have a TLS segment
                    throw new Exception($"TLSImage was not found during elf loading");
                }
            }
        }

        internal static void SetTLS(uint virtualAddress, uint size)
        {
            lock (_managerLock)
            {
                //Save the address of the TLS image (which can be in the middle of a block)
                TLS_Image = virtualAddress;
                TLS_Image_Size = size;
            }
        }

        internal static bool IsVirtualAddressAllocated(uint virtualAddress)
        {
            lock (_managerLock)
            {
                for (int i = 0; i < Allocations.Count; i++)
                {
                    MemoryBlock block = Allocations[i];
                    if (virtualAddress >= block.VirtualAddress && virtualAddress < block.End)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal static bool IsVirtualHandleAllocated(uint handle)
        {
            lock (_managerLock)
            {
                for (int i = 0; i < Allocations.Count; i++)
                {
                    MemoryBlock block = Allocations[i];
                    if (block.Handle == handle)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal static uint FindAllocatableAddress(uint targetAddress, uint size)
        {
            lock (_managerLock)
            {
                uint currentAddress = targetAddress;

                //The list is already sorted
                for (int i = 0; i < Allocations.Count; i++)
                {
                    if (currentAddress + size <= Allocations[i].VirtualAddress)
                    {
                        return currentAddress;
                    }

                    if (currentAddress < Allocations[i].End)
                    {
                        currentAddress = (uint)Allocations[i].End;
                    }
                }

                return currentAddress;
            }
        }

        internal static MemoryBlock GetAllocationByVirtualAddress(uint virtualAddress)
        {
            lock (_managerLock)
            {
                //Search in the allocation list
                for (int i = 0; i < Allocations.Count; i++)
                {
                    MemoryBlock block = Allocations[i];
                    if (virtualAddress >= block.VirtualAddress && virtualAddress < block.End)
                    {
                        return block;
                    }
                }

                throw new PageFaultException(virtualAddress);
            }
        }

        internal static MemoryBlock GetAllocationByHandle(ulong handle)
        {
            lock (_managerLock)
            {
                for (int i = 0; i < Allocations.Count; i++)
                {
                    MemoryBlock block = Allocations[i];
                    if (block.Handle == handle)
                    {
                        return block;
                    }
                }

                throw new Exception($"Can't find handle: {handle}");
            }
        }

        internal static bool IsHandleAllocated(ulong handle)
        {
            lock (_managerLock)
            {
                for (int i = 0; i < Allocations.Count; i++)
                {
                    MemoryBlock block = Allocations[i];
                    if (block.Handle == handle)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal static uint RemapByHandle(uint handle, uint destinationStart)
        {
            lock (_managerLock)
            {
                MemoryBlock block = GetAllocationByHandle(handle);
                Allocations.Remove(block);

                //if (block.IsFastmem) {
                //    UnsetFastmem(ref block);
                //}

                uint newAddress = FindAllocatableAddress(destinationStart, (uint)block.Size);
                block.VirtualAddress = newAddress;
                AddAllocationBlock(ref block);
                return newAddress;
            }
        }

        internal static uint RemapByVirtualAddress(uint virtualAddress, uint destinationStart)
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

        internal static void CopyBufferToVirtualMemory(ReadOnlySpan<byte> source, uint destinationVirtualAddress)
        {
            // Direct copy
            byte* nativeDestination = (byte*)GetNativeAddress(destinationVirtualAddress);
            Span<byte> destSpan = new(nativeDestination, source.Length);
            source.CopyTo(destSpan);
        }

        internal static void ReadBufferFromVirtualMemory(uint sourceVirtualAddress, Span<byte> destination)
        {
            // Direct copy
            byte* nativeSource = (byte*)GetNativeAddress(sourceVirtualAddress);
            Span<byte> sourceSpan = new(nativeSource, destination.Length);
            sourceSpan.CopyTo(destination);
        }

        internal static void CopyVirtualMemory(uint sourceVirtualAddress, uint destinationVirtualAddress, int byteCount)
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

        internal static void ClearMemory(uint virtualAddress, uint byteCount, byte value = 0)
        {
            if (byteCount == 0)
            {
                return;
            }

            void* dest = GetNativeAddress((uint)virtualAddress);
            NativeMemory.Fill(dest, byteCount, value);
        }

        internal static string ReadString(uint virtualAddress)
        {
            byte* native = (byte*)GetNativeAddress(virtualAddress);
            return Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(native));
        }

        internal static unsafe Span<T> CreateSpan<T>(uint virtualAddress, int length) where T : unmanaged
        { // Must be in unsafe conte
            byte* ptr = (byte*)GetNativeAddress(virtualAddress);
            return new Span<T>(ptr, length);
        }

        // Returns a ref of a value without flipping it -- Use carefully !
        internal static unsafe ref T DangerousBigEndianRef<T>(uint virtualAddress)
        {
            T* ptr = (T*)GetNativeAddress(virtualAddress);
            return ref *ptr;
        }
    }
}