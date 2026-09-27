using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class CellPNGDec
    { //We can probably LLE this

        public const uint PNG_CODEC_VERSION = 0x00420000;
        public const uint CELL_PNGDEC_FILE = 0;
        public const uint CELL_PNGDEC_BUFFER = 1;

        public const int CELL_PNGDEC_GRAYSCALE = 1;
        public const int CELL_PNGDEC_RGB = 2;
        public const int CELL_PNGDEC_PALETTE = 4;
        public const int CELL_PNGDEC_GRAYSCALE_ALPHA = 9;
        public const int CELL_PNGDEC_RGBA = 10;
        public const int CELL_PNGDEC_ARGB = 20;

        public struct CellPngDecThreadOutParam : IPS3Struct
        {
            public BigEndian<uint> CodecVersion;
        }

        public struct CellPngDecSrc : IPS3Struct
        {
            public BigEndian<uint> SrcSelect;
            public BigEndian<uint> Filename_ptr;
            public BigEndian<long> FileOffset;
            public BigEndian<uint> FileSize;
            public BigEndian<uint> StreamPtr;
            public BigEndian<uint> StreamSize;
            public BigEndian<uint> spu_thread_enable; //?
        }

        public struct CellPngDecInfo : IPS3Struct
        {
            public BigEndian<uint> Width;
            public BigEndian<uint> Height;
            public BigEndian<uint> NumberOfComponents;
            public BigEndian<int> ColorSpace;
            public BigEndian<uint> BitDepth;
            public BigEndian<int> InterlaceMethod;
            public BigEndian<uint> ChunkInfo;
        }

        public struct CellPngDecInParam : IPS3Struct
        {
            public BigEndian<uint> CommandPtr;
            public BigEndian<uint> OutputMode;
            public BigEndian<int> OutputColorSpace;
            public BigEndian<uint> OutputBitDepth;
            public BigEndian<uint> OutputPackFlag;
            public BigEndian<uint> OutputAlphaSel;
            public BigEndian<uint> OutputColorAlpha;
        }

        public struct CellPngDecOutParam : IPS3Struct
        {
            public BigEndian<ulong> OutputWidthByte;
            public BigEndian<uint> OutputWidth;
            public BigEndian<uint> OutputHeight;
            public BigEndian<uint> OutputComponents;
            public BigEndian<uint> OutputBitDepth;
            public BigEndian<int> OutputMode;
            public BigEndian<int> OutputColorSpace;
            public BigEndian<uint> UseMemorySpace;
        }

        public struct CellPngDecDataOutInfo : IPS3Struct
        {
            public BigEndian<uint> ChunkInformation;
            public BigEndian<uint> NumText;
            public BigEndian<uint> NumUnknownChunk;
            public BigEndian<int> Status;
        }

        public struct CellPngDecDataCtrlParam : IPS3Struct
        {
            public BigEndian<ulong> OutputBytesPerLine;
        }

        private static uint _decoderCurrentHandle;
        private static CellPngDecSrc _currentSource;
        private static CellPngDecOutParam _currentOutParam;
        private static Image? _currentImage;

        private static Image GetImage(ReadOnlySpan<byte> pngData) => Image.Load(pngData);

        public static void EnsureImageIsSet(string function)
        {
            if (_currentImage == null)
            {
                throw new NullReferenceException($"[CellPNGDec] {function}: _currentImage is null");
            }
        }

        public static void SwizzleRGBAtoARGB(ref Image<Rgba32> rgba_img)
        {
            rgba_img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);

                    for (int x = 0; x < row.Length; x++)
                    {
                        ref Rgba32 pixel = ref row[x];
                        pixel = new Rgba32(pixel.A, pixel.R, pixel.G, pixel.B);
                    }
                }
            });
        }

        private static ReadOnlySpan<byte> GetCurrentSourceData(PPU ppu)
        {
            if ((long)_currentSource.FileOffset > (uint)_currentSource.FileSize)
            {
                throw new Exception($"Invalid offset/size");
            }

            if (_currentSource.SrcSelect == CELL_PNGDEC_FILE)
            {
                string guestPath = MemoryManager.ReadString(_currentSource.Filename_ptr);
                string hostPath = SysFs.GetAbsoluteHostPath(guestPath);

                FileStream fileStream = File.OpenRead(hostPath);
                fileStream.Seek(0, SeekOrigin.Begin);

                long file_size = _currentSource.FileSize > 0 ? _currentSource.FileSize : fileStream.Length; //not sure about this

                long offset = _currentSource.FileOffset;
                long length = (uint)file_size - offset;
                byte[] data = new byte[file_size];

                fileStream.ReadExactly(data, (int)offset, (int)length);
                //Console.WriteLine($"CellPNGDec: Read: {hostPath} - _currentSource.FileSize = {_currentSource.FileSize} offset = {offset} size = {data.Length}");
                return data;

            }
            else
            {
                uint source = _currentSource.StreamPtr;
                uint size = _currentSource.StreamSize;

                unsafe
                {
                    // Return a copy of the data
                    return MemoryManager.CreateSpan<byte>(source, (int)size).ToArray();
                }
            }
        }

        public static ulong CellPngDecCreate(PPU ppu)
        {
            GuestPtr<uint> handle_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            uint thread_in_param_ptr = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellPngDecThreadOutParam> thread_out_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecThreadOutParam>>(ARG2);

            //Write the handle
            uint handle = MemoryManager.GetHandle();
            _decoderCurrentHandle = handle;
            MemoryManager.Write<uint>(handle_ptr, _decoderCurrentHandle);

            //Write the codec version
            unsafe
            {
                CellPngDecThreadOutParam* out_param = thread_out_param_ptr.AsNativePtr();
                out_param->CodecVersion = PNG_CODEC_VERSION;
            }

            //Console.WriteLine($"[CellPNGDec] cellPngDecCreate");
            return CELL_OK;
        }

        public static ulong CellPngDecOpen(PPU ppu)
        {
            uint handle = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> sub_handle_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            GuestPtr<CellPngDecSrc> src_ptr = ppu.ReadArg<GuestPtr<CellPngDecSrc>>(ARG2);
            GuestVoidPtr open_info_ptr = ppu.ReadArg<GuestVoidPtr>(ARG3);

            if (handle != _decoderCurrentHandle)
            {
                throw new Exception($"[CellPNGDec] Got a different handle: 0x{handle:X8}");
            }

            _currentSource = MemoryManager.ReadStruct<CellPngDecSrc>(src_ptr);

            ReadOnlySpan<byte> pngData = GetCurrentSourceData(ppu);
            _currentImage = GetImage(pngData);

            string source = _currentSource.SrcSelect == CELL_PNGDEC_FILE ? MemoryManager.ReadString(_currentSource.Filename_ptr) : "memory buffer";
            //Console.WriteLine($"[CellPNGDec] cellPngDecOpen src = {source}");
            return CELL_OK;
        }

        public static ulong CellPngDecReadHeader(PPU ppu)
        {
            EnsureImageIsSet("cellPngDecReadHeader");

            uint handle = ppu.ReadArg<uint>(ARG0);
            uint sub_handle = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellPngDecInfo> info_ptr = ppu.ReadArg<GuestPtr<CellPngDecInfo>>(ARG2);

            //Fill the CellPngDecInfo data from the ImageSharp.Image instance
            unsafe
            {
                CellPngDecInfo* info = info_ptr.AsNativePtr();
                info->Width = (uint)_currentImage.Width;
                info->Height = (uint)_currentImage.Height;
                info->NumberOfComponents = (uint)_currentImage.PixelType.BitsPerPixel / 8;
                info->ColorSpace = info->NumberOfComponents == 3 ? CELL_PNGDEC_RGB : CELL_PNGDEC_RGBA;
                info->BitDepth = (uint)_currentImage.PixelType.BitsPerPixel;
                info->InterlaceMethod = 0;
                info->ChunkInfo = 0;
            }

            //Console.WriteLine($"[CellPNGDec] cellPngDecReadHeader");
            return CELL_OK;
        }

        public static ulong CellPngDecSetParameter(PPU ppu)
        {
            EnsureImageIsSet("cellPngDecReadHeader");

            uint handle = ppu.ReadArg<uint>(ARG0);
            uint sub_handle = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellPngDecInParam> in_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecInParam>>(ARG2);
            GuestPtr<CellPngDecOutParam> out_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecOutParam>>(ARG3);

            //Fill the CellPngDecOutParam data from the ImageSharp.Image instance
            unsafe
            {
                CellPngDecInParam* in_param = in_param_ptr.AsNativePtr();
                CellPngDecOutParam* out_param = out_param_ptr.AsNativePtr();

                ulong outputWidthInBits = (ulong)(_currentImage.Width * _currentImage.PixelType.BitsPerPixel);
                out_param->OutputWidthByte = outputWidthInBits / 8;
                out_param->OutputWidth = (uint)_currentImage.Width;
                out_param->OutputHeight = (uint)_currentImage.Height;
                out_param->OutputComponents = (uint)(_currentImage.PixelType.BitsPerPixel / 8);
                out_param->OutputBitDepth = (uint)(_currentImage.PixelType.BitsPerPixel);
                out_param->OutputMode = (int)(uint)in_param->OutputMode;      //same as in_param
                out_param->OutputColorSpace = in_param->OutputColorSpace;     //same as in_param
                out_param->UseMemorySpace = 0;                                //?

                //Save a copy of this struct -- we need it when we decode in cellPngDecDecodeData
                _currentOutParam = *out_param;
            }

            return CELL_OK;
        }

        public static ulong CellPngDecDecodeData(PPU ppu)
        {
            EnsureImageIsSet("cellPngDecDecodeData");

            uint handle = ppu.ReadArg<uint>(ARG0);
            uint sub_handle = ppu.ReadArg<uint>(ARG1);
            GuestVoidPtr data_buffer_ptr = ppu.ReadArg<GuestVoidPtr>(ARG2);
            GuestPtr<CellPngDecDataCtrlParam> data_ctrl_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecDataCtrlParam>>(ARG3);
            GuestPtr<CellPngDecDataOutInfo> data_out_info_ptr = ppu.ReadArg<GuestPtr<CellPngDecDataOutInfo>>(ARG4);

            CellPngDecDataCtrlParam data_ctrl_param = MemoryManager.ReadStruct<CellPngDecDataCtrlParam>(data_ctrl_param_ptr);

            bool isRgb = _currentOutParam.OutputColorSpace == CELL_PNGDEC_RGB;
            ulong outputWidthByte = _currentOutParam.OutputWidthByte;
            uint outputHeight = _currentOutParam.OutputHeight;
            int lengthInBytes = (int)(outputHeight * data_ctrl_param.OutputBytesPerLine); //what if the image has a different width?
            ulong outputBytesPerLine = data_ctrl_param.OutputBytesPerLine;

            // Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData: outputWidthByte = {outputWidthByte}");
            // Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData: outputHeight = {outputHeight}");
            // Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData: lengthInBytes = {lengthInBytes}");
            // Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData: img_w = {_currentImage.Width}");
            // Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData: img_h = {_currentImage.Height}");
            // Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData: data_ctrl_param.OutputBytesPerLine = {data_ctrl_param.OutputBytesPerLine}");

            using (MemoryManager.AcquireTemporaryLock())
            {
                // Create a temporary buffer
                Span<byte> buffer = new byte[lengthInBytes];

                // Decode the data
                switch (_currentOutParam.OutputColorSpace)
                {
                    case CELL_PNGDEC_RGB:
                        DecodeImage<Rgb24>((int)outputBytesPerLine, buffer);
                        break;

                    case CELL_PNGDEC_RGBA:
                        DecodeImage<Rgba32>((int)outputBytesPerLine, buffer);
                        break;

                    case CELL_PNGDEC_ARGB:
                        DecodeImage<Argb32>((int)outputBytesPerLine, buffer);
                        break;

                    default:
                        throw new Exception($"[CellPNGDec] unhandled OutputColorSpace = 0x{_currentOutParam.OutputColorSpace:X}");
                }

                // Copy it to virtual memory, this copy handles crossing block boundaries
                MemoryManager.CopyBufferToVirtualMemory(buffer, data_buffer_ptr);

                // Set staus to finish
                unsafe
                {
                    CellPngDecDataOutInfo* data_out_info = data_out_info_ptr.AsNativePtr();
                    data_out_info->Status = 0;
                }
            }

            //Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData");
            return CELL_OK;
        }

        public static void DecodeImage<T>(int bytesPerLine, Span<byte> destination) where T : unmanaged, IPixel<T>
        {
            destination.Clear();
            Span<T> destinationPixels = MemoryMarshal.Cast<byte, T>(destination);

            Image<T> srcImg = _currentImage.CloneAs<T>();

            int numberOfComponents = Unsafe.SizeOf<T>(); // this works because each component is a byte in Rgb24/Rgba32/Argb32
            int destinationWidth = bytesPerLine / numberOfComponents;

            // We need to copy row by row, don't use CopyPixelDataTo(), because it copies all pixels sequentially.
            // 1: if the destination image memory has a larger row width, the extra width of each row remains untouced
            // 2: if the destination image memory has a smaller row width, each source row is trimmed (this case is bugged)

            for (int y = 0; y < srcImg.Height; y++)
            {
                Span<T> srcRow = srcImg.DangerousGetPixelRowMemory(y).Span;
                Span<T> destRow = destinationPixels.Slice(y * destinationWidth, destinationWidth);
                destRow.Clear();

                // Console.WriteLine($"Y = {y} srcRow length = {srcRow.Length} destinationWidth = {destinationWidth} destRow length = {destRow.Length}");

                if (srcRow.Length > destRow.Length)
                {
                    srcRow = srcRow.Slice(0, destRow.Length);
                }

                srcRow.CopyTo(destRow);
            }

            //DumpDecodedImage(srcImg);
        }

        public static ulong CellPngDecClose(PPU ppu)
        {
            // Reset
            _currentOutParam = default;
            _currentImage = null;

            //Console.WriteLine($"[CellPNGDec] cellPngDecClose");

            return CELL_OK;
        }

        public static ulong CellPngDecDestroy(PPU ppu)
        {
            //Reset
            _currentOutParam = default;
            _currentImage = null;

            //Console.WriteLine($"[CellPNGDec] cellPngDecDestroy lr = {ppu.LR:X8}");

            return CELL_OK;
        }

        static int dumps = 0;
        public static void DumpDecodedImage(Image image)
        {
            image.Save($@"pngdumps\{dumps}.png");
            dumps++;
        }
    }
}
