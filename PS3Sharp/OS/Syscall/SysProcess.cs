
using OpenTK.Graphics.ES20;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall {
    public static class SysProcess {
        public static ulong SysProcessExit(PPU ppu) {
            int exitCode = ppu.ReadArg<int>(ARG0);
            ppu.StopThread();

            Console.WriteLine($"[SysProcess] sys_process_exit - exit code = {exitCode} caller thread: [{ppu.ThreadName}]");

            if (exitCode > 0) {
                Console.WriteLine($"[SysProcess] sys_process_exit call stack:");
                PPUInterpreter.PrintCallstack(ppu);
            }

            PS3Sharp.TerminateEmulator(exitCode);
            return CELL_OK;
        }

        public static ulong SysProcessExit2(PPU ppu) {
            return SysProcessExit(ppu);
        }

        public static ulong SysProcessGetPID(PPU ppu) {
            return 0x1000500;
        }

        public static ulong SysProcessGetSDKVersion(PPU ppu) {
            uint pid = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            MemoryManager.Write<uint>(ptr, 0x00360001);
            return CELL_OK;
        }

        public static ulong SysProcessIsSPULockLineReservationAddress(PPU ppu) {
            uint address = ppu.ReadArg<uint>(ARG0);
            ulong flags = ppu.ReadArg<ulong>(ARG1);

            uint line = address >> 28; //lock line?
            if (line == 0xD || line == 0xF) {
                return CELL_EPERM;
            }

            return CELL_OK;
        }
    }
}
