using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal static class CellFs
    {
        internal static ulong CellFsAioInit(PPU ppu) => SysFs.SysFsAioInit(ppu);
        internal static ulong CellFsStat(PPU ppu) => SysFs.SysFsStat(ppu);
        internal static ulong CellFsFstat(PPU ppu) => SysFs.SysFsFstat(ppu);
        internal static ulong CellFsOpen(PPU ppu) => SysFs.SysFsOpen(ppu);
        internal static ulong CellFsLseek(PPU ppu) => SysFs.SysFsLseek(ppu);
        internal static ulong CellFsRead(PPU ppu) => SysFs.SysFsRead(ppu, false);
        internal static ulong CellFsWrite(PPU ppu) => SysFs.SysFsWrite(ppu);
        internal static ulong CellFsClose(PPU ppu) => SysFs.SysFsClose(ppu);
        internal static ulong CellFsMakedir(PPU ppu) => SysFs.SysFsMkDir(ppu);
        internal static ulong CellFsReaddir(PPU ppu) => SysFs.SysFsReadDir(ppu);
        internal static ulong CellFsOpenDir(PPU ppu) => SysFs.SysFsOpenDir(ppu);
        internal static ulong CellFsCloseDir(PPU ppu) => SysFs.SysFsCloseDir(ppu);

        internal static ulong CellFsGetFreeSize(PPU ppu)
        {
            GuestVoidPtr path_ptr = ppu.ReadArg<GuestVoidPtr>(ARG0);
            GuestPtr<uint> block_size_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            GuestPtr<ulong> block_cnt_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG2);

            MemoryManager.Write<uint>(block_size_ptr, 4096);
            MemoryManager.Write<ulong>(block_cnt_ptr, 5242880); // 20gb / 4096

            return CELL_OK;
        }
    }
}
