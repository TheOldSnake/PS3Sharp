using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellNetCtl
    {
        internal const uint CellNetCtlStateDisconnected = 0;
        internal const uint CellNetCtlStateConnecting = 1;
        internal const uint CellNetCtlStateIpobtaining = 2;
        internal const uint CellNetCtlStateIpobtained = 3;

        private readonly MemoryManager _memoryManager;

        internal CellNetCtl(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellNetCtlGetState(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> state_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            _memoryManager.Write<BigEndian<uint>>(state_ptr, CellNetCtlStateIpobtained);

            Console.WriteLine($"cellNetCtlGetState -> CELL_NET_CTL_STATE_IPObtained");
            return CellOk;
        }
    }
}
