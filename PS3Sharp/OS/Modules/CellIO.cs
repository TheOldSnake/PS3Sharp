using PS3Sharp.Cell.PPU;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal static class CellIO
    {
        internal static ulong CellMouseGetInfo(PPU ppu)
        {
            return CELL_OK;
        }

        internal static ulong CellPadPeriphGetInfo(PPU ppu)
        {
            return CELL_OK;
        }
    }
}
