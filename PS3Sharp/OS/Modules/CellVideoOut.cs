using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules {   
    public static class CellVideoOut {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoOutConfiguration : IPS3Struct {
            public byte ResolutionId;
            public byte Format;
            public byte Aspect;
            public InlineData.InlineArray9<byte> Reserved;   // inline 9 bytes
            public BigEndian<uint> Pitch;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoResolution : IPS3Struct {
            public BigEndian<ushort> Width;               /* Screen width in pixels. */
            public BigEndian<ushort> Height;              /* Screen height in pixels. */
        }

        private const VideoResolution _defaultResolution = VideoResolution.Resolution720;

        public enum VideoResolution {
            Undefined = 0,                        
            Resolution1080 = 1,                   // 1920 x 1080
            Resolution720 = 2,                    // 1280 x 720
            Resolution480 = 4,                    // 640 x 480
            Resolution576 = 5,                    // 720 x 576
            Resolution1600x1080 = 10,             // 1600 x 1080
            Resolution1440x1080 = 11,             // 1440 x 1080
            Resolution1280x1080 = 12,             // 1280 x 1080
            Resolution960x1080 = 13               // 960 x 1080
        }

        public static readonly (ushort Width, ushort Height)[] VideoResolutionSizes = [
           (0, 0),        // 0  = Undefined

           (1920, 1080),  // 1  = Resolution1080
           (1280, 720),   // 2  = Resolution720

           (0, 0),        // 3  = unused?

           (640, 480),    // 4  = Resolution480
           (720, 576),    // 5  = Resolution576

           (0, 0),        // 6  = unused?
           (0, 0),        // 7  = unused?
           (0, 0),        // 8  = unused?
           (0, 0),        // 9  = unused?

           (1600, 1080),  // 10 = Resolution1600x1080
           (1440, 1080),  // 11 = Resolution1440x1080
           (1280, 1080),  // 12 = Resolution1280x1080
           (960, 1080),   // 13 = Resolution960x1080          
        ];

        public const byte CELL_VIDEO_OUT_SCAN_MODE_INTERLACE = 0x00;
        public const byte CELL_VIDEO_OUT_SCAN_MODE_PROGRESSIVE = 0x01;

        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_NONE = 0x00;
        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_TO_WXGA = 0x01;
        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_TO_SXGA = 0x02;
        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_TO_WUXGA = 0x03;
        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_TO_1080 = 0x05;
        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_TO_REMOTEPLAY = 0x10;
        public const byte CELL_VIDEO_OUT_DISPLAY_CONVERSION_TO_720_3D_FRAME_PACKING = 0x80;

        public const byte CELL_VIDEO_OUT_ASPECT_AUTO = 0x00;
        public const byte CELL_VIDEO_OUT_ASPECT_4_3 = 0x01;
        public const byte CELL_VIDEO_OUT_ASPECT_16_9 = 0x02;

        public const ushort CELL_VIDEO_OUT_REFRESH_RATE_AUTO = 0x0000;
        public const ushort CELL_VIDEO_OUT_REFRESH_RATE_59_94HZ = 0x0001;
        public const ushort CELL_VIDEO_OUT_REFRESH_RATE_50HZ = 0x0002;
        public const ushort CELL_VIDEO_OUT_REFRESH_RATE_60HZ = 0x0004;
        public const ushort CELL_VIDEO_OUT_REFRESH_RATE_30HZ = 0x0008;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoDisplayMode : IPS3Struct {
            public byte ResolutionID;
            public byte ScanMode;
            public byte Conversion;
            public byte Aspect;
            public InlineData.InlineArray2<byte> Padding;
            public BigEndian<ushort> RefreshRates;
        }

        public const byte CELL_VIDEO_OUT_OUTPUT_STATE_ENABLED = 0x00;
        public const byte CELL_VIDEO_OUT_OUTPUT_STATE_DISABLED = 0x01;
        public const byte CELL_VIDEO_OUT_OUTPUT_STATE_PREPARING = 0x02;

        public const byte CELL_VIDEO_OUT_COLOR_SPACE_RGB = 0x01;
        public const byte CELL_VIDEO_OUT_COLOR_SPACE_YUV = 0x02;
        public const byte CELL_VIDEO_OUT_COLOR_SPACE_XVYCC = 0x04;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoState : IPS3Struct {
            public byte State;
            public byte ColorSpace;
            public InlineData.InlineArray6<byte> Padding;
            public CellVideoDisplayMode DisplayMode;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoOutColorInfo {
            public BigEndian<ushort> RedX;
            public BigEndian<ushort> RedY;
            public BigEndian<ushort> GreenX;
            public BigEndian<ushort> GreenY;
            public BigEndian<ushort> BlueX;
            public BigEndian<ushort> BlueY;
            public BigEndian<ushort> WhiteX;
            public BigEndian<ushort> WhiteY;
            public BigEndian<uint> Gamma;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoOutDisplayMode {
            public byte ResolutionId;
            public byte ScanMode;
            public byte Conversion;
            public byte Aspect;
            public InlineData.InlineArray2<byte> Reserved;
            public BigEndian<ushort> RefreshRates;
        }

        public const byte CELL_VIDEO_OUT_PORT_NONE = 0x00;
        public const byte CELL_VIDEO_OUT_PORT_HDMI = 0x01;
        public const byte CELL_VIDEO_OUT_PORT_NETWORK = 0x41;
        public const byte CELL_VIDEO_OUT_PORT_COMPOSITE_S = 0x81;
        public const byte CELL_VIDEO_OUT_PORT_D = 0x82;
        public const byte CELL_VIDEO_OUT_PORT_COMPONENT = 0x83;
        public const byte CELL_VIDEO_OUT_PORT_RGB = 0x84;
        public const byte CELL_VIDEO_OUT_PORT_AVMULTI_SCART = 0x85;
        public const byte CELL_VIDEO_OUT_PORT_DSUB = 0x86;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellVideoOutDeviceInfo : IPS3Struct {
            public byte PortType;
            public byte ColorSpace;
            public BigEndian<ushort> Latency;
            public byte NumberOfAvailableModes;
            public byte State;
            public byte RGBOutputRange;
            public InlineData.InlineArray5<byte> Reserved;
            public CellVideoOutColorInfo ColorInfo;
            public InlineData.InlineArray32<CellVideoOutDisplayMode> AvailableModes;
        }

        
        public static CellVideoOutConfiguration VideoConfiguration = new CellVideoOutConfiguration {
            ResolutionId = (byte)_defaultResolution,
            Format = 0,
            Aspect = CELL_VIDEO_OUT_ASPECT_16_9,
            Pitch = 0,
        };


        public static ulong CellVideoOutGetResolutionAvailability(PPU ppu) {
            //TODO
            uint video_out = ppu.ReadArg<uint>(ARG0);
            uint resolutionID = ppu.ReadArg<uint>(ARG1);
            uint aspect = ppu.ReadArg<uint>(ARG2);
            uint option = ppu.ReadArg<uint>(ARG3);

            Console.WriteLine($"[CellSysutil] cellVideoOutGetResolutionAvailability video_out = {video_out} resolution = {(VideoResolution)resolutionID} aspect = {aspect} option = {option}");

            return 1; //seems that 1 means resolution is available
        }

        public static ulong CellVideoOutConfigure(PPU ppu) {
            //TODO
            uint video_out = ppu.ReadArg<uint>(ARG0);
            GuestPtr<CellVideoOutConfiguration> config_ptr = ppu.ReadArg<GuestPtr<CellVideoOutConfiguration>>(ARG1);
            uint option_ptr = ppu.ReadArg<uint>(ARG2);
            uint wait_for_event = ppu.ReadArg<uint>(ARG3);

            unsafe {
                CellVideoOutConfiguration* config = config_ptr.AsNativePtr();
                VideoConfiguration.ResolutionId = config->ResolutionId;
                VideoConfiguration.Format = config->Format;
                VideoConfiguration.Aspect = config->Aspect;
                VideoConfiguration.Pitch = config->Pitch;
            }

            VideoResolution res = (VideoResolution)VideoConfiguration.ResolutionId;
            Console.WriteLine($"[CellSysutil] cellVideoOutConfigure: Res = {res} Format = {VideoConfiguration.Format} Aspect = {VideoConfiguration.Aspect} Pitch = {VideoConfiguration.Pitch}");
            return CELL_OK;
        }

        public static ulong CellVideoOutGetResolution(PPU ppu) {
            uint resolutionID = ppu.ReadArg<uint>(ARG0);
            GuestPtr<CellVideoResolution> resPtr = ppu.ReadArg<GuestPtr<CellVideoResolution>>(ARG1);

            Console.WriteLine($"[CellGcmSys] cellVideoOutGetResolution resID = {(VideoResolution)resolutionID} resPtr = {resPtr:X8}");

            (ushort width, ushort height) = VideoResolutionSizes[resolutionID];

            unsafe {
                CellVideoResolution* videoResolutionStruct = resPtr.AsNativePtr();
                if (width > 0 && height > 0) {
                    videoResolutionStruct->Width = width;
                    videoResolutionStruct->Height = height;
                } else {
                    Console.WriteLine($"[CellVideoOut] Undefined Resolution -- Returning 1280x720");
                    videoResolutionStruct->Width = 1280;
                    videoResolutionStruct->Height = 720;
                }
            }

            return CELL_OK;
        }

        public static ulong CellVideoOutGetState(PPU ppu) {
            //TODO
            uint video_out = ppu.ReadArg<uint>(ARG0);
            uint deviceIndex = ppu.ReadArg<uint>(ARG1);
            GuestPtr<CellVideoState> state_ptr = ppu.ReadArg<uint>(ARG2);

            byte resID;

            unsafe {
                CellVideoState* state = state_ptr.AsNativePtr();
                state->State = CELL_VIDEO_OUT_OUTPUT_STATE_ENABLED;
                state->ColorSpace = CELL_VIDEO_OUT_COLOR_SPACE_RGB;
                state->DisplayMode.ResolutionID = VideoConfiguration.ResolutionId;
                state->DisplayMode.ScanMode = CELL_VIDEO_OUT_SCAN_MODE_PROGRESSIVE;
                state->DisplayMode.Conversion = CELL_VIDEO_OUT_DISPLAY_CONVERSION_NONE;

                //TODO: Calculate the correct aspect ratio for AUTO
                state->DisplayMode.Aspect = VideoConfiguration.Aspect != CELL_VIDEO_OUT_ASPECT_AUTO ? VideoConfiguration.Aspect : CELL_VIDEO_OUT_ASPECT_16_9;
                state->DisplayMode.RefreshRates = CELL_VIDEO_OUT_REFRESH_RATE_59_94HZ;

                resID = state->DisplayMode.ResolutionID;
            }

            Console.WriteLine($"[CellSysutil] cellVideoOutGetState video_out = {video_out} state.DisplayMode.ResolutionID = {resID} deviceIndex = {deviceIndex}");
            return CELL_OK;
        }

        public static ulong CellVideoOutGetNumberOfDevice(PPU ppu) {
            //TODO
            uint video_out = ppu.ReadArg<uint>(ARG0);
            Console.WriteLine($"[CellSysutil] cellVideoOutGetNumberOfDevice video_out = {video_out}");
            return 1;
        }

        public static ulong CellVideoOutGetDeviceInfo(PPU ppu) {
            //TODO
            uint video_out = ppu.ReadArg<uint>(ARG0);
            uint deviceIndex = ppu.ReadArg<uint>(ARG1);

            GuestPtr<CellVideoOutDeviceInfo> info_ptr = ppu.ReadArg<GuestPtr<CellVideoOutDeviceInfo>>(ARG2);

            unsafe {
                CellVideoOutDeviceInfo* info = info_ptr.AsNativePtr();
                info->PortType = CELL_VIDEO_OUT_PORT_HDMI;
                info->State = CELL_VIDEO_OUT_OUTPUT_STATE_ENABLED;
                info->RGBOutputRange = 1;
                info->NumberOfAvailableModes = 1;

                info->AvailableModes[0].ResolutionId = (byte)VideoResolution.Resolution720;
                info->AvailableModes[0].ScanMode = CELL_VIDEO_OUT_SCAN_MODE_PROGRESSIVE;
                info->AvailableModes[0].Conversion = CELL_VIDEO_OUT_DISPLAY_CONVERSION_NONE;
                info->AvailableModes[0].Aspect = CELL_VIDEO_OUT_ASPECT_16_9;
                info->AvailableModes[0].RefreshRates = CELL_VIDEO_OUT_REFRESH_RATE_59_94HZ;
            }

            Console.WriteLine($"[CellSysutil] cellVideoOutGetDeviceInfo");
            return CELL_OK;
        }
    }
}
