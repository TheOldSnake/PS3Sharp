using PS3Sharp.Cell.Ppu;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
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

        internal ulong SCENpInit(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpScoreInit(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpTrophyRegisterContext(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpManagerGetContentRatingFlag(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpBasicGetEvent(PpuThread ppu)
        {
            return SceNpBasicErrorNoEvent;
        }

        internal ulong SCENpManagerGetNpId(PpuThread ppu)
        {
            return SceNpErrorOffline;
        }

        internal ulong SCENpManagerGetStatus(PpuThread ppu)
        {
            GuestPtr<BigEndian<int>> stat_ptr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg0);
            _memoryManager.Write<BigEndian<int>>(stat_ptr, SceNpManagerStatusOffline);
            return CellOk;
        }

        internal ulong SCENpScoreTerm(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpDrmIsAvailable(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpBasicRegisterContextSensitiveHandler(PpuThread ppu)
        {
            return CellOk;
        }

        internal ulong SCENpManagerRegisterCallback(PpuThread ppu)
        {
            return CellOk;
        }
    }
}
