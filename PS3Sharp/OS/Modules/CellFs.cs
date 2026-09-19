using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules {
    public static class CellFs {
        public static ulong CellFsAioInit(PPU ppu) => SysFs.SysFsAioInit(ppu);
        public static ulong CellFsStat(PPU ppu) => SysFs.SysFsStat(ppu);
        public static ulong CellFsFstat(PPU ppu) => SysFs.SysFsFstat(ppu);
        public static ulong CellFsOpen(PPU ppu) => SysFs.SysFsOpen(ppu);
        public static ulong CellFsLseek(PPU ppu) => SysFs.SysFsLseek(ppu);
        public static ulong CellFsRead(PPU ppu) => SysFs.SysFsRead(ppu, false);
        public static ulong CellFsWrite(PPU ppu) => SysFs.SysFsWrite(ppu);
        public static ulong CellFsClose(PPU ppu) => SysFs.SysFsClose(ppu);
        public static ulong CellFsMakedir(PPU ppu) => SysFs.SysFsMkDir(ppu);
        public static ulong CellFsReaddir(PPU ppu) => SysFs.SysFsReadDir(ppu);
        public static ulong CellFsOpenDir(PPU ppu) => SysFs.SysFsOpenDir(ppu);
        public static ulong CellFsCloseDir(PPU ppu) => SysFs.SysFsCloseDir(ppu);

        public static ulong CellFsGetFreeSize(PPU ppu) {
            GuestVoidPtr path_ptr = ppu.ReadArg<GuestVoidPtr>(ARG0);
            GuestPtr<uint> block_size_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            GuestPtr<ulong> block_cnt_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG2);

            MemoryManager.Write<uint>(block_size_ptr, 4096);
            MemoryManager.Write<ulong>(block_cnt_ptr, 5242880); // 20gb / 4096

            return CELL_OK;
        }
    }
}
