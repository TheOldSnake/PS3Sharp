using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.GuestMemory
{
    internal sealed unsafe partial class MemoryManager : IDisposable
    {
        // Memory lock for operations that require synchronization
        private readonly Lock _memoryLock = new();

        // Seperate list of reservation keys (thread IDs), which makes iterating the dictionary easier
        private readonly List<uint> _reservationKeys = [];
        private readonly Dictionary<uint, MemoryReservation> _currentReservations = [];

        // Tls image data (should probably move it somewhere else)
        private byte[]? _tlsData;

        // Memory allocator
        internal MemoryAllocator Allocator { get; }

        internal MemoryManager()
        {
            Allocator = new MemoryAllocator(_memoryLock);
        }

        internal uint GetHandle() => Allocator.CreateHandle();

        // Returns a native pointer to the struct in the -unmanaged- guest memory
        internal T* GetPtr<T>(GuestPtr<T> ptr) where T : unmanaged
        {
            return (T*)GetNativeAddress(ptr.Address);
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

        internal T LoadAndReserve<T>(uint virtualAddress, uint threadId) where T : unmanaged
        {
            using (_memoryLock.EnterScope())
            {
                ReserveAddress(virtualAddress, sizeof(T), threadId);
                T value = Read<T>(virtualAddress);
                return value;
            }
        }

        internal void LoadAndReserve(uint virtualAddress, uint threadId, Span<byte> destination, ManualResetEventSlim lostReservationEvent)
        {
            using (_memoryLock.EnterScope())
            {
                ReserveAddress(virtualAddress, destination.Length, threadId, lostReservationEvent);
                CopyBufferFromVirtualMemory(virtualAddress, destination);
            }
        }

        internal bool StoreConditional<T>(uint virtualAddress, uint threadId, T value) where T : unmanaged
        {
            using (_memoryLock.EnterScope())
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

        internal bool StoreConditional(uint virtualAddress, uint threadId, ReadOnlySpan<byte> src)
        {
            using (_memoryLock.EnterScope())
            {
                // Acquire reservation and only write if successful 
                bool ok = TryAccess(virtualAddress, src.Length, threadId);

                if (ok)
                {
                    CopyBufferToVirtualMemory(src, virtualAddress);
                }

                return ok;
            }
        }

        internal void RegisterReservationKey(uint threadId)
        {
            using (_memoryLock.EnterScope())
            {
                // Register the thread id as a reservation key
                if (!_reservationKeys.Contains(threadId))
                {
                    _reservationKeys.Add(threadId);
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected duplicate thread id key: 0x{threadId:X8}");
                }
            }
        }

        internal void ReserveAddress(uint virtualAddress, int size, uint threadId, ManualResetEventSlim? lostReservationEvent = null)
        {
            MemoryReservation reservation = new()
            {
                AddressStart = virtualAddress,
                Size = (uint)size,
                ThreadID = threadId,
                LostReservationEvent = lostReservationEvent
            };

            using (_memoryLock.EnterScope())
            {
                // One reservation per thread, apprearently
                _currentReservations[threadId] = reservation;
            }
        }

        internal bool TryAccess(uint virtualAddress, int size, uint threadId)
        {
            using (_memoryLock.EnterScope())
            {
                // Fail if no reservation exists for this thread
                if (!_currentReservations.TryGetValue(threadId, out MemoryReservation reservation))
                {
                    return false;
                }

                // Fail if the access is not within the reserved space
                if (!reservation.Contains(virtualAddress, (ulong)size))
                {
                    return false;
                }

                // Access is OK, invalidate all reservations that overlaps
                InvalidateReservations(virtualAddress, size);
                return true;
            }
        }

        internal void InvalidateReservations(uint virtualAddress, int size)
        {
            using (_memoryLock.EnterScope())
            {
                foreach (uint key in _reservationKeys)
                {
                    if (_currentReservations.TryGetValue(key, out MemoryReservation reservation))
                    {
                        if (reservation.OverlapsWith(virtualAddress, (ulong)size))
                        {
                            _currentReservations.Remove(key);
                            reservation.SetEvent();
                        }
                    }
                }
            }
        }

        internal MemoryBlock AllocateTlsBlock()
        {
            using (_memoryLock.EnterScope())
            {
                ArgumentNullException.ThrowIfNull(_tlsData, $"Tls data is null");

                // Copy tls image to a newly allocated area per thread
                MemoryBlock threadTls = Allocator.AllocateVirtualMemory((uint)(_tlsData.Length + 0x30));
                CopyBufferToVirtualMemory(_tlsData, threadTls.VirtualAddress + 0x30);

                return threadTls;
            }
        }

        internal void SetTlsImage(byte[]? tls)
        {
            using (_memoryLock.EnterScope())
            {
                _tlsData = tls;
            }
        }

        internal void CopyBufferToVirtualMemory(ReadOnlySpan<byte> source, uint destinationVirtualAddress)
        {
            byte* nativeDestination = (byte*)GetNativeAddress(destinationVirtualAddress);
            Span<byte> destinationSpan = new(nativeDestination, source.Length);
            source.CopyTo(destinationSpan);
        }

        internal void CopyBufferFromVirtualMemory(uint sourceVirtualAddress, Span<byte> destination)
        {
            byte* nativeSource = (byte*)GetNativeAddress(sourceVirtualAddress);
            Span<byte> sourceSpan = new(nativeSource, destination.Length);
            sourceSpan.CopyTo(destination);
        }

        internal void CopyVirtualMemory(uint sourceVirtualAddress, uint destinationVirtualAddress, int byteCount)
        {
            byte* nativeSource = (byte*)GetNativeAddress(sourceVirtualAddress);
            byte* nativeDestination = (byte*)GetNativeAddress(destinationVirtualAddress);

            Span<byte> sourceSpan = new(nativeSource, byteCount);
            Span<byte> destinationSpan = new(nativeDestination, byteCount);

            sourceSpan.CopyTo(destinationSpan);
        }

        internal void ClearVirtualMemory(uint virtualAddress, int byteCount, byte value = 0)
        {
            void* destination = GetNativeAddress(virtualAddress);
            Span<byte> destinationSpan = new(destination, byteCount);
            destinationSpan.Fill(value);
        }

        internal string ReadString(uint virtualAddress, int length = -1)
        {
            byte* nativeAddress = (byte*)GetNativeAddress(virtualAddress);
            ReadOnlySpan<byte> text = length > 0 ? new ReadOnlySpan<byte>(nativeAddress, length) : MemoryMarshal.CreateReadOnlySpanFromNullTerminated(nativeAddress);
            return Encoding.UTF8.GetString(text);
        }

        internal unsafe Span<byte> CreateByteSpan(uint virtualAddress, int length)
        {
            byte* nativeAddress = (byte*)GetNativeAddress(virtualAddress);
            return new Span<byte>(nativeAddress, length);
        }

        internal T[] ReadArray<T>(uint virtualAddress, int length) where T : unmanaged
        {
            T[] values = new T[length];

            Span<byte> asBytes = MemoryMarshal.Cast<T, byte>(values.AsSpan());
            CopyBufferFromVirtualMemory(virtualAddress, asBytes);

            return values;
        }

        internal void WriteArray<T>(uint virtualAddress, ReadOnlySpan<T> values) where T : unmanaged
        {
            ReadOnlySpan<byte> asBytes = MemoryMarshal.Cast<T, byte>(values);
            CopyBufferToVirtualMemory(asBytes, virtualAddress);
        }


        // Returns a native address for the given virtual address
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void* GetNativeAddress(uint virtualAddress)
        {
            // Debug
            //if (!IsAllocated(virtualAddress))
            //{
            //    throw new PageFaultException(virtualAddress);
            //}

            return (byte*)Allocator.GuestMemoryBase + virtualAddress;
        }

        public void Dispose()
        {
            Allocator.Dispose();
        }
    }
}