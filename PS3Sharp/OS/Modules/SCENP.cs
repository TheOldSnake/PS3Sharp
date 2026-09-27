using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class SCENP
    {
        public const ulong SCE_NP_DRM_ERROR_LICENSE_NOT_FOUND = 0x80029521;
        public const ulong SCE_NP_BASIC_ERROR_NO_EVENT = 0x8002a66a;
        public const ulong SCE_NP_ERROR_OFFLINE = 0x8002aa0c;
        public const int SCE_NP_MANAGER_STATUS_OFFLINE = -1;
        public static ulong SCENpInit(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpScoreInit(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpTrophyRegisterContext(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpManagerGetContentRatingFlag(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpBasicGetEvent(PPU ppu)
        {
            return SCE_NP_BASIC_ERROR_NO_EVENT;
        }

        public static ulong SCENpManagerGetNpId(PPU ppu)
        {
            return SCE_NP_ERROR_OFFLINE;
        }

        public static ulong SCENpManagerGetStatus(PPU ppu)
        {
            GuestPtr<int> stat_ptr = ppu.ReadArg<GuestPtr<int>>(ARG0);
            MemoryManager.Write<int>(stat_ptr, SCE_NP_MANAGER_STATUS_OFFLINE);
            return CELL_OK;
        }

        public static ulong SCENpScoreTerm(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpDrmIsAvailable(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpBasicRegisterContextSensitiveHandler(PPU ppu)
        {
            return CELL_OK;
        }

        public static ulong SCENpManagerRegisterCallback(PPU ppu)
        {
            return CELL_OK;
        }
    }
}
