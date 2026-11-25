using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class SCENP
    {
        internal const ulong SceNpDrmErrorLicenseNotFound = 0x80029521;
        internal const ulong SceNpBasicErrorNoEvent = 0x8002a66a;
        internal const ulong SceNpErrorOffline = 0x8002aa0c;
        internal const int SceNpManagerStatusOffline = -1;

        private readonly MemoryManager _memoryManager;

        internal SCENP(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SCENpInit(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpScoreInit(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpTrophyRegisterContext(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpManagerGetContentRatingFlag(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpBasicGetEvent(PPUThread ppu)
        {
            return SceNpBasicErrorNoEvent;
        }

        internal ulong SCENpManagerGetNpId(PPUThread ppu)
        {
            return SceNpErrorOffline;
        }

        internal ulong SCENpManagerGetStatus(PPUThread ppu)
        {
            GuestPtr<BigEndian<int>> stat_ptr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg0);
            _memoryManager.Write<BigEndian<int>>(stat_ptr, SceNpManagerStatusOffline);
            return CellOk;
        }

        internal ulong SCENpScoreTerm(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpDrmIsAvailable(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpBasicRegisterContextSensitiveHandler(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpManagerRegisterCallback(PPUThread ppu)
        {
            return CellOk;
        }
    }
}
