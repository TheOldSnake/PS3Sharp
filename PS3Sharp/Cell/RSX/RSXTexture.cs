using OpenTK.Graphics.OpenGL4;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.IO.Hashing;
using static PS3Sharp.Cell.RSX.RSXCommand;

namespace PS3Sharp.Cell.RSX
{
    public sealed class RSXTexture()
    {

        // Atributes 
        public uint Address => PS3Sharp.RSX.OffsetToAddress(Offset, MemoryLocation); // Final address
        public uint Offset { get; set; }         // Offset in memory     
        public uint MemoryLocation { get; set; } // RSX memory or CELL
        public uint Format { get; set; }
        public uint Filter { get; set; }
        public uint Wrap { get; set; }
        public uint SwizzleValue { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint Pitch { get; set; }
        public uint Depth { get; set; }
        public bool IsEnabled { get; set; }

        public uint PixelFormatValue => Format & 0x00001F00;
        public bool IsSurface => GLRenderer.AddressIsSurface(Address);

        public ScratchBuffer ScratchBuffer { get; } = new ScratchBuffer();
        public TextureCacheKey CacheKey = new();

        private static ReadOnlySpan<int> _swizzleTable => [(int)All.Alpha, (int)All.Red, (int)All.Green, (int)All.Blue];
        private static readonly string[] _swizzleStringTable = ["a", "r", "g", "b"];

        public static readonly Swizzle DefaultSwizzle = new()
        {
            A = _swizzleTable[0],
            R = _swizzleTable[1],
            G = _swizzleTable[2],
            B = _swizzleTable[3],
        };

        public struct Swizzle
        {
            public int A;
            public int R;
            public int G;
            public int B;
        }

        public struct TextureCacheKey
        {
            public uint Address;
            public uint Width;
            public uint Height;
            public ulong Hash;
        }

        public int GetSizeInBytes()
        {
            int pitch = (int)Pitch;
            int height = (int)Height;
            return IsCompressed() ? GetCompressedSize() : pitch * height;
        }

        public Span<byte> GetScratchData()
        {
            int sizeInBytes = GetSizeInBytes();
            Span<byte> data = ScratchBuffer.RequestBuffer<byte>(sizeInBytes);
            return data;
        }

        public Span<byte> GetLastScratchData()
        {
            return ScratchBuffer.GetLastRequestedBuffer<byte>();
        }

        public PixelInternalFormat GetPixelInternalFormat()
        {
            switch (PixelFormatValue)
            {
                case NV40TCL_TEX_FORMAT_FORMAT_A8R8G8B8: return PixelInternalFormat.Rgba8;
                case NV40TCL_TEX_FORMAT_FORMAT_R5G6B5: return (PixelInternalFormat)All.Rgb565;
                case NV40TCL_TEX_FORMAT_FORMAT_G8B8: return PixelInternalFormat.Rg8;
                case NV40TCL_TEX_FORMAT_FORMAT_L8: return PixelInternalFormat.R8;

                case NV40TCL_TEX_FORMAT_FORMAT_DXT1: return PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                case NV40TCL_TEX_FORMAT_FORMAT_DXT3: return PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                case NV40TCL_TEX_FORMAT_FORMAT_DXT5: return PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;

                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Format = 0x{PixelFormatValue:X8}");
            }
        }

        public InternalFormat GetCompressedInternalFormat()
        {
            // For GL.CompressedTexImage2D

            switch (PixelFormatValue)
            {
                case NV40TCL_TEX_FORMAT_FORMAT_DXT1: return InternalFormat.CompressedRgbaS3tcDxt1Ext;  //CELL_GCM_TEXTURE_DXT1 - used by LBP and gta sa
                case NV40TCL_TEX_FORMAT_FORMAT_DXT3: return InternalFormat.CompressedRgbaS3tcDxt3Ext;  //CELL_GCM_TEXTURE_DXT3 - used by gta sa
                case NV40TCL_TEX_FORMAT_FORMAT_DXT5: return InternalFormat.CompressedRgbaS3tcDxt5Ext;  //CELL_GCM_TEXTURE_DXT3 - used by gta sa
                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Compressed Format = 0x{PixelFormatValue:X8}");
            }
        }

        public PixelFormat GetPixelFormat()
        {
            switch (PixelFormatValue)
            {
                case NV40TCL_TEX_FORMAT_FORMAT_A8R8G8B8: return PixelFormat.Bgra; // ARGB in big endian == BGRA

                case NV40TCL_TEX_FORMAT_FORMAT_R5G6B5: return PixelFormat.Rgb;
                case NV40TCL_TEX_FORMAT_FORMAT_G8B8: return PixelFormat.Rg;
                case NV40TCL_TEX_FORMAT_FORMAT_L8: return PixelFormat.Red;

                case NV40TCL_TEX_FORMAT_FORMAT_DXT1: return PixelFormat.Rgba;
                case NV40TCL_TEX_FORMAT_FORMAT_DXT3: return PixelFormat.Rgba;
                case NV40TCL_TEX_FORMAT_FORMAT_DXT5: return PixelFormat.Rgba;
                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Format = 0x{PixelFormatValue:X8}");
            }
        }

        public PixelType GetPixelType()
        {

            switch (PixelFormatValue)
            {
                case NV40TCL_TEX_FORMAT_FORMAT_R5G6B5: return PixelType.UnsignedShort565;

                default:
                    return PixelType.UnsignedByte;
            }
        }

        public int GetBytesPerPixel()
        {
            switch (PixelFormatValue)
            {
                case NV40TCL_TEX_FORMAT_FORMAT_A8R8G8B8: return 4;
                case NV40TCL_TEX_FORMAT_FORMAT_R5G6B5: return 2;
                case NV40TCL_TEX_FORMAT_FORMAT_G8B8: return 2;
                case NV40TCL_TEX_FORMAT_FORMAT_L8: return 1;

                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Format = 0x{PixelFormatValue:X8}");
            }
        }

        public bool IsCompressed()
        {
            uint pixelFormat = PixelFormatValue;

            bool isCompressed =
                pixelFormat == NV40TCL_TEX_FORMAT_FORMAT_DXT1 ||
                pixelFormat == NV40TCL_TEX_FORMAT_FORMAT_DXT3 ||
                pixelFormat == NV40TCL_TEX_FORMAT_FORMAT_DXT5;

            return isCompressed;
        }

        public int GetCompressedSize()
        {
            // DXT texture is divided into 4×4 blocks
            // the size of a single tile is either 8 or 16 bytes depending on the type

            float width = Width;
            float height = Height;

            int blocksWidth = (int)Math.Ceiling(width / 4.0f);  // How many blocks wide
            int blockHeight = (int)Math.Ceiling(height / 4.0f); // How many blocks high

            int bytesPerBlock = PixelFormatValue switch
            {
                NV40TCL_TEX_FORMAT_FORMAT_DXT1 => 8,
                NV40TCL_TEX_FORMAT_FORMAT_DXT3 => 16,
                NV40TCL_TEX_FORMAT_FORMAT_DXT5 => 16,
                _ => throw new Exception($"Texture format 0x{PixelFormatValue:X8} is not compressed"),
            };

            return blocksWidth * blockHeight * bytesPerBlock;
        }

        public Swizzle GetSwizzle()
        {

            uint pixelFormat = PixelFormatValue;
            int swizzle = (int)SwizzleValue;

            // the encoding index here is reversed (WZYX)
            // check NV40TCL_TEX_SWIZZLE_S1 in PSL1GHT
            int sw_a = (swizzle & 3);
            int sw_r = ((swizzle >> 2) & 3);
            int sw_g = ((swizzle >> 4) & 3);
            int sw_b = ((swizzle >> 6) & 3);

            Swizzle swizzleResult;

            // Compressed texture
            if (IsCompressed())
            {
                return DefaultSwizzle;
            }

            // CELL_GCM_TEXTURE_B8 is a single channel format,
            // we use the RED channel for all of them
            if (pixelFormat == NV40TCL_TEX_FORMAT_FORMAT_L8)
            {
                swizzleResult.A = _swizzleTable[1];
                swizzleResult.R = _swizzleTable[1];
                swizzleResult.G = _swizzleTable[1];
                swizzleResult.B = _swizzleTable[1];
                return swizzleResult;
            }


            // NV40TCL_TEX_FORMAT_FORMAT_G8B8 
            if (pixelFormat == NV40TCL_TEX_FORMAT_FORMAT_G8B8)
            {
                swizzleResult.A = _swizzleTable[1]; // R
                swizzleResult.R = _swizzleTable[2]; // G
                swizzleResult.G = _swizzleTable[1]; // R
                swizzleResult.B = _swizzleTable[2]; // G
                return swizzleResult;
            }

            // TODO: Handle more special cases

            if (pixelFormat == NV40TCL_TEX_FORMAT_FORMAT_A8R8G8B8 && !IsSurface)
            {
                // For A8R8G8B8 mode we used BGRA which sets the color channels correctly
                // We have to reverse the swizzle lookup because our tabel is ARGB, but the indexer is BRGA
                swizzleResult.A = _swizzleTable[3 - sw_a];
                swizzleResult.R = _swizzleTable[3 - sw_r];
                swizzleResult.G = _swizzleTable[3 - sw_g];
                swizzleResult.B = _swizzleTable[3 - sw_b];

            }
            else
            {

                // For modes that were uploaded with normal RGBA, the channels are already reversed due to the memory being big endian
                // so we keep the indexer reversed too, so it matches the channels
                swizzleResult.A = _swizzleTable[sw_a];
                swizzleResult.R = _swizzleTable[sw_r];
                swizzleResult.G = _swizzleTable[sw_g];
                swizzleResult.B = _swizzleTable[sw_b];
            }

            return swizzleResult;
        }

        public TextureMinFilter GetMinFilter()
        {
            uint minFilter = (Filter >> 16) & 0xF;

            switch (minFilter)
            {
                case 1: return TextureMinFilter.Nearest;
                case 2: return TextureMinFilter.Linear;
                case 3: return TextureMinFilter.NearestMipmapNearest;
                case 4: return TextureMinFilter.LinearMipmapNearest;
                case 5: return TextureMinFilter.NearestMipmapLinear;
                case 6: return TextureMinFilter.LinearMipmapLinear;

                default:
                    throw new Exception($"Unknown TexMinFilter = {minFilter}");
            }
        }

        public TextureMagFilter GetMagFilter()
        {
            uint magFilter = (Filter >> 24) & 0xF;

            switch (magFilter)
            {
                case 1: return TextureMagFilter.Nearest;
                case 2: return TextureMagFilter.Linear;

                // These should be like MinFilter, but we don't have this.. ?
                case 3:
                case 4:
                case 5:
                case 6: return TextureMagFilter.Nearest;

                default:
                    throw new Exception($"Unknown TexMagFilter = {magFilter}");
            }
        }

        public TextureWrapMode GetWrapS()
        {
            uint wrapS = Wrap & 0xFF;
            return GetWrap(wrapS);
        }

        public TextureWrapMode GetWrapT()
        {
            uint wrapT = (Wrap >> 8) & 0xFF;
            return GetWrap(wrapT);
        }

        public TextureWrapMode GetWrapR()
        {
            uint wrapR = (Wrap >> 16) & 0xFF;
            return GetWrap(wrapR);
        }

        private TextureWrapMode GetWrap(uint wrap)
        {
            switch (wrap)
            {
                case 1: return TextureWrapMode.Repeat;
                case 2: return TextureWrapMode.MirroredRepeat;
                case 3: return TextureWrapMode.ClampToEdge;
                case 4: return TextureWrapMode.ClampToBorder;

                // I don't know what to use here
                case 5:
                case 6:
                case 7:
                case 8:
                    return TextureWrapMode.Repeat;

                default:
                    throw new Exception($"Unknown Wrap = {wrap}");
            }
        }

        public void FetchTextureData()
        {
            Span<byte> destination = GetScratchData();

            // Read a copy of the texture into the scratch buffer
            MemoryManager.ReadBufferFromVirtualMemory(Address, destination);
        }

        public void UpdateHashKey()
        {
            ReadOnlySpan<byte> data = GetLastScratchData();
            CacheKey.Address = Address;
            CacheKey.Width = Width;
            CacheKey.Height = Height;
            CacheKey.Hash = XxHash3.HashToUInt64(data);
        }

        public void PrintInfo(int location)
        { // For debugging
            Console.WriteLine($"Texture[{location}] addr = 0x{Address:X8} w = {Width} h = {Height} pitch = {Pitch} swizzle = 0x{SwizzleValue:X} Format = 0x{Format:X8}");
        }
    }
}
