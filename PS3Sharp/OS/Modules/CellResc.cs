using PS3Sharp.Cell.Ppu;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellResc
    {
        private readonly MemoryManager _memoryManager;
        private readonly CellGcmSys _cellGcmSys;

        internal CellResc(MemoryManager memoryManager, CellGcmSys cellGcmSys)
        {
            _memoryManager = memoryManager;
            _cellGcmSys = cellGcmSys;
        }

        internal ulong CellRescInit(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescVideoOutResolutionId2RescBufferMode(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescSetDsts(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescSetDisplayMode(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescGetBufferSize(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescSetBufferAddress(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescGcmSurface2RescSrc(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescSetSrc(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellRescGetFlipStatus(PpuThread ppu)
        {
            return _cellGcmSys.CellGcmGetFlipStatus(ppu);
        }

        internal ulong CellRescResetFlipStatus(PpuThread ppu)
        {
            return _cellGcmSys.CellGcmResetFlipStatus(ppu);
        }

        internal ulong CellRescSetConvertAndFlip(PpuThread ppu)
        {
            return _cellGcmSys.CellGcmSetFlip(ppu);
        }

        internal ulong CellRescSetWaitFlip(PpuThread ppu)
        {
            return _cellGcmSys.CellGcmSetWaitFlip(ppu);
        }
    }
}
