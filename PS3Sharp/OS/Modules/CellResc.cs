using PS3Sharp.Cell.PPU;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class CellResc
    {
        private readonly MemoryManager _memoryManager;
        private readonly CellGcmSys _cellGcmSys;

        internal CellResc(MemoryManager memoryManager, CellGcmSys cellGcmSys)
        {
            _memoryManager = memoryManager;
            _cellGcmSys = cellGcmSys;
        }

        internal ulong CellRescInit(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescVideoOutResolutionId2RescBufferMode(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescSetDsts(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescSetDisplayMode(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescGetBufferSize(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescSetBufferAddress(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescGcmSurface2RescSrc(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescSetSrc(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong CellRescGetFlipStatus(PPUThread ppu)
        {
            return _cellGcmSys.CellGcmGetFlipStatus(ppu);
        }

        internal ulong CellRescResetFlipStatus(PPUThread ppu)
        {
            return _cellGcmSys.CellGcmResetFlipStatus(ppu);
        }

        internal ulong CellRescSetConvertAndFlip(PPUThread ppu)
        {
            return _cellGcmSys.CellGcmSetFlip(ppu);
        }

        internal ulong CellRescSetWaitFlip(PPUThread ppu)
        {
            return _cellGcmSys.CellGcmSetWaitFlip(ppu);
        }
    }
}
