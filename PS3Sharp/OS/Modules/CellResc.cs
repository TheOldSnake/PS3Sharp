using PS3Sharp.Cell.PPU;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal static class CellResc
    {
        internal static ulong CellRescInit(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] cellRescInit");
            return CELL_OK;
        }

        internal static ulong CellRescVideoOutResolutionId2RescBufferMode(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] CellRescVideoOutResolutionId2RescBufferMode");
            return CELL_OK;
        }

        internal static ulong CellRescSetDsts(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] cellRescSetDsts");
            return CELL_OK;
        }

        internal static ulong CellRescSetDisplayMode(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] cellRescSetDisplayMode");
            return CELL_OK;
        }

        internal static ulong CellRescGetBufferSize(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] cellRescGetBufferSize");
            return CELL_OK;
        }

        internal static ulong CellRescSetBufferAddress(PPU ppu)
        {
            //TODO
            //   Console.WriteLine($"[CellResc] cellRescSetBufferAddress");
            return CELL_OK;
        }

        internal static ulong CellRescGcmSurface2RescSrc(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] cellRescGcmSurface2RescSrc");
            return CELL_OK;
        }

        internal static ulong CellRescSetSrc(PPU ppu)
        {
            //TODO
            //  Console.WriteLine($"[CellResc] cellRescSetSrc");
            return CELL_OK;
        }

        internal static ulong CellRescGetFlipStatus(PPU ppu)
        {
            // Console.WriteLine($"[CellResc] cellRescGetFlipStatus");
            return CellGcmSys.CellGcmGetFlipStatus(ppu);
        }

        internal static ulong CellRescResetFlipStatus(PPU ppu)
        {
            // Console.WriteLine($"[CellResc] cellRescResetFlipStatus");
            return CellGcmSys.CellGcmResetFlipStatus(ppu);
        }

        internal static ulong CellRescSetConvertAndFlip(PPU ppu)
        {
            //  Console.WriteLine($"[CellResc] cellRescSetConvertAndFlip");
            return CellGcmSys.CellGcmSetFlip(ppu);
        }

        internal static ulong CellRescSetWaitFlip(PPU ppu)
        {
            //   Console.WriteLine($"[CellResc] cellRescSetWaitFlip");
            return CellGcmSys.CellGcmSetWaitFlip(ppu);
        }
    }
}
