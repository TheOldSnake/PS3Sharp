using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Common
{
    internal sealed class ScratchBuffer
    {
        // This buffer is ment to be reused and hence it can grow if needed,
        // and will stay at the maximum size.
        private byte[] _data;

        internal int LastLength { get; private set; }
        internal int CurrentCapacity => _data.Length;

        internal ScratchBuffer(int initialByteSize = 4 * 1024)
        {
            _data = new byte[initialByteSize];
        }

        internal void Clear()
        {
            _data.AsSpan().Clear();
        }

        internal Span<T> RequestBuffer<T>(int elementsCount) where T : unmanaged
        {
            int requiredSize = checked(elementsCount * Unsafe.SizeOf<T>());
            LastLength = requiredSize;

            // Resize if needed
            if (requiredSize > _data.Length)
            {
                Array.Resize(ref _data, requiredSize);
            }

            return MemoryMarshal.Cast<byte, T>(_data.AsSpan(0, requiredSize));
        }

        internal Span<T> GetLastRequestedBuffer<T>() where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(_data.AsSpan(0, LastLength));
        }
    }
}
