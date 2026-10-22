using OpenTK.Graphics.OpenGL4;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.IO.Hashing;
using static PS3Sharp.Cell.RSX.RSXCommand;

namespace PS3Sharp.Cell.RSX
{
    internal sealed class RSXTexture(RealitySynthesizer rsx)
    {
        internal struct Swizzle
        {
            internal int A;
            internal int R;
            internal int G;
            internal int B;
        }

        internal struct TextureCacheKey
        {
            internal uint Address;
            internal uint Width;
            internal uint Height;
            internal ulong Hash;
        }

        private static ReadOnlySpan<int> _swizzleTable => [(int)All.Alpha, (int)All.Red, (int)All.Green, (int)All.Blue];
        private static readonly string[] _swizzleStringTable = ["a", "r", "g", "b"];
        internal static readonly Swizzle DefaultSwizzle = new()
        {
            A = _swizzleTable[0],
            R = _swizzleTable[1],
            G = _swizzleTable[2],
            B = _swizzleTable[3],
        };

        // Atributes 
        internal uint Address => rsx.OffsetToAddress(Offset, MemoryLocation); // Final address
        internal uint Offset { get; set; }         // Offset in memory     
        internal uint MemoryLocation { get; set; } // RSX memory or CELL
        internal uint Format { get; set; }
        internal uint Filter { get; set; }
        internal uint Wrap { get; set; }
        internal uint SwizzleValue { get; set; }
        internal uint Width { get; set; }
        internal uint Height { get; set; }
        internal uint Pitch { get; set; }
        internal uint Depth { get; set; }
        internal bool IsEnabled { get; set; }

        internal uint PixelFormatValue => Format & 0x00001F00;
        internal bool IsSurface => rsx.AddressIsSurface(Address);

        internal ScratchBuffer ScratchBuffer { get; } = new ScratchBuffer();
        internal TextureCacheKey CacheKey = new();

        internal int GetSizeInBytes()
        {
            int pitch = (int)Pitch;
            int height = (int)Height;
            return IsCompressed() ? GetCompressedSize() : pitch * height;
        }

        internal Span<byte> GetScratchData()
        {
            int sizeInBytes = GetSizeInBytes();
            Span<byte> data = ScratchBuffer.RequestBuffer<byte>(sizeInBytes);
            return data;
        }

        internal Span<byte> GetLastScratchData()
        {
            return ScratchBuffer.GetLastRequestedBuffer<byte>();
        }

        internal PixelInternalFormat GetPixelInternalFormat()
        {
            switch (PixelFormatValue)
            {
                case Nv40TclTexFormatFormatA8r8g8b8: return PixelInternalFormat.Rgba8;
                case Nv40TclTexFormatFormatR5g6b5: return (PixelInternalFormat)All.Rgb565;
                case Nv40TclTexFormatFormatG8b8: return PixelInternalFormat.Rg8;
                case Nv40TclTexFormatFormatL8: return PixelInternalFormat.R8;

                case Nv40TclTexFormatFormatDxt1: return PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                case Nv40TclTexFormatFormatDxt3: return PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                case Nv40TclTexFormatFormatDxt5: return PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;

                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Format = 0x{PixelFormatValue:X8}");
            }
        }

        internal InternalFormat GetCompressedInternalFormat()
        {
            // For GL.CompressedTexImage2D

            switch (PixelFormatValue)
            {
                case Nv40TclTexFormatFormatDxt1: return InternalFormat.CompressedRgbaS3tcDxt1Ext;  //CELL_GCM_TEXTURE_DXT1 - used by LBP and gta sa
                case Nv40TclTexFormatFormatDxt3: return InternalFormat.CompressedRgbaS3tcDxt3Ext;  //CELL_GCM_TEXTURE_DXT3 - used by gta sa
                case Nv40TclTexFormatFormatDxt5: return InternalFormat.CompressedRgbaS3tcDxt5Ext;  //CELL_GCM_TEXTURE_DXT3 - used by gta sa
                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Compressed Format = 0x{PixelFormatValue:X8}");
            }
        }

        internal PixelFormat GetPixelFormat()
        {
            switch (PixelFormatValue)
            {
                case Nv40TclTexFormatFormatA8r8g8b8: return PixelFormat.Bgra; // ARGB in big endian == BGRA

                case Nv40TclTexFormatFormatR5g6b5: return PixelFormat.Rgb;
                case Nv40TclTexFormatFormatG8b8: return PixelFormat.Rg;
                case Nv40TclTexFormatFormatL8: return PixelFormat.Red;

                case Nv40TclTexFormatFormatDxt1: return PixelFormat.Rgba;
                case Nv40TclTexFormatFormatDxt3: return PixelFormat.Rgba;
                case Nv40TclTexFormatFormatDxt5: return PixelFormat.Rgba;
                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Format = 0x{PixelFormatValue:X8}");
            }
        }

