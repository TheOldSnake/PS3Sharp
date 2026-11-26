using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellVideoOut
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoOutConfiguration : IPS3Struct
        {
            internal byte ResolutionId;
            internal byte Format;
            internal byte Aspect;
            internal InlineData.InlineArray9<byte> Reserved;   // inline 9 bytes
            internal BigEndian<uint> Pitch;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoResolution : IPS3Struct
        {
            internal BigEndian<ushort> Width;               /* Screen width in pixels. */
            internal BigEndian<ushort> Height;              /* Screen height in pixels. */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoDisplayMode : IPS3Struct
        {
            internal byte ResolutionID;
            internal byte ScanMode;
            internal byte Conversion;
            internal byte Aspect;
            internal InlineData.InlineArray2<byte> Padding;
            internal BigEndian<ushort> RefreshRates;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoState : IPS3Struct
        {
            internal byte State;
            internal byte ColorSpace;
            internal InlineData.InlineArray6<byte> Padding;
            internal CellVideoDisplayMode DisplayMode;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoOutColorInfo
        {
            internal BigEndian<ushort> RedX;
            internal BigEndian<ushort> RedY;
            internal BigEndian<ushort> GreenX;
            internal BigEndian<ushort> GreenY;
            internal BigEndian<ushort> BlueX;
            internal BigEndian<ushort> BlueY;
            internal BigEndian<ushort> WhiteX;
            internal BigEndian<ushort> WhiteY;
            internal BigEndian<uint> Gamma;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoOutDisplayMode
        {
            internal byte ResolutionId;
            internal byte ScanMode;
            internal byte Conversion;
            internal byte Aspect;
            internal InlineData.InlineArray2<byte> Reserved;
            internal BigEndian<ushort> RefreshRates;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellVideoOutDeviceInfo : IPS3Struct
        {
            internal byte PortType;
            internal byte ColorSpace;
            internal BigEndian<ushort> Latency;
            internal byte NumberOfAvailableModes;
            internal byte State;
            internal byte RGBOutputRange;
            internal InlineData.InlineArray5<byte> Reserved;
            internal CellVideoOutColorInfo ColorInfo;
            internal InlineData.InlineArray32<CellVideoOutDisplayMode> AvailableModes;
        }

        internal enum VideoResolution
        {
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

        internal readonly (ushort Width, ushort Height)[] VideoResolutionSizes =
        [
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

        internal const byte CellVideoOutScanModeInterlace = 0x00;
        internal const byte CellVideoOutScanModeProgressive = 0x01;

        internal const byte CellVideoOutDisplayConversionNone = 0x00;
        internal const byte CellVideoOutDisplayConversionToWxga = 0x01;
        internal const byte CellVideoOutDisplayConversionToSxga = 0x02;
        internal const byte CellVideoOutDisplayConversionToWuxga = 0x03;
        internal const byte CellVideoOutDisplayConversionTo1080 = 0x05;
        internal const byte CellVideoOutDisplayConversionToRemoteplay = 0x10;
        internal const byte CellVideoOutDisplayConversionTo7203dFramePacking = 0x80;

        internal const byte CellVideoOutAspectAuto = 0x00;
        internal const byte CellVideoOutAspect43 = 0x01;
        internal const byte CellVideoOutAspect169 = 0x02;

        internal const ushort CellVideoOutRefreshRateAuto = 0x0000;
        internal const ushort CellVideoOutRefreshRate5994hz = 0x0001;
        internal const ushort CellVideoOutRefreshRate50hz = 0x0002;
        internal const ushort CellVideoOutRefreshRate60hz = 0x0004;
        internal const ushort CellVideoOutRefreshRate30hz = 0x0008;

        internal const byte CellVideoOutOutputStateEnabled = 0x00;
        internal const byte CellVideoOutOutputStateDisabled = 0x01;
        internal const byte CellVideoOutOutputStatePreparing = 0x02;

        internal const byte CellVideoOutColorSpaceRgb = 0x01;
        internal const byte CellVideoOutColorSpaceYuv = 0x02;
        internal const byte CellVideoOutColorSpaceXvycc = 0x04;

        internal const byte CellVideoOutPortNone = 0x00;
        internal const byte CellVideoOutPortHdmi = 0x01;
        internal const byte CellVideoOutPortNetwork = 0x41;
        internal const byte CellVideoOutPortCompositeS = 0x81;
        internal const byte CellVideoOutPortD = 0x82;
        internal const byte CellVideoOutPortComponent = 0x83;
        internal const byte CellVideoOutPortRgb = 0x84;
        internal const byte CellVideoOutPortAvmultiScart = 0x85;
        internal const byte CellVideoOutPortDsub = 0x86;
        internal const VideoResolution DefaultResolution = VideoResolution.Resolution720;

        private readonly MemoryManager _memoryManager;

        internal CellVideoOutConfiguration VideoConfiguration = new()
        {
            ResolutionId = (byte)DefaultResolution,
            Format = 0,
            Aspect = CellVideoOutAspect169,
            Pitch = 0,
        };

        internal CellVideoOut(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellVideoOutGetResolutionAvailability(PPUThread ppu)
        {
            //TODO
            uint video_out = ppu.ReadArg<uint>(Arg0);
            uint resolutionID = ppu.ReadArg<uint>(Arg1);
            uint aspect = ppu.ReadArg<uint>(Arg2);
            uint option = ppu.ReadArg<uint>(Arg3);

            Console.WriteLine($"[CellSysutil] cellVideoOutGetResolutionAvailability video_out = {video_out} resolution = {(VideoResolution)resolutionID} aspect = {aspect} option = {option}");

            return 1; //seems that 1 means resolution is available
        }

        internal ulong CellVideoOutConfigure(PPUThread ppu)
        {
            //TODO
            uint video_out = ppu.ReadArg<uint>(Arg0);
            GuestPtr<CellVideoOutConfiguration> config_ptr = ppu.ReadArg<GuestPtr<CellVideoOutConfiguration>>(Arg1);
            uint option_ptr = ppu.ReadArg<uint>(Arg2);
            uint wait_for_event = ppu.ReadArg<uint>(Arg3);
            
            // Read/Save configuration struct
            VideoConfiguration = _memoryManager.Read<CellVideoOutConfiguration>(config_ptr);

            VideoResolution res = (VideoResolution)VideoConfiguration.ResolutionId;
            Console.WriteLine($"[CellSysutil] cellVideoOutConfigure: Res = {res} Format = {VideoConfiguration.Format} Aspect = {VideoConfiguration.Aspect} Pitch = {VideoConfiguration.Pitch}");
            return CellOk;
        }

        internal ulong CellVideoOutGetResolution(PPUThread ppu)
        {
            uint resolutionID = ppu.ReadArg<uint>(Arg0);
            GuestPtr<CellVideoResolution> resPtr = ppu.ReadArg<GuestPtr<CellVideoResolution>>(Arg1);

            Console.WriteLine($"[CellGcmSys] cellVideoOutGetResolution resID = {(VideoResolution)resolutionID} resPtr = {resPtr:X8}");

            (ushort width, ushort height) = VideoResolutionSizes[resolutionID];
            CellVideoResolution videoResolutionStruct = new();

            if (width > 0 && height > 0)
            {
                videoResolutionStruct.Width = width;
                videoResolutionStruct.Height = height;
            }
            else
            {
                Console.WriteLine($"[CellVideoOut] Undefined Resolution -- Returning 1280x720");
                videoResolutionStruct.Width = 1280;
                videoResolutionStruct.Height = 720;
            }

            _memoryManager.Write(resPtr, videoResolutionStruct);
            return CellOk;
        }

        internal ulong CellVideoOutGetState(PPUThread ppu)
        {
            //TODO
            uint video_out = ppu.ReadArg<uint>(Arg0);
            uint deviceIndex = ppu.ReadArg<uint>(Arg1);
            GuestPtr<CellVideoState> state_ptr = ppu.ReadArg<uint>(Arg2);

            CellVideoState state = new()
            {
                State = CellVideoOutOutputStateEnabled,
                ColorSpace = CellVideoOutColorSpaceRgb,
                DisplayMode = new()
                {
                    ResolutionID = VideoConfiguration.ResolutionId,
                    ScanMode = CellVideoOutScanModeProgressive,
                    Conversion = CellVideoOutDisplayConversionNone,

                    //TODO: Calculate the correct aspect ratio for AUTO
                    Aspect = VideoConfiguration.Aspect != CellVideoOutAspectAuto ? VideoConfiguration.Aspect : CellVideoOutAspect169,
                    RefreshRates = CellVideoOutRefreshRate5994hz,
                }
            };

            _memoryManager.Write(state_ptr, state);

            byte resID = state.DisplayMode.ResolutionID;
            Console.WriteLine($"[CellSysutil] cellVideoOutGetState video_out = {video_out} state.DisplayMode.ResolutionID = {resID} deviceIndex = {deviceIndex}");

            return CellOk;
        }

        internal ulong CellVideoOutGetNumberOfDevice(PPUThread ppu)
        {
            //TODO
            uint video_out = ppu.ReadArg<uint>(Arg0);
            Console.WriteLine($"[CellSysutil] cellVideoOutGetNumberOfDevice video_out = {video_out}");
            return 1;
        }

        internal ulong CellVideoOutGetDeviceInfo(PPUThread ppu)
        {
            //TODO
            uint video_out = ppu.ReadArg<uint>(Arg0);
            uint deviceIndex = ppu.ReadArg<uint>(Arg1);

            GuestPtr<CellVideoOutDeviceInfo> info_ptr = ppu.ReadArg<GuestPtr<CellVideoOutDeviceInfo>>(Arg2);

            CellVideoOutDisplayMode displayMode = new()
            {
                ResolutionId = (byte)VideoResolution.Resolution720,
                ScanMode = CellVideoOutScanModeProgressive,
                Conversion = CellVideoOutDisplayConversionNone,
                Aspect = CellVideoOutAspect169,
                RefreshRates = CellVideoOutRefreshRate5994hz
            };

            CellVideoOutDeviceInfo info = new()
            {
                PortType = CellVideoOutPortHdmi,
                State = CellVideoOutOutputStateEnabled,
                RGBOutputRange = 1,
                NumberOfAvailableModes = 1,
            };

            info.AvailableModes[0] = displayMode;

            _memoryManager.Write(info_ptr, info);
            Console.WriteLine($"[CellSysutil] cellVideoOutGetDeviceInfo");
            return CellOk;
        }
    }
}
