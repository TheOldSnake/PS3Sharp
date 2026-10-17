using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class SCENP
    {
        internal const ulong SCE_NP_DRM_ERROR_LICENSE_NOT_FOUND = 0x80029521;
        internal const ulong SCE_NP_BASIC_ERROR_NO_EVENT = 0x8002a66a;
        internal const ulong SCE_NP_ERROR_OFFLINE = 0x8002aa0c;
        internal const int SCE_NP_MANAGER_STATUS_OFFLINE = -1;

        private readonly MemoryManager _memoryManager;

        internal SCENP(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SCENpInit(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpScoreInit(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpTrophyRegisterContext(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpManagerGetContentRatingFlag(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpBasicGetEvent(PPUThread ppu)
        {
            return SCE_NP_BASIC_ERROR_NO_EVENT;
        }

        internal ulong SCENpManagerGetNpId(PPUThread ppu)
        {
            return SCE_NP_ERROR_OFFLINE;
        }

        internal ulong SCENpManagerGetStatus(PPUThread ppu)
        {
            GuestPtr<int> stat_ptr = ppu.ReadArg<GuestPtr<int>>(ARG0);
            _memoryManager.Write<BigEndian<int>>(stat_ptr, SCE_NP_MANAGER_STATUS_OFFLINE);
            return CELL_OK;
        }

        internal ulong SCENpScoreTerm(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpDrmIsAvailable(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpBasicRegisterContextSensitiveHandler(PPUThread ppu)
        {
            return CELL_OK;
        }

        internal ulong SCENpManagerRegisterCallback(PPUThread ppu)
        {
            return CELL_OK;
        }
    }
}