        internal PixelType GetPixelType()
        {

            switch (PixelFormatValue)
            {
                case Nv40TclTexFormatFormatR5g6b5: return PixelType.UnsignedShort565;

                default:
                    return PixelType.UnsignedByte;
            }
        }

        internal int GetBytesPerPixel()
        {
            switch (PixelFormatValue)
            {
                case Nv40TclTexFormatFormatA8r8g8b8: return 4;
                case Nv40TclTexFormatFormatR5g6b5: return 2;
                case Nv40TclTexFormatFormatG8b8: return 2;
                case Nv40TclTexFormatFormatL8: return 1;

                default:
                    throw new NotImplementedException($"[RSX] TODO: Texture Format = 0x{PixelFormatValue:X8}");
            }
        }

        internal bool IsCompressed()
        {
            uint pixelFormat = PixelFormatValue;

            bool isCompressed =
                pixelFormat == Nv40TclTexFormatFormatDxt1 ||
                pixelFormat == Nv40TclTexFormatFormatDxt3 ||
                pixelFormat == Nv40TclTexFormatFormatDxt5;

            return isCompressed;
        }

        internal int GetCompressedSize()
        {
            // DXT texture is divided into 4×4 blocks
            // the size of a single tile is either 8 or 16 bytes depending on the type

            float width = Width;
            float height = Height;

            int blocksWidth = (int)Math.Ceiling(width / 4.0f);  // How many blocks wide
            int blockHeight = (int)Math.Ceiling(height / 4.0f); // How many blocks high

            int bytesPerBlock = PixelFormatValue switch
            {
                Nv40TclTexFormatFormatDxt1 => 8,
                Nv40TclTexFormatFormatDxt3 => 16,
                Nv40TclTexFormatFormatDxt5 => 16,
                _ => throw new Exception($"Texture format 0x{PixelFormatValue:X8} is not compressed"),
            };

            return blocksWidth * blockHeight * bytesPerBlock;
        }

        internal Swizzle GetSwizzle()
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
            if (pixelFormat == Nv40TclTexFormatFormatL8)
            {
                swizzleResult.A = _swizzleTable[1];
                swizzleResult.R = _swizzleTable[1];
                swizzleResult.G = _swizzleTable[1];
                swizzleResult.B = _swizzleTable[1];
                return swizzleResult;
            }


            // NV40TCL_TEX_FORMAT_FORMAT_G8B8 
            if (pixelFormat == Nv40TclTexFormatFormatG8b8)
            {
                swizzleResult.A = _swizzleTable[1]; // R
                swizzleResult.R = _swizzleTable[2]; // G
                swizzleResult.G = _swizzleTable[1]; // R
                swizzleResult.B = _swizzleTable[2]; // G
                return swizzleResult;
            }

            // TODO: Handle more special cases

            if (pixelFormat == Nv40TclTexFormatFormatA8r8g8b8 && !IsSurface)
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

        internal TextureMinFilter GetMinFilter()
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

        internal TextureMagFilter GetMagFilter()
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

        internal TextureWrapMode GetWrapS()
        {
            uint wrapS = Wrap & 0xFF;
            return GetWrap(wrapS);
        }

        internal TextureWrapMode GetWrapT()
        {
            uint wrapT = (Wrap >> 8) & 0xFF;
            return GetWrap(wrapT);
        }

        internal TextureWrapMode GetWrapR()
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

        internal void FetchTextureData(MemoryManager memoryManager)
        {
            Span<byte> destination = GetScratchData();

            // Read a copy of the texture into the scratch buffer
            memoryManager.ReadBufferFromVirtualMemory(Address, destination);
        }

        internal void UpdateHashKey()
        {
            ReadOnlySpan<byte> data = GetLastScratchData();
            CacheKey.Address = Address;
            CacheKey.Width = Width;
            CacheKey.Height = Height;
            CacheKey.Hash = XxHash3.HashToUInt64(data);
        }

        internal void PrintInfo(int location)
        { // For debugging
            Console.WriteLine($"Texture[{location}] addr = 0x{Address:X8} w = {Width} h = {Height} pitch = {Pitch} swizzle = 0x{SwizzleValue:X} Format = 0x{Format:X8}");
        }
    }
}
