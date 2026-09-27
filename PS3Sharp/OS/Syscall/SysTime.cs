using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    public static class SysTime
    {
        public static uint SysTimeGetCurrentTime(PPU ppu)
        {
            GuestPtr<long> sec = ppu.ReadArg<GuestPtr<long>>(ARG0);   //sys_time_sec_t*
            GuestPtr<long> nsec = ppu.ReadArg<GuestPtr<long>>(ARG1);  //sys_time_nsec_t* 

            MemoryManager.Write<long>(sec, (long)Utility.GetCurrentSecondsSince1970());
            MemoryManager.Write<long>(nsec, (long)Utility.GetCurrentNanosecondsSince1970());

            //Console.WriteLine("[SysTime] SysTimeGetCurrentTime");
            return CELL_OK;
        }

        public static ulong SysTimeGetSystemTime(PPU ppu)
        {
            //Console.WriteLine($"[SysPrxForUser] sys_time_get_system_time");
            return Utility.GetCurrentMicrosecondSince1970(); //in microseconds
        }

        public static ulong SysTimeGetTimeZone(PPU ppu)
        {
            GuestPtr<uint> timezone_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<uint> summertime_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);

            // todo: get actual timezone values

            MemoryManager.Write<uint>(timezone_ptr, 60);
            MemoryManager.Write<uint>(summertime_ptr, 00);
            return CELL_OK;
        }

        public static ulong SysTimeGetTimebaseFrequency(PPU ppu)
        {
            //Console.WriteLine("[SysTime] SysTimeGetTimebaseFrequency");
            return 80000000UL; //80 Mhz
        }
    }
}
