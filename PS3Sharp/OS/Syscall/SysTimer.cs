using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysTimer
    {
        private readonly MemoryManager _memoryManager;

        internal SysTimer(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysTimerUSleep(PPUThread ppu)
        {
            ulong time_us = ppu.ReadArg<ulong>(Arg0);
            // SleepMicroseconds((long)time_us);

            ulong start = Utility.ReadTime_Microseconds();
            ulong duration = time_us;

            while ((Utility.ReadTime_Microseconds() - start) < duration)
            {
                Thread.Yield();
                if (ppu.ShouldExit)
                {
                    break;
                }
            }

            //Console.WriteLine($"[{ppu.ThreadName}] sys_timer_usleep: {time_us} us -- pc: {ppu.PC:X8} lr: {ppu.LR:X8}");
            return CellOk;
        }

        internal ulong SysTimerSleep(PPUThread ppu)
        {
            ulong time_s = ppu.ReadArg<ulong>(Arg0);
            //int time_ms = (int)time_s * 1000;
            //Thread.Sleep(time_ms);

            ulong start = Utility.ReadTime_Microseconds();
            ulong duration = time_s * 1_000_000;

            while ((Utility.ReadTime_Microseconds() - start) < duration)
            {
                Thread.Yield();
                if (ppu.ShouldExit)
                {
                    break;
                }
            }

            //Console.WriteLine($"sys_timer_sleep: {time_s} s");
            return CellOk;
        }
    }
}
