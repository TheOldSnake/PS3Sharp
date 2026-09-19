using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System;
using System.Collections.Generic;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules {
    public static class CellNetCtl {
        public const uint CELL_NET_CTL_STATE_Disconnected = 0;
        public const uint CELL_NET_CTL_STATE_Connecting = 1;
        public const uint CELL_NET_CTL_STATE_IPObtaining = 2;
        public const uint CELL_NET_CTL_STATE_IPObtained = 3;


        public static ulong CellNetCtlGetState(PPU ppu) {
            GuestPtr<uint> state_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            MemoryManager.Write<uint>(state_ptr, CELL_NET_CTL_STATE_IPObtained);

            Console.WriteLine($"cellNetCtlGetState -> CELL_NET_CTL_STATE_IPObtained");
            return CELL_OK;
        }
    }
}
