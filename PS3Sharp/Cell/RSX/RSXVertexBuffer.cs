using OpenTK.Graphics.OpenGL4;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.IO.Hashing;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX {
    public class RSXVertexBuffer {

        // Attributes 
        public bool IsEnabled { get; set; }
        public uint BufferAddress { get; set; }
        public VertexAttribPointerType AttribType { get; set; }
        public uint NumberOfElements { get; set; }
        public uint Stride { get; set; }
        public bool IsNormalized { get; set; }

        // Immediate data is currently only for floats
        public List<float> ImmediateData { get; } = new List<float>();
        public Span<float> ImmediateDataSpan => CollectionsMarshal.AsSpan(ImmediateData);
        public bool HasImmediateData => ImmediateData.Count > 0;

        // Scratch buffer to hold data
        public ScratchBuffer ScratchBuffer { get; } = new ScratchBuffer();
        public VertexCacheKey CacheKey = new VertexCacheKey();

        public struct VertexCacheKey {
            public uint Address;
            public uint Type;
            public uint NumberOfElements;
            public uint Stride;
            public ulong Hash;
        }

        public Span<T> GetScratchData<T>(int elementsCount) where T: unmanaged {
            Span<T> data = ScratchBuffer.RequestBuffer<T>(elementsCount);
            return data;
        }

        public Span<byte> GetLastScratchData() {
            return ScratchBuffer.GetLastRequestedBuffer<byte>();
        }

        public void FetchVertexData(uint vertexCount, uint startOffset) {
            switch (AttribType) {
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

        public void FetchVertexBufferData<T>(uint vertexCount, uint startOffset) where T : unmanaged {
            uint bufferAddress = BufferAddress;
            uint elementsPerUnitCount = NumberOfElements;
            uint stride = Stride;

            uint sizeOfT = (uint)Unsafe.SizeOf<T>();

            //Should never happen
            if (stride < (ulong)elementsPerUnitCount * sizeOfT) {
                throw new Exception($"Invalid Stride: {stride}");
            }

            bufferAddress += (startOffset * stride);     // advance by the start amount of verts
            int totalElements = (int)(vertexCount * elementsPerUnitCount);

            // Write into a span over the scratch buffer
            Span<T> vertexData = GetScratchData<T>(totalElements);

            // Fill in the data
            for (uint dataIndex = 0; dataIndex < vertexCount; dataIndex++) {
                uint dataMemoryOffset = dataIndex * stride;
                uint dataBaseIndex = dataIndex * elementsPerUnitCount;

                for (uint element = 0; element < elementsPerUnitCount; element++) {
                    uint address = bufferAddress + dataMemoryOffset + (element * sizeOfT);
                    T data = MemoryManager.Read<T>(address);
                    vertexData[(int)(dataBaseIndex + element)] = data;
                }
            }
        }

        public void UpdateHashKey() {
            Span<byte> data = GetLastScratchData();
            CacheKey.Address = BufferAddress;
            CacheKey.Type = (uint)AttribType;
            CacheKey.NumberOfElements = NumberOfElements;
            CacheKey.Stride = Stride;
            CacheKey.Hash = XxHash3.HashToUInt64(data);
        }

        public void PrintInfo(int location) { // For debugging
            string inputName = VertexShaderGenerator.InputNames[location];
            Console.WriteLine($"[RSX] Setting up vertex buffer {location} ({inputName}) - type = {AttribType}, address = 0x{BufferAddress:X8}, n_elements = {NumberOfElements}, stride = {Stride}");
        }
    }
}
