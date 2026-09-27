using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.NID;

using static PS3Sharp.Cell.Arguments;

namespace PS3Sharp.OS.Syscall
{
    internal static class SysPRX
    { // used by vsh

        internal static ulong SysPRXLoadModule(PPU ppu)
        {
            GuestVoidPtr path_ptr = ppu.ReadArg<GuestVoidPtr>(ARG0); //const char* path
            string path = MemoryManager.ReadString(path_ptr);
            Console.WriteLine($"SysPRXLoadModule: {path}");
            return 0; // should return module id?
        }

        internal static ulong SysPRXGetModuleIDByName(PPU ppu)
        {
            GuestVoidPtr name_ptr = ppu.ReadArg<GuestVoidPtr>(ARG0); //const char* path
            string name = MemoryManager.ReadString(name_ptr);
            Console.WriteLine($"SysPRXGetModuleIDByName: {name}");

            // This is based on chonkystation
            if (name == "cellLibprof")
            {
                return 0x8001112e;   // CELL_PRX_ERROR_UNKNOWN_MODULE 
            }

            return NIDManager.GetNID(name);
        }
    }
}
