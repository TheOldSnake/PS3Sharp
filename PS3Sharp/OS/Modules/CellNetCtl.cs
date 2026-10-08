using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class CellNetCtl
    {
        internal const uint CELL_NET_CTL_STATE_Disconnected = 0;
        internal const uint CELL_NET_CTL_STATE_Connecting = 1;
        internal const uint CELL_NET_CTL_STATE_IPObtaining = 2;
        internal const uint CELL_NET_CTL_STATE_IPObtained = 3;

        private readonly MemoryManager _memoryManager;

        internal CellNetCtl(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellNetCtlGetState(PPUThread ppu)
        {
            GuestPtr<uint> state_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            _memoryManager.Write<uint>(state_ptr, CELL_NET_CTL_STATE_IPObtained);

            Console.WriteLine($"cellNetCtlGetState -> CELL_NET_CTL_STATE_IPObtained");
            return CELL_OK;
        }
    }
}
