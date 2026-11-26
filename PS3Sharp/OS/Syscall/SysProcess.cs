using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysProcess
    {
        private readonly MemoryManager _memoryManager;

        internal SysProcess(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysProcessExit(PPUThread ppu)
        {
            int exitCode = ppu.ReadArg<int>(Arg0);
            ppu.StopThread();

            Console.WriteLine($"[SysProcess] sys_process_exit - exit code = {exitCode} caller thread: [{ppu.ThreadName}]");

            if (exitCode > 0)
            {
                Console.WriteLine($"[SysProcess] sys_process_exit call stack:");
                PPUInterpreter.PrintCallstack(ppu);
            }

            ppu.SetExitCode(exitCode);
            ppu.ThreadManager.RequestShutdown();

            return CellOk;
        }

        internal ulong SysProcessExit2(PPUThread ppu)
        {
            return SysProcessExit(ppu);
        }

        internal ulong SysProcessGetPID(PPUThread ppu)
        {
            return 0x1000500;
        }

        internal ulong SysProcessGetSDKVersion(PPUThread ppu)
        {
            uint pid = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<uint>> ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);
            _memoryManager.Write<BigEndian<uint>>(ptr, 0x00360001);
            return CellOk;
        }

        internal ulong SysProcessIsSPULockLineReservationAddress(PPUThread ppu)
        {
            uint address = ppu.ReadArg<uint>(Arg0);
            ulong flags = ppu.ReadArg<ulong>(Arg1);

            uint line = address >> 28; //lock line?
            if (line == 0xD || line == 0xF)
            {
                return CellEPerm;
            }

            return CellOk;
        }
    }
}
