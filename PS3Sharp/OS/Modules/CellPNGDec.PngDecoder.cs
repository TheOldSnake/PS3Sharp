using OpenTK.Graphics.OpenGL;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using SkiaSharp;
using StbImageSharp;
using System.Diagnostics;

namespace PS3Sharp.OS.Modules
{
    internal partial class CellPngDec
    {
        private class PngDecoder : IDisposable
        {
            internal uint Handle { get; }
            internal CellPngDecOutParam OutputParam { get; set; }
            internal ImageInfo ImageInfo { get; private set; }

            private readonly MemoryManager _memoryManager;
            private CellPngDecSrc _src;
            private byte[]? _srcData;
            private MemoryStream? _srcDataStream;

            internal int NumberOfComponents => ImageInfo.ColorComponents == ColorComponents.RedGreenBlue ? 3 : 4;
            internal int BitsPerPixel => ImageInfo.BitsPerChannel * NumberOfComponents;
            internal int ColorSpace => NumberOfComponents == 3 ? CELL_PNGDEC_RGB : CELL_PNGDEC_RGBA;

            internal PngDecoder(MemoryManager memoryManager)
            {
                _memoryManager = memoryManager;        
                Handle = _memoryManager.GetHandle();
            }

            internal void Open(CellPngDecSrc src)
            {
                _src = src;
                _srcData = GetCurrentSourceData(_src);
                _srcDataStream = new(_srcData);
                ImageInfo = CreateImageInfo();
            }

            internal Span<byte> DecodeImage(int totalBufferSize, int bytesPerLine)
            {
                Debug.Assert(_srcDataStream != null);
                _srcDataStream.Position = 0;

                int cellColorSpace = OutputParam.OutputColorSpace;
                ColorComponents colorComponenents = ToStbImageColorComponents((uint)cellColorSpace);
                ImageResult decodedImage = ImageResult.FromStream(_srcDataStream, colorComponenents);

                if (cellColorSpace == CELL_PNGDEC_ARGB)
                {
                    ConvertRGBAToARGB(decodedImage.Data);
                }

                // We need to copy row by row
                // 1: if the destination image memory has a larger row width, the extra width of each row remains untouched
                // 2: if the destination image memory has a smaller row width, each source row is trimmed (this case is bugged?)

                Span<byte> dest = new byte[totalBufferSize];
                int destRowBytes = bytesPerLine;

                Span<byte> src = decodedImage.Data;
                int pixelBytes = GetPixelByteSize((uint)cellColorSpace);
                int srcRowBytes = int.Min(decodedImage.Width * pixelBytes, destRowBytes);

                for (int y = 0; y < decodedImage.Height; y++)
                {
                    Span<byte> srcRow = src.Slice(y * srcRowBytes, srcRowBytes);
                    Span<byte> destRow = dest.Slice(y * destRowBytes, destRowBytes);
                    srcRow.CopyTo(destRow);
                }

                return dest;
            }

            private ImageInfo CreateImageInfo()
            {
                Debug.Assert(_srcDataStream != null);

                _srcDataStream.Position = 0;
                ImageInfo? info = ImageInfo.FromStream(_srcDataStream);

                Debug.Assert(info != null);
                return (ImageInfo)info;
            }

            private byte[] GetCurrentSourceData(CellPngDecSrc src)
            {
                if ((long)src.FileOffset > (uint)src.FileSize)
                {
                    throw new Exception($"Invalid offset/size");
                }

                byte[] data;

                if (src.SrcSelect == CELL_PNGDEC_FILE)
                {
                    string guestPath = _memoryManager.ReadString(src.Filename_ptr);
                    string hostPath = FileSystemManager.GetHostPath(guestPath);

                    FileStream fileStream = File.OpenRead(hostPath);
                    fileStream.Seek(0, SeekOrigin.Begin);

                    long file_size = src.FileSize > 0 ? src.FileSize : fileStream.Length; // not sure about this

                    long offset = src.FileOffset;
                    long length = (uint)file_size - offset;
                    data = new byte[file_size];
                    fileStream.ReadExactly(data, (int)offset, (int)length);

                    return data;
                }
                else
                {
                    uint source = src.StreamPtr;
                    uint size = src.StreamSize;

                    unsafe
                    {
                        // Return a copy of the data
                        data = new byte[size];
                        _memoryManager.ReadBufferFromVirtualMemory(source, data.AsSpan());
                        return data;
                    }
                }
            }

            private static int GetPixelByteSize(uint cellPngDecColor)
            {
                switch (cellPngDecColor)
                {
                    case CELL_PNGDEC_RGB:
                        return 3;

                    case CELL_PNGDEC_RGBA:
                    case CELL_PNGDEC_ARGB:
                        return 4;

                    default:
                        throw new Exception($"[CellPNGDec] todo color size: {cellPngDecColor:X}");
                }
            }

            private static ColorComponents ToStbImageColorComponents(uint cellPngDecColor)
            {
                switch (cellPngDecColor)
                {
                    case CELL_PNGDEC_RGB:
                        return ColorComponents.RedGreenBlue;

                    case CELL_PNGDEC_RGBA:
                    case CELL_PNGDEC_ARGB:
                        return ColorComponents.RedGreenBlueAlpha;

                    default:
                        throw new Exception($"[CellPNGDec] todo color size: {cellPngDecColor:X}");
                }
            }

            private static void ConvertRGBAToARGB(Span<byte> rgba)
            {
                // Swizzle RGBA to ARGB
                for (int i = 0; i < rgba.Length; i += 4)
                {
                    byte r = rgba[i];
                    byte g = rgba[i + 1];
                    byte b = rgba[i + 2];
                    byte a = rgba[i + 3];

                    rgba[i] = a;
                    rgba[i + 1] = r;
                    rgba[i + 2] = g;
                    rgba[i + 3] = b;
                }
            }

            internal void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                _srcDataStream?.Close();
                _srcDataStream = null;
                _srcData = null;
                _src = default;
                ImageInfo = default;
            }
        }
    }
}
