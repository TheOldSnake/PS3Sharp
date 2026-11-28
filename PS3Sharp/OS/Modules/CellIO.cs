using PS3Sharp.Cell.Ppu;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellIO
    {
        private readonly MemoryManager _memoryManager;

        internal CellIO(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellMouseGetInfo(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong CellPadPeriphGetInfo(PpuThread ppu)
        {
            return CellOk;
        }
    }
}
