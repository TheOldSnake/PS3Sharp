using OpenTK.Graphics.OpenGL4;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    internal sealed class RSXVertexBuffer
    {
        private readonly MemoryManager _memoryManager;

        internal RSXVertexBuffer(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        // Attributes 
        internal bool IsEnabled { get; set; }
        internal uint BufferAddress { get; set; }
        internal VertexAttribPointerType AttribType { get; set; }
        internal uint NumberOfElements { get; set; }
        internal uint Stride { get; set; }
        internal bool IsNormalized { get; set; }

        // Immediate data is currently only for floats
        internal List<float> ImmediateData { get; } = new List<float>();
        internal Span<float> ImmediateDataSpan => CollectionsMarshal.AsSpan(ImmediateData);
        internal bool HasImmediateData => ImmediateData.Count > 0;

        // Scratch buffer to hold data
        internal ScratchBuffer ScratchBuffer { get; } = new ScratchBuffer();
        internal VertexCacheKey CacheKey = new();

        internal struct VertexCacheKey
        {
            internal uint Address;
            internal uint Type;
            internal uint NumberOfElements;
            internal uint Stride;
            internal ulong Hash;
        }

        internal Span<T> GetScratchData<T>(int elementsCount) where T : unmanaged
        {
            Span<T> data = ScratchBuffer.RequestBuffer<T>(elementsCount);
            return data;
        }

        internal Span<byte> GetLastScratchData()
        {
            return ScratchBuffer.GetLastRequestedBuffer<byte>();
        }

        internal void FetchVertexData(uint vertexCount, uint startOffset)
        {
            switch (AttribType)
            {
                case VertexAttribPointerType.Float:
                    FetchVertexBufferData<float>(vertexCount, startOffset);
                    break;

                //use ushort because half float is 2 bytes
                case VertexAttribPointerType.HalfFloat:
                    FetchVertexBufferData<ushort>(vertexCount, startOffset);
                    break;

                case VertexAttribPointerType.UnsignedByte:
                    FetchVertexBufferData<byte>(vertexCount, startOffset);
                    break;

                case VertexAttribPointerType.Short:
                    FetchVertexBufferData<short>(vertexCount, startOffset);
                    break;

                default:
                    throw new Exception($"[RSX] Unhandled AttribType: {AttribType}");
            }
        }

        internal void FetchVertexBufferData<T>(uint vertexCount, uint startOffset) where T : unmanaged
        {
            uint bufferAddress = BufferAddress;
            uint elementsPerUnitCount = NumberOfElements;
            uint stride = Stride;

            uint sizeOfT = (uint)Unsafe.SizeOf<T>();

            //Should never happen
            if (stride < (ulong)elementsPerUnitCount * sizeOfT)
            {
                throw new Exception($"Invalid Stride: {stride}");
            }

            bufferAddress += (startOffset * stride);     // advance by the start amount of verts
            int totalElements = (int)(vertexCount * elementsPerUnitCount);

            // Write into a span over the scratch buffer
            Span<T> vertexData = GetScratchData<T>(totalElements);

            // Fill in the data
            for (uint dataIndex = 0; dataIndex < vertexCount; dataIndex++)
            {
                uint dataMemoryOffset = dataIndex * stride;
                uint dataBaseIndex = dataIndex * elementsPerUnitCount;

                for (uint element = 0; element < elementsPerUnitCount; element++)
                {
                    uint address = bufferAddress + dataMemoryOffset + (element * sizeOfT);
                    T data = _memoryManager.Read<T>(address);
                    vertexData[(int)(dataBaseIndex + element)] = data;
                }
            }
        }

        internal void UpdateHashKey()
        {
            Span<byte> data = GetLastScratchData();
            CacheKey.Address = BufferAddress;
            CacheKey.Type = (uint)AttribType;
            CacheKey.NumberOfElements = NumberOfElements;
            CacheKey.Stride = Stride;
            CacheKey.Hash = XxHash3.HashToUInt64(data);
        }

        internal void PrintInfo(int location)
        { // For debugging
            string inputName = VertexShaderGenerator.InputNames[location];
            Console.WriteLine($"[RSX] Setting up vertex buffer {location} ({inputName}) - type = {AttribType}, address = 0x{BufferAddress:X8}, n_elements = {NumberOfElements}, stride = {Stride}");
        }
    }
}
