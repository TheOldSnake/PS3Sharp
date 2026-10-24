using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysTime
    {
        private readonly MemoryManager _memoryManager;

        internal SysTime(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysTimeGetCurrentTime(PPUThread ppu)
        {
            GuestPtr<BigEndian<long>> sec = ppu.ReadArg<GuestPtr<BigEndian<long>>>(Arg0);   //sys_time_sec_t*
            GuestPtr<BigEndian<long>> nsec = ppu.ReadArg<GuestPtr<BigEndian<long>>>(Arg1);  //sys_time_nsec_t* 

            _memoryManager.Write<BigEndian<long>>(sec, (long)Utility.GetCurrentSecondsSince1970());
            _memoryManager.Write<BigEndian<long>>(nsec, (long)Utility.GetCurrentNanosecondsSince1970());

            //Console.WriteLine("[SysTime] SysTimeGetCurrentTime");
            return CellOk;
        }

        internal ulong SysTimeGetSystemTime(PPUThread ppu)
        {
            //Console.WriteLine($"[SysPrxForUser] sys_time_get_system_time");
            return Utility.GetCurrentMicrosecondSince1970(); //in microseconds
        }

        internal ulong SysTimeGetTimeZone(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> timezone_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<BigEndian<uint>> summertime_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);

            // todo: get actual timezone values

            _memoryManager.Write<BigEndian<uint>>(timezone_ptr, 60);
            _memoryManager.Write<BigEndian<uint>>(summertime_ptr, 00);
            return CellOk;
        }

        internal ulong SysTimeGetTimebaseFrequency(PPUThread ppu)
        {
            //Console.WriteLine("[SysTime] SysTimeGetTimebaseFrequency");
            return 80000000UL; //80 Mhz
        }
    }
}
