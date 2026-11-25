using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using StbImageSharp;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed partial class CellPngDec : IDisposable
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

        internal const uint PngCodecVersion = 0x00420000;
        internal const uint CellPngdecFile = 0;
        internal const uint CellPngdecBuffer = 1;
        internal const int CellPngdecGrayscale = 1;
        internal const int CellPngdecRgb = 2;
        internal const int CellPngdecPalette = 4;
        internal const int CellPngdecGrayscaleAlpha = 9;
        internal const int CellPngdecRgba = 10;
        internal const int CellPngdecArgb = 20;

        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, PngDecoder> _decoders = [];

        internal CellPngDec(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellPngDecCreate(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> handle_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            uint thread_in_param_ptr = ppu.ReadArg<uint>(Arg1);
            GuestPtr<CellPngDecThreadOutParam> thread_out_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecThreadOutParam>>(Arg2);

            // Create a decoder
            PngDecoder decoder = new(_memoryManager);

            // Add it to the dictionary 
            AddPngDecoder(decoder);

            // Write the handle
            _memoryManager.Write<BigEndian<uint>>(handle_ptr, decoder.Handle);

            //Write the codec version
            CellPngDecThreadOutParam out_param = new()
            {
                CodecVersion = PngCodecVersion,
            };

            _memoryManager.Write(thread_out_param_ptr, out_param);
            return CellOk;
        }

        internal ulong CellPngDecOpen(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<uint>> sub_handle_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);
            GuestPtr<CellPngDecSrc> src_ptr = ppu.ReadArg<GuestPtr<CellPngDecSrc>>(Arg2);
            GuestVoidPtr open_info_ptr = ppu.ReadArg<GuestVoidPtr>(Arg3);

            if (TryGetPngDec(handle, out PngDecoder? decoder))
            {
                CellPngDecSrc src = _memoryManager.Read<CellPngDecSrc>(src_ptr);
                decoder.Open(src);
            }
            else
            {
                // Todo: Return actual error code
                throw new Exception($"[CellPngDec] Can't find decoder handle: {handle:X}");
            }

            return CellOk;
        }

        internal ulong CellPngDecReadHeader(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);
            uint sub_handle = ppu.ReadArg<uint>(Arg1);
            GuestPtr<CellPngDecInfo> info_ptr = ppu.ReadArg<GuestPtr<CellPngDecInfo>>(Arg2);

            if (TryGetPngDec(handle, out PngDecoder? decoder))
            {
                ImageInfo srcImageInfo = decoder.ImageInfo;
                CellPngDecInfo info = new()
                {
                    Width = (uint)srcImageInfo.Width,
                    Height = (uint)srcImageInfo.Height,
                    NumberOfComponents = (uint)decoder.NumberOfComponents,
                    ColorSpace = decoder.ColorSpace,
                    BitDepth = (uint)decoder.BitsPerPixel,
                    InterlaceMethod = 0,
                    ChunkInfo = 0
                };

                _memoryManager.Write(info_ptr, info);
            }
            else
            {
                // Todo: Return actual error code
                throw new Exception($"[CellPngDec] Can't find decoder handle: {handle:X}");
            }

            return CellOk;
        }

        internal ulong CellPngDecSetParameter(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);
            uint sub_handle = ppu.ReadArg<uint>(Arg1);
            GuestPtr<CellPngDecInParam> in_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecInParam>>(Arg2);
            GuestPtr<CellPngDecOutParam> out_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecOutParam>>(Arg3);

            if (TryGetPngDec(handle, out PngDecoder? decoder))
            {
                ImageInfo srcImageInfo = decoder.ImageInfo;
                ulong outputWidthInBits = (ulong)(srcImageInfo.Width * decoder.BitsPerPixel);

                CellPngDecInParam in_param = _memoryManager.Read<CellPngDecInParam>(in_param_ptr);
                CellPngDecOutParam out_param = new()
                {
                    OutputWidthByte = outputWidthInBits / 8,
                    OutputWidth = (uint)srcImageInfo.Width,
                    OutputHeight = (uint)srcImageInfo.Height,
                    OutputComponents = (uint)decoder.NumberOfComponents,
                    OutputBitDepth = (uint)decoder.BitsPerPixel,
                    OutputMode = (int)(uint)in_param.OutputMode,      //same as in_param
                    OutputColorSpace = in_param.OutputColorSpace,     //same as in_param
                    UseMemorySpace = 0,                               //?
                };

                _memoryManager.Write(out_param_ptr, out_param);

                // Save a copy of this struct -- we need it when we decode in cellPngDecDecodeData
                decoder.OutputParam = out_param;
            }
            else
            {
                // Todo: Return actual error code
                throw new Exception($"[CellPngDec] Can't find decoder handle: {handle:X}");
            }

            return CellOk;
        }

        internal ulong CellPngDecDecodeData(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);
            uint sub_handle = ppu.ReadArg<uint>(Arg1);
            GuestVoidPtr data_buffer_ptr = ppu.ReadArg<GuestVoidPtr>(Arg2);
            GuestPtr<CellPngDecDataCtrlParam> data_ctrl_param_ptr = ppu.ReadArg<GuestPtr<CellPngDecDataCtrlParam>>(Arg3);
            GuestPtr<CellPngDecDataOutInfo> data_out_info_ptr = ppu.ReadArg<GuestPtr<CellPngDecDataOutInfo>>(Arg4);

            CellPngDecDataCtrlParam data_ctrl_param = _memoryManager.Read<CellPngDecDataCtrlParam>(data_ctrl_param_ptr);

            if (TryGetPngDec(handle, out PngDecoder? decoder))
            {

                uint outputHeight = decoder.OutputParam.OutputHeight;
                int lengthInBytes = (int)(outputHeight * data_ctrl_param.OutputBytesPerLine);
                ulong outputBytesPerLine = data_ctrl_param.OutputBytesPerLine;

                // Decode the data
                Span<byte> buffer = decoder.DecodeImage(lengthInBytes, (int)outputBytesPerLine);

                // Copy it to virtual memory, this copy handles crossing block boundaries
                _memoryManager.CopyBufferToVirtualMemory(buffer, data_buffer_ptr);

                // Set staus to finish
                CellPngDecDataOutInfo data_out_info = _memoryManager.Read<CellPngDecDataOutInfo>(data_out_info_ptr);
                data_out_info.Status = 0;
                _memoryManager.Write(data_out_info_ptr, data_out_info);
            }
            else
            {
                // Todo: Return actual error code
                throw new Exception($"[CellPngDec] Can't find decoder handle: {handle:X}");
            }

            return CellOk;
        }

        internal ulong CellPngDecClose(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);

            if (TryGetPngDec(handle, out PngDecoder? decoder))
            {
                decoder.Close();
            }
            else
            {
                // Todo: Return actual error code
                throw new Exception($"[CellPngDec] Can't find decoder handle: {handle:X}");
            }

            return CellOk;
        }

        internal ulong CellPngDecDestroy(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);

            if (TryGetPngDec(handle, out PngDecoder? decoder))
            {
                decoder.Close();
                RemovePngDec(handle);
            }
            else
            {
                // Todo: Return actual error code
                throw new Exception($"[CellPngDec] Can't find decoder handle: {handle:X}");
            }

            return CellOk;
        }

        private void AddPngDecoder(PngDecoder pngDecoder)
        {
            _decoders.TryAdd(pngDecoder.Handle, pngDecoder);
        }

        private bool TryGetPngDec(uint handle, [NotNullWhen(true)] out PngDecoder? pngDecoder)
        {
            return _decoders.TryGetValue(handle, out pngDecoder);
        }

        private void RemovePngDec(uint handle)
        {
            _decoders.TryRemove(handle, out _);
        }

        public void Dispose()
        {
            foreach(KeyValuePair<uint, PngDecoder> decoder in _decoders)
            {
                decoder.Value.Dispose();
            }

            _decoders.Clear();
        }
    }
}