using PS3Sharp.Cell.PPU;
using System;
using System.Collections.Generic;
using System.Text;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class CellIO
    {
        public static ulong CellMouseGetInfo(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong CellPadPeriphGetInfo(PPU ppu)
        {
            return CELL_OK;
        }
    }
}
