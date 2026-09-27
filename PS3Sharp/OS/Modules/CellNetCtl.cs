using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal static class CellNetCtl
    {
        internal const uint CELL_NET_CTL_STATE_Disconnected = 0;
        internal const uint CELL_NET_CTL_STATE_Connecting = 1;
        internal const uint CELL_NET_CTL_STATE_IPObtaining = 2;
        internal const uint CELL_NET_CTL_STATE_IPObtained = 3;


        internal static ulong CellNetCtlGetState(PPU ppu)
        {
            GuestPtr<uint> state_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            MemoryManager.Write<uint>(state_ptr, CELL_NET_CTL_STATE_IPObtained);

            Console.WriteLine($"cellNetCtlGetState -> CELL_NET_CTL_STATE_IPObtained");
            return CELL_OK;
        }
    }
}
