using OpenTK.Graphics.OpenGL4;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PS3Sharp.Cell.RSX
{
    public sealed class RSXElementBuffer
    {
        public uint BufferAddress { get; set; }                 // Index list address
        public DrawElementsType ElementBufferType { get; set; } // Index list element type

        public int TotalElements { get; private set; }

        public ScratchBuffer ScratchBuffer = new();

        // I don't like passing the Queue here
        public uint SetupElementBuffer(Queue<BigEndian<uint>> param)
        {
            uint max = 0;

            switch (ElementBufferType)
            {
                case DrawElementsType.UnsignedShort:
                    DecodeAndFill<ushort>(param);
                    max = GetMax<ushort>();
                    break;

                default: // TODO
                    throw new Exception($"[TODO] Unimplemented ElementBufferType: {ElementBufferType}");
            }

            return max;
        }

        private void DecodeAndFill<T>(Queue<BigEndian<uint>> param) where T : unmanaged, INumber<T>
        {
            while (param.Count > 0)
            {
                uint info = param.Dequeue();
                uint startIndex = info & 0xFFFFFF;
                uint indicesCount = (info >> 24) + 1;
                FillIndexList<T>(startIndex, indicesCount);
            }
        }

        private void FillIndexList<T>(uint indexStart, uint indicesCount) where T : unmanaged, INumber<T>
        {
            uint sizeOfT = (uint)Unsafe.SizeOf<T>();
            int previousTotal = TotalElements;
            uint currentAddress = BufferAddress + (indexStart * sizeOfT);

            // This is to append data to the buffer without overwriting - we need a better way.
            int newTotal = previousTotal + (int)indicesCount;
            Span<T> buffer = ScratchBuffer.RequestBuffer<T>(newTotal).Slice(previousTotal);

            // Note: We can't use MemoryManager.ReadBufferFromVirtualMemory because it does not handle endianness

            for (uint i = 0; i < buffer.Length; i++)
            {
                buffer[(int)i] = MemoryManager.Read<T>(currentAddress);
                currentAddress += sizeOfT;
            }

            TotalElements = newTotal;
        }

        public T GetMax<T>() where T : unmanaged, INumber<T>
        {
            Span<T> buffer = ScratchBuffer.GetLastRequestedBuffer<T>();
            T max = buffer[0];

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] > max)
                {
                    max = buffer[i];
                }
            }

            return max;
        }

        public Span<byte> GetLastScratchData()
        {
            return ScratchBuffer.GetLastRequestedBuffer<byte>();
        }

        public void Reset()
        {
            TotalElements = 0;
        }
    }
}
