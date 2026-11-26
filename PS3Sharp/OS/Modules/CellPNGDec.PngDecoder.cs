using PS3Sharp.FileSystem;
using PS3Sharp.GuestMemory;
using StbImageSharp;
using System.Diagnostics;

namespace PS3Sharp.OS.Modules
{
    internal sealed partial class CellPngDec
    {
        private sealed class PngDecoder : IDisposable
        {
            private readonly MemoryManager _memoryManager;
            private CellPngDecSrc _src;
            private Stream? _srcDataStream;
            
            internal uint Handle { get; }
            internal CellPngDecOutParam OutputParam { get; set; }
            internal ImageInfo ImageInfo { get; private set; }
            internal int NumberOfComponents => ImageInfo.ColorComponents == ColorComponents.RedGreenBlue ? 3 : 4;
            internal int BitsPerPixel => ImageInfo.BitsPerChannel * NumberOfComponents;
            internal int ColorSpace => NumberOfComponents == 3 ? CellPngdecRgb : CellPngdecRgba;

            internal PngDecoder(MemoryManager memoryManager)
            {
                _memoryManager = memoryManager;        
                Handle = _memoryManager.GetHandle();
            }

            internal void Open(CellPngDecSrc src)
            {
                _src = src;
                _srcDataStream = GetDataStream(_src);
                ImageInfo? info = CreateImageInfo();
                Debug.Assert(info != null, $"[PngDecoder] Failed to get image info");
                ImageInfo = (ImageInfo)info;
            }

            internal byte[] DecodeImage(int totalBufferSize, int bytesPerLine)
            {
                Debug.Assert(_srcDataStream != null);
                _srcDataStream.Position = 0;

                int cellColorSpace = OutputParam.OutputColorSpace;
                int pixelBytes = GetPixelByteSize((uint)cellColorSpace);

                ColorComponents colorComponenents = ToStbImageColorComponents((uint)cellColorSpace);
                ImageResult decodedImage = ImageResult.FromStream(_srcDataStream, colorComponenents);

                if (cellColorSpace == CellPngdecArgb)
                {
                    ConvertRGBAToARGB(decodedImage.Data);
                }

                return CreateImageBuffer(decodedImage, totalBufferSize, bytesPerLine, pixelBytes);
            }

            private ImageInfo? CreateImageInfo()
            {
                Debug.Assert(_srcDataStream != null);
                _srcDataStream.Position = 0;
                return ImageInfo.FromStream(_srcDataStream);
            }

            private Stream GetDataStream(CellPngDecSrc src)
            {
                if ((long)src.FileOffset > (uint)src.FileSize)
                {
                    throw new Exception($"Invalid offset/size");
                }

                Stream dataStream;

                if (src.SrcSelect == CellPngdecFile)
                {
                    string guestPath = _memoryManager.ReadString(src.Filename_ptr);
                    string hostPath = FileSystemManager.GetHostPath(guestPath);
                    dataStream = File.OpenRead(hostPath);
                }
                else
                {
                    // Copy the data from memory
                    uint source = src.StreamPtr;
                    uint size = src.StreamSize;
                    byte[] buffer = new byte[size];
                    _memoryManager.ReadBufferFromVirtualMemory(source, buffer.AsSpan());
                    dataStream = new MemoryStream(buffer);
                }

                return dataStream;
            }

            private static byte[] CreateImageBuffer(ImageResult decodedImage, int destBufferSizeBytes, int destPitchBytes, int pixelSizeBytes)
            {
                // We need to copy row by row
                // 1: if the destination image memory has a larger row width, the extra width of each row remains untouched
                // 2: if the destination image memory has a smaller row width, each source row is trimmed (this case is bugged?)

                byte[] buffer = new byte[destBufferSizeBytes];
                Span<byte> dest = buffer.AsSpan();

                Span<byte> src = decodedImage.Data;
                int srcPixelWidth = decodedImage.Width;
                int srcPixelHeight = decodedImage.Height;
                int srcRowBytes = int.Min(srcPixelWidth * pixelSizeBytes, destPitchBytes);

                for (int y = 0; y < srcPixelHeight; y++)
                {
                    Span<byte> srcRow = src.Slice(y * srcRowBytes, srcRowBytes);
                    Span<byte> destRow = dest.Slice(y * destPitchBytes, destPitchBytes);
                    srcRow.CopyTo(destRow);
                }

                return buffer;
            }

            private static int GetPixelByteSize(uint cellPngDecColor)
            {
                switch (cellPngDecColor)
                {
                    case CellPngdecRgb:
                        return 3;

                    case CellPngdecRgba:
                    case CellPngdecArgb:
                        return 4;

                    default:
                        throw new Exception($"[CellPNGDec] todo color size: {cellPngDecColor:X}");
                }
            }

            private static ColorComponents ToStbImageColorComponents(uint cellPngDecColor)
            {
                switch (cellPngDecColor)
                {
                    case CellPngdecRgb:
                        return ColorComponents.RedGreenBlue;

                    case CellPngdecRgba:
                    case CellPngdecArgb:
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
                _src = default;
                ImageInfo = default;
            }
        }
    }
}