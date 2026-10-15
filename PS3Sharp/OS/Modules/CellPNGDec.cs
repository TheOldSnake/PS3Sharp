using OpenTK.Graphics.OpenGL;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using SkiaSharp;
using StbImageSharp;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Cell.RSX.FragmentShaderGenerator.FragmentInstruction;

namespace PS3Sharp.OS.Modules
{
    internal class CellPNGDec
    {
        internal struct CellPngDecThreadOutParam : IPS3Struct
        {
            internal BigEndian<uint> CodecVersion;
        }

        internal struct CellPngDecSrc : IPS3Struct
        {
            internal BigEndian<uint> SrcSelect;
            internal BigEndian<uint> Filename_ptr;
            internal BigEndian<long> FileOffset;
            internal BigEndian<uint> FileSize;
            internal BigEndian<uint> StreamPtr;
            internal BigEndian<uint> StreamSize;
            internal BigEndian<uint> spu_thread_enable; //?
        }

        internal struct CellPngDecInfo : IPS3Struct
        {
            internal BigEndian<uint> Width;
            internal BigEndian<uint> Height;
            internal BigEndian<uint> NumberOfComponents;
            internal BigEndian<int> ColorSpace;
            internal BigEndian<uint> BitDepth;
            internal BigEndian<int> InterlaceMethod;
            internal BigEndian<uint> ChunkInfo;
        }

        internal struct CellPngDecInParam : IPS3Struct
        {
            internal BigEndian<uint> CommandPtr;
            internal BigEndian<uint> OutputMode;
            internal BigEndian<int> OutputColorSpace;
            internal BigEndian<uint> OutputBitDepth;
            internal BigEndian<uint> OutputPackFlag;
            internal BigEndian<uint> OutputAlphaSel;
            internal BigEndian<uint> OutputColorAlpha;
        }

        internal struct CellPngDecOutParam : IPS3Struct
        {
            internal BigEndian<ulong> OutputWidthByte;
            internal BigEndian<uint> OutputWidth;
            internal BigEndian<uint> OutputHeight;
            internal BigEndian<uint> OutputComponents;
            internal BigEndian<uint> OutputBitDepth;
            internal BigEndian<int> OutputMode;
            internal BigEndian<int> OutputColorSpace;
            internal BigEndian<uint> UseMemorySpace;
        }

        internal struct CellPngDecDataOutInfo : IPS3Struct
        {
            internal BigEndian<uint> ChunkInformation;
            internal BigEndian<uint> NumText;
            internal BigEndian<uint> NumUnknownChunk;
            internal BigEndian<int> Status;
        }

        internal struct CellPngDecDataCtrlParam : IPS3Struct
        {
            internal BigEndian<ulong> OutputBytesPerLine;
        }

        internal const uint PNG_CODEC_VERSION = 0x00420000;
        internal const uint CELL_PNGDEC_FILE = 0;
        internal const uint CELL_PNGDEC_BUFFER = 1;
        internal const int CELL_PNGDEC_GRAYSCALE = 1;
        internal const int CELL_PNGDEC_RGB = 2;
        internal const int CELL_PNGDEC_PALETTE = 4;
        internal const int CELL_PNGDEC_GRAYSCALE_ALPHA = 9;
        internal const int CELL_PNGDEC_RGBA = 10;
        internal const int CELL_PNGDEC_ARGB = 20;

        private readonly MemoryManager _memoryManager;
        private uint _decoderCurrentHandle;
        private CellPngDecSrc _currentSource;
        private CellPngDecOutParam _currentOutParam;
        //private Image? _currentImage;

        private byte[]? _currentPNGData;

        internal CellPNGDec(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellPngDecCreate(PPUThread ppu)
        {
            GuestPtr<uint> handle_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            uint thread_in_param_ptr = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellPngDecThreadOutParam> thread_out_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecThreadOutParam>>(ARG2);

            //Write the handle
            uint handle = _memoryManager.GetHandle();
            _decoderCurrentHandle = handle;
            _memoryManager.Write<uint>(handle_ptr, _decoderCurrentHandle);

            //Write the codec version
            unsafe
            {
                CellPngDecThreadOutParam* out_param = _memoryManager.GetPtr(thread_out_param_ptr);
                out_param->CodecVersion = PNG_CODEC_VERSION;
            }

            //Console.WriteLine($"[CellPNGDec] cellPngDecCreate");
            return CELL_OK;
        }

        internal ulong CellPngDecOpen(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> sub_handle_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            GuestPtr<CellPngDecSrc> src_ptr = ppu.ReadArg<GuestPtr<CellPngDecSrc>>(ARG2);
            GuestVoidPtr open_info_ptr = ppu.ReadArg<GuestVoidPtr>(ARG3);

            if (handle != _decoderCurrentHandle)
            {
                throw new Exception($"[CellPNGDec] Got a different handle: 0x{handle:X8}");
            }

            _currentSource = _memoryManager.ReadStruct<CellPngDecSrc>(src_ptr);
            _currentPNGData = GetCurrentSourceData();

            string source = _currentSource.SrcSelect == CELL_PNGDEC_FILE ? _memoryManager.ReadString(_currentSource.Filename_ptr) : "memory buffer";
            //Console.WriteLine($"[CellPNGDec] cellPngDecOpen src = {source}");
            return CELL_OK;
        }

        internal ulong CellPngDecReadHeader(PPUThread ppu)
        {
            Debug.Assert(_currentPNGData != null);

            uint handle = ppu.ReadArg<uint>(ARG0);
            uint sub_handle = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellPngDecInfo> info_ptr = ppu.ReadArg<GuestPtr<CellPngDecInfo>>(ARG2);

            //Fill the CellPngDecInfo data from the ImageSharp.Image instance
            unsafe
            {
                ImageInfo srcImageInfo = GetImageInfo(_currentPNGData);
                CellPngDecInfo* info = _memoryManager.GetPtr(info_ptr);

                info->Width = (uint)srcImageInfo.Width;
                info->Height = (uint)srcImageInfo.Height;
                info->NumberOfComponents = (uint)GetNumberOfComponents(srcImageInfo);
                info->ColorSpace = GetColorSpace(srcImageInfo);
                info->BitDepth = (uint)GetBitsPerPixel(srcImageInfo);
                info->InterlaceMethod = 0;
                info->ChunkInfo = 0;
            }

            //Console.WriteLine($"[CellPNGDec] cellPngDecReadHeader");
            return CELL_OK;
        }

        internal ulong CellPngDecSetParameter(PPUThread ppu)
        {
            Debug.Assert(_currentPNGData != null);
            ImageInfo srcImageInfo = GetImageInfo(_currentPNGData);

            uint handle = ppu.ReadArg<uint>(ARG0);
            uint sub_handle = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellPngDecInParam> in_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecInParam>>(ARG2);
            GuestPtr<CellPngDecOutParam> out_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecOutParam>>(ARG3);

            //Fill the CellPngDecOutParam data from the ImageSharp.Image instance
            unsafe
            {
                CellPngDecInParam* in_param = _memoryManager.GetPtr(in_param_ptr);
                CellPngDecOutParam* out_param = _memoryManager.GetPtr(out_param_ptr);

                ulong outputWidthInBits = (ulong)(srcImageInfo.Width * GetBitsPerPixel(srcImageInfo));
                out_param->OutputWidthByte = outputWidthInBits / 8;
                out_param->OutputWidth = (uint)srcImageInfo.Width;
                out_param->OutputHeight = (uint)srcImageInfo.Height;
                out_param->OutputComponents = (uint)(GetNumberOfComponents(srcImageInfo));
                out_param->OutputBitDepth = (uint)(GetBitsPerPixel(srcImageInfo));
                out_param->OutputMode = (int)(uint)in_param->OutputMode;      //same as in_param
                out_param->OutputColorSpace = in_param->OutputColorSpace;     //same as in_param
                out_param->UseMemorySpace = 0;                                //?

                //Save a copy of this struct -- we need it when we decode in cellPngDecDecodeData
                _currentOutParam = *out_param;
            }

            return CELL_OK;
        }

        internal ulong CellPngDecDecodeData(PPUThread ppu)
        {
            Debug.Assert(_currentPNGData != null);

            uint handle = ppu.ReadArg<uint>(ARG0);
            uint sub_handle = ppu.ReadArg<uint>(ARG1);
            GuestVoidPtr data_buffer_ptr = ppu.ReadArg<GuestVoidPtr>(ARG2);
            GuestPtr<CellPngDecDataCtrlParam> data_ctrl_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecDataCtrlParam>>(ARG3);
            GuestPtr<CellPngDecDataOutInfo> data_out_info_ptr = ppu.ReadArg<GuestPtr<CellPngDecDataOutInfo>>(ARG4);

            CellPngDecDataCtrlParam data_ctrl_param = _memoryManager.ReadStruct<CellPngDecDataCtrlParam>(data_ctrl_param_ptr);

            int outputColorSpace = _currentOutParam.OutputColorSpace;
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

            // Decode the data
            Span<byte> buffer = DecodeImage(lengthInBytes, (int)outputBytesPerLine, (uint)outputColorSpace);

            // Copy it to virtual memory, this copy handles crossing block boundaries
            _memoryManager.CopyBufferToVirtualMemory(buffer, data_buffer_ptr);

            // Set staus to finish
            unsafe
            {
                CellPngDecDataOutInfo* data_out_info = _memoryManager.GetPtr(data_out_info_ptr);
                data_out_info->Status = 0;
            }

            //Console.WriteLine($"[CellPNGDec] cellPngDecDecodeData");
            return CELL_OK;
        }

        internal Span<byte> DecodeImage(int totalBufferSize, int bytesPerLine, uint outColorSpace)
        {
            Debug.Assert(_currentPNGData != null);

            ColorComponents colorComponenents = ToStbImageColorComponents(outColorSpace);
            using MemoryStream stream = new(_currentPNGData);
            ImageResult decodedImage = ImageResult.FromStream(stream, colorComponenents);

            if (outColorSpace == CELL_PNGDEC_ARGB)
            {
                ConvertRGBAToARGB(decodedImage.Data);
            }
            
            // We need to copy row by row
            // 1: if the destination image memory has a larger row width, the extra width of each row remains untouched
            // 2: if the destination image memory has a smaller row width, each source row is trimmed (this case is bugged?)

            Span<byte> dest = new byte[totalBufferSize];
            int destRowBytes = bytesPerLine;

            Span<byte> src = decodedImage.Data;
            int pixelBytes = GetPixelByteSize(outColorSpace);
            int srcRowBytes = int.Min(decodedImage.Width * pixelBytes, destRowBytes);

            for (int y = 0; y < decodedImage.Height; y++)
            {
                Span<byte> srcRow = src.Slice(y * srcRowBytes, srcRowBytes);
                Span<byte> destRow = dest.Slice(y * destRowBytes, destRowBytes);
                srcRow.CopyTo(destRow);
            }

            return dest;
        }

        internal ulong CellPngDecClose(PPUThread ppu)
        {
            // Reset
            _currentOutParam = default;
            _currentSource = default;
            _currentPNGData = null;

            //Console.WriteLine($"[CellPNGDec] cellPngDecClose");

            return CELL_OK;
        }

        internal ulong CellPngDecDestroy(PPUThread ppu)
        {
            //Reset
            _currentOutParam = default;
            _currentSource = default;
            _currentPNGData = null;

            return CELL_OK;
        }

        private byte[] GetCurrentSourceData()
        {
            if ((long)_currentSource.FileOffset > (uint)_currentSource.FileSize)
            {
                throw new Exception($"Invalid offset/size");
            }

            byte[] data;

            if (_currentSource.SrcSelect == CELL_PNGDEC_FILE)
            {
                string guestPath = _memoryManager.ReadString(_currentSource.Filename_ptr);
                string hostPath = FileSystemManager.GetHostPath(guestPath);

                FileStream fileStream = File.OpenRead(hostPath);
                fileStream.Seek(0, SeekOrigin.Begin);

                long file_size = _currentSource.FileSize > 0 ? _currentSource.FileSize : fileStream.Length; //not sure about this

                long offset = _currentSource.FileOffset;
                long length = (uint)file_size - offset;
                data = new byte[file_size];

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
                    data = new byte[size];
                   _memoryManager.ReadBufferFromVirtualMemory(source, data.AsSpan());
                    return data;
                }
            }
        }

        private static ImageInfo GetImageInfo(byte[] src)
        {
            using MemoryStream stream = new(src);
            ImageInfo? info = ImageInfo.FromStream(stream);

            if (info == null)
            {
                throw new Exception($"[CellPNGDec] Got null ImageInfo!");
            }

            return (ImageInfo)info;
        }
        
        private static int GetNumberOfComponents(ImageInfo srcImageInfo)
        {
            return srcImageInfo.ColorComponents == ColorComponents.RedGreenBlue ? 3 : 4;
        }

        private static int GetBitsPerPixel(ImageInfo srcImageInfo)
        {
            return (srcImageInfo.BitsPerChannel * GetNumberOfComponents(srcImageInfo));
        }

        private static int GetColorSpace(ImageInfo srcImageInfo)
        {
            return GetNumberOfComponents(srcImageInfo) == 3 ? CELL_PNGDEC_RGB : CELL_PNGDEC_RGBA;
        }

        private static int GetPixelByteSize(uint cellPngDecColor)
        {
            switch (cellPngDecColor)
            {
                case CELL_PNGDEC_RGB: return 3;
                case CELL_PNGDEC_RGBA: return 4;
                case CELL_PNGDEC_ARGB: return 4;
                default:
                    throw new Exception($"[CellPNGDec] todo color size: {cellPngDecColor:X}");
            }
        }

        private static ColorComponents ToStbImageColorComponents(uint cellPngDecColor)
        {
            switch (cellPngDecColor)
            {
                case CELL_PNGDEC_RGB: return ColorComponents.RedGreenBlue;

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
    }
}
