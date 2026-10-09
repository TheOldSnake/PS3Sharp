using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class CellRtc
    {
        internal struct CellRtcDateTime : IPS3Struct
        {
            internal BigEndian<ushort> Year;
            internal BigEndian<ushort> Month;
            internal BigEndian<ushort> Day;
            internal BigEndian<ushort> Hour;
            internal BigEndian<ushort> Minute;
            internal BigEndian<ushort> Second;
            internal BigEndian<uint> Microsecond;
        }

        private readonly MemoryManager _memoryManager;

        internal CellRtc(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellRtcGetCurrentTick(PPUThread ppu)
        {
            Console.WriteLine($"[CellRtc] cellRtcGetCurrentTick");
            uint ptr = ppu.ReadArg<uint>(ARG0);
            ulong tick = Utility.GetCurrentTickSince1970();
            _memoryManager.Write<ulong>(ptr, tick);
            return CELL_OK;
        }

        internal ulong CellRtcGetCurrentClockLocalTime(PPUThread ppu)
        {
            Console.WriteLine($"[CellRtc] cellRtcGetCurrentClockLocalTime");
            GuestPtr<CellRtcDateTime> clock_ptr = ppu.ReadArg<GuestPtr<CellRtcDateTime>>(ARG0);

            DateTime dateTimeNow = DateTime.Now;
            unsafe
            {
                CellRtcDateTime* clock = _memoryManager.GetPtr(clock_ptr);

                clock->Year = (ushort)dateTimeNow.Year;
                clock->Month = (ushort)dateTimeNow.Month;
                clock->Day = (ushort)dateTimeNow.Day;
                clock->Hour = (ushort)dateTimeNow.Hour;
                clock->Minute = (ushort)dateTimeNow.Minute;
                clock->Second = (ushort)dateTimeNow.Second;
                clock->Microsecond = (uint)dateTimeNow.Microsecond;
            }

            return CELL_OK;
        }
    }
}
