using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using System.Diagnostics;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall {
    public static class SysTimer {
        public static uint SysTimerUSleep(PPU ppu) {
            ulong time_us = ppu.ReadArg<ulong>(ARG0);
            // SleepMicroseconds((long)time_us);

            ulong start = Utility.ReadTime_Microseconds();
            ulong duration = time_us;

            while ((Utility.ReadTime_Microseconds() - start) < duration) {
                Thread.Yield();
            }

            //Console.WriteLine($"[{ppu.ThreadName}] sys_timer_usleep: {time_us} us -- pc: {ppu.PC:X8} lr: {ppu.LR:X8}");
            return CELL_OK;
        }

        public static uint SysTimerSleep(PPU ppu) {
            ulong time_s = ppu.ReadArg<ulong>(ARG0);
            //int time_ms = (int)time_s * 1000;
            //Thread.Sleep(time_ms);

            ulong start = Utility.ReadTime_Microseconds();
            ulong duration = time_s * 1_000_000;

            while ((Utility.ReadTime_Microseconds() - start) < duration) {
                Thread.Yield();
            }

            //Console.WriteLine($"sys_timer_sleep: {time_s} s");
            return CELL_OK;
        }
    }
}
