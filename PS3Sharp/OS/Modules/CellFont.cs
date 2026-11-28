using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using System.Runtime.CompilerServices;

using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.Ppu.PpuThread;

namespace PS3Sharp.OS.Modules
{
    // We LLE CellFont, this sealed class is purely for debugging purposes
    internal sealed class CellFont
    {
        internal struct CellFont_ : IPS3Struct
        {
            internal BigEndian<float> scale_x;
            internal BigEndian<float> scale_y;
            internal BigEndian<float> slant;
            internal BigEndian<uint> renderer_addr;

            internal BigEndian<uint> fontdata_addr;
            internal BigEndian<uint> origin;
            //struct stbtt_fontinfo* stbfont;
        }

        internal struct CellFontGlyphMetrics : IPS3Struct
        {
            internal BigEndian<float> width;
            internal BigEndian<float> height;

            internal BigEndian<float> h_bearingX;
            internal BigEndian<float> h_bearingY;
            internal BigEndian<float> h_advance;

            internal BigEndian<float> v_bearingX;
            internal BigEndian<float> v_bearingY;
            internal BigEndian<float> v_advance;
        }

        internal struct CellFontRenderSurface : IPS3Struct
        {
            internal BigEndianGuestVoidPtr buffer;
            internal BigEndian<int> widthByte;
            internal BigEndian<int> pixelSizeByte;
            internal BigEndian<int> width;
            internal BigEndian<int> height;

            // Scissor
            internal BigEndian<uint> sc_x0;
            internal BigEndian<uint> sc_y0;
            internal BigEndian<uint> sc_x1;
            internal BigEndian<uint> sc_y1;
        }

        internal struct CellFontImageTransInfo : IPS3Struct
        {
            internal BigEndianGuestVoidPtr image;
            internal BigEndian<uint> imageWidthByte;
            internal BigEndian<uint> imageWidth;
            internal BigEndian<uint> imageHeight;
            internal BigEndianGuestVoidPtr surface;
            internal BigEndian<uint> surfWidthByte;
        }

        private readonly MemoryManager _memoryManager;

