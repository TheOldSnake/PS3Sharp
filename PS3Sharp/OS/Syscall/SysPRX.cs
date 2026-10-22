using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.NID;

using static PS3Sharp.Cell.Arguments;

namespace PS3Sharp.OS.Syscall
{
    internal class SysPRX // used by vsh
    {
        private readonly MemoryManager _memoryManager;

        internal SysPRX(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysPRXLoadModule(PPUThread ppu)
        {
            GuestVoidPtr path_ptr = ppu.ReadArg<GuestVoidPtr>(Arg0); //const char* path
            string path = _memoryManager.ReadString(path_ptr);
            Console.WriteLine($"SysPRXLoadModule: {path}");
            return 0; // should return module id?
        }

        internal ulong SysPRXGetModuleIDByName(PPUThread ppu)
        {
            GuestVoidPtr name_ptr = ppu.ReadArg<GuestVoidPtr>(Arg0); //const char* path
            string name = _memoryManager.ReadString(name_ptr);
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