        internal CellFont(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        private void PrintCellFontData(GuestPtr<CellFont_> font_ptr)
        {
            CellFont_ font = _memoryManager.Read<CellFont_>(font_ptr);
            uint[] data = new uint[Unsafe.SizeOf<CellFont_>() / 4];

            for (uint i = 0; i < data.Length; i++)
            {
                data[i] = _memoryManager.Read<BigEndian<uint>>(font_ptr + (i * 4));
            }

            Console.WriteLine($"CellFont data:");
            Console.WriteLine($"scale_x       = {font.scale_x: 00.000000}      = 0x{data[0]:X8}");
            Console.WriteLine($"scale_y       = {font.scale_y: 00.000000}      = 0x{data[1]:X8}");
            Console.WriteLine($"slant         = {font.slant:   00.000000}      = 0x{data[2]:X8}");
            Console.WriteLine($"renderer_addr = {font.renderer_addr:X8}        = 0x{data[3]:X8}");
            Console.WriteLine($"fontdata_addr = {font.fontdata_addr:X8}        = 0x{data[4]:X8}");
            Console.WriteLine($"origin        = {font.origin:X8}               = 0x{data[5]:X8}");
        }

        private void PrintCellFontGlyphMetricsData(GuestPtr<CellFontGlyphMetrics> metrics_ptr)
        {
            CellFontGlyphMetrics metrics = _memoryManager.Read<CellFontGlyphMetrics>(metrics_ptr);
            uint[] data = new uint[Unsafe.SizeOf<CellFontGlyphMetrics>() / 4];

            for (uint i = 0; i < data.Length; i++)
            {
                data[i] = _memoryManager.Read<BigEndian<uint>>(metrics_ptr + (i * 4));
            }

            Console.WriteLine($"CellFontGlyphMetrics data:");

            Console.WriteLine($"width       = {metrics.width: 00.000000}       = 0x{data[0]:X8}");
            Console.WriteLine($"height      = {metrics.height: 00.000000}      = 0x{data[1]:X8}");

            Console.WriteLine($"h_bearingX  = {metrics.h_bearingX: 00.000000}  = 0x{data[2]:X8}");
            Console.WriteLine($"h_bearingY  = {metrics.h_bearingY: 00.000000}  = 0x{data[3]:X8}");
            Console.WriteLine($"h_advance   = {metrics.h_advance: 00.000000}   = 0x{data[4]:X8}");

            Console.WriteLine($"v_bearingX  = {metrics.v_bearingX: 00.000000}  = 0x{data[5]:X8}");
            Console.WriteLine($"v_bearingY  = {metrics.v_bearingY: 00.000000}  = 0x{data[6]:X8}");
            Console.WriteLine($"v_advance   = {metrics.v_advance: 00.000000}   = 0x{data[7]:X8}");
        }

        private void PrintCellFontRenderSurfaceData(GuestPtr<CellFontRenderSurface> surface_ptr)
        {
            CellFontRenderSurface surface = _memoryManager.Read<CellFontRenderSurface>(surface_ptr);

            Console.WriteLine($"CellFontRenderSurface data:");

            Console.WriteLine($"buffer = 0x{surface.buffer:X8}");
            Console.WriteLine($"widthByte = 0x{surface.widthByte:X8}");
            Console.WriteLine($"pixelSizeByte = 0x{surface.pixelSizeByte:X8}");
            Console.WriteLine($"width = 0x{surface.width:X8}");
            Console.WriteLine($"height = 0x{surface.height:X8}");

            Console.WriteLine($"sc_x0 = 0x{surface.sc_x0:X8}");
            Console.WriteLine($"sc_y0 = 0x{surface.sc_y0:X8}");

            Console.WriteLine($"sc_x1 = 0x{surface.sc_x1:X8}");
            Console.WriteLine($"sc_y1 = 0x{surface.sc_y1:X8}");


            //Console.WriteLine("Buffer dump:");
            //for (uint i = 0; i < 1024; i += 4) {
            //    uint addr = surface.buffer + i;
            //    uint val = MemoryManager.Read<uint>(addr);
            //    Console.WriteLine($"[0x{addr:X8}] = 0x{val:X8}");
            //}
        }

        private void PrintCellFontImageTransInfoData(GuestPtr<CellFontImageTransInfo> image_trans_info_ptr)
        {
            CellFontImageTransInfo image_trans_info = _memoryManager.Read<CellFontImageTransInfo>(image_trans_info_ptr);

            Console.WriteLine($"CellFontImageTransInfo data:");

            Console.WriteLine($"image = 0x{image_trans_info.image:X8}");
            Console.WriteLine($"imageWidthByte = 0x{image_trans_info.imageWidthByte:X8}");
            Console.WriteLine($"imageWidth = 0x{image_trans_info.imageWidth:X8}");
            Console.WriteLine($"imageHeight = 0x{image_trans_info.imageHeight:X8}");
            Console.WriteLine($"surface = 0x{image_trans_info.surface:X8}");
            Console.WriteLine($"surfWidthByte = 0x{image_trans_info.surfWidthByte:X8}");

            //Console.WriteLine("Buffer dump:");

            //uint first = MemoryManager.Read<byte>(image_trans_info.image);
            //Console.WriteLine($"{first:X2}");

            //for (uint i = 1; i < 1024; i += 16) {
            //    uint addr = image_trans_info.image + i;

            //    uint val0 = MemoryManager.Read<uint>(addr + 0);
            //    uint val1 = MemoryManager.Read<uint>(addr + 4);
            //    uint val2 = MemoryManager.Read<uint>(addr + 8);
            //    uint val3 = MemoryManager.Read<uint>(addr + 12);

            //    Console.WriteLine($"{val0:X8} {val1:X8} {val2:X8} {val3:X8}");
            //}
        }

        internal void Log_CellFontGetCharGlyphMetrics(PpuGpr[] gprs)
        {
            GuestPtr<CellFont_> font_ptr = gprs[Arg0].Value32;
            uint code = gprs[Arg1].Value32;
            GuestPtr<CellFontGlyphMetrics> metrics_ptr = gprs[Arg2].Value32;


            char character = (char)code;
            Console.WriteLine($"CellFontGetCharGlyphMetrics Code = {code} char = {character}");
            PrintCellFontData(font_ptr);
            PrintCellFontGlyphMetricsData(metrics_ptr);

            throw new Exception("Done");
        }

        internal void Log_CellFontRenderCharGlyphImage(PpuGpr[] gprs, PpuFpr[] fprs)
        {
            GuestPtr<CellFont_> font_ptr = gprs[Arg0].Value32;
            uint code = gprs[Arg1].Value32;
            GuestPtr<CellFontRenderSurface> surface_ptr = gprs[Arg2].Value32;

            float x = (float)fprs[1].Float64;
            float y = (float)fprs[2].Float64;

            GuestPtr<CellFontGlyphMetrics> metrics_ptr = gprs[Arg5].Value32;
            GuestPtr<CellFontImageTransInfo> trans_info_ptr = gprs[Arg6].Value32;


            //char character = (char)code;
            //Console.WriteLine($"CellFontRenderCharGlyphImage Code = {code} char = {character} x = {x} y = {y}");

            PrintCellFontRenderSurfaceData(surface_ptr);
            PrintCellFontImageTransInfoData(trans_info_ptr);


            //for (int i = 0; i < fprs.Length; i++) {
            //    Console.WriteLine($"FPR[{i}] = {fprs[i].Float64}");
            //}

            //throw new Exception("Done");
        }
    }
}
