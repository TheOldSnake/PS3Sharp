using PS3Sharp.Cell.PPU;
using PS3Sharp.OS.NID;
using PS3Sharp.OS.Syscall;

namespace PS3Sharp.OS.Modules
{
    public static partial class ModuleHandler
    {

        // Bind Module functions
        private static readonly Dictionary<uint, Func<PPU, ulong>> _handlers = new() {
            // SysPrxForUser
            { NIDManager.GetNID("sys_process_exit"), SysPrxForUser.SysProcessExit },
            { NIDManager.GetNID("sys_initialize_tls"), SysPrxForUser.SysInitializeTLS },
            { NIDManager.GetNID("sys_mmapper_allocate_memory"), SysPrxForUser.SysMmapperAllocateMemory },
            { NIDManager.GetNID("sys_mmapper_free_memory"), SysPrxForUser.SysMmapperFreeMemory },
            { NIDManager.GetNID("sys_mmapper_unmap_memory"), SysPrxForUser.SysMmapperUnmapMemory },
            { NIDManager.GetNID("sys_spinlock_initialize"), SysPrxForUser.SysSpinlockInitialize },
            { NIDManager.GetNID("sys_spinlock_lock"), SysPrxForUser.SysSpinlockLock },
            { NIDManager.GetNID("sys_spinlock_unlock"), SysPrxForUser.SysSpinlockUnlock },
            { NIDManager.GetNID("_sys_memset"), SysPrxForUser._Sys_Memset },
            { NIDManager.GetNID("_sys_strncpy"), SysPrxForUser._Sys_Strncpy },

            // SysLwMutex
            { NIDManager.GetNID("sys_lwmutex_create"), SysLwMutex.SysLWmutexCreate },
            { NIDManager.GetNID("sys_lwmutex_lock"), SysLwMutex.SysLWmutexLock },
            { NIDManager.GetNID("sys_lwmutex_trylock"), SysLwMutex.SysLWmutexTryLock },
            { NIDManager.GetNID("sys_lwmutex_unlock"), SysLwMutex.SysLWmutexUnlock },
            { NIDManager.GetNID("sys_lwmutex_destroy"), SysLwMutex.SysLWmutexDestroy },

            // SysPPUThread
            { NIDManager.GetNID("sys_ppu_thread_get_id"), SysPPUThread.SysPPUThreadGetID },
            { NIDManager.GetNID("sys_ppu_thread_create"), SysPPUThread.SysPPUThreadCreate },
            { NIDManager.GetNID("sys_ppu_thread_exit"), SysPPUThread.SysPPUThreadExit },
            { NIDManager.GetNID("sys_ppu_thread_once"), SysPPUThread.SysPPUThreadOnce },

            // SysTime
            { NIDManager.GetNID("sys_time_get_system_time"), SysTime.SysTimeGetSystemTime },

            // SysPrxForUser 
            { NIDManager.GetNID("_sys_process_atexitspawn"), SysPrxForUser._Sys_Process_atexitspawn },
            { NIDManager.GetNID("_sys_process_at_Exitspawn"), SysPrxForUser._Sys_Process_at_Exitspawn },
            { NIDManager.GetNID("sys_game_process_exitspawn2"), SysPrxForUser.Sys_game_process_exitspawn2 },
            { NIDManager.GetNID("sys_prx_exitspawn_with_level"), SysPrxForUser.Sys_Prx_Exitspawn_With_Level },
            { NIDManager.GetNID("sys_spu_image_import"), SysPrxForUser.Sys_SPU_Image_Import },

            // CellGcmSys
            { NIDManager.GetNID("_cellGcmInitBody"), CellGcmSys.CellGcmInitBody },
            { NIDManager.GetNID("cellGcmSetDefaultCommandBuffer"), CellGcmSys.CellGcmSetDefaultCommandBuffer },
            { NIDManager.GetNID("cellGcmGetConfiguration"), CellGcmSys.CellGcmGetConfiguration },
            { NIDManager.GetNID("cellGcmGetControlRegister"), CellGcmSys.CellGcmGetControlRegister },
            { NIDManager.GetNID("cellGcmAddressToOffset"), CellGcmSys.CellGcmAddressToOffset },
            { NIDManager.GetNID("cellGcmGetLabelAddress"), CellGcmSys.CellGcmGetLabelAddress },
            { NIDManager.GetNID("cellGcmSetFlipMode"), CellGcmSys.CellGcmSetFlipMode },
            { NIDManager.GetNID("cellGcmSetDisplayBuffer"), CellGcmSys.CellGcmSetDisplayBuffer },
            { NIDManager.GetNID("cellGcmResetFlipStatus"), CellGcmSys.CellGcmResetFlipStatus },

            { NIDManager.GetNID("cellGcmSetFlip"), CellGcmSys.CellGcmSetFlip },
            { NIDManager.GetNID("_cellGcmSetFlipCommand"), CellGcmSys.CellGcmSetFlip },

            { NIDManager.GetNID("cellGcmSetWaitFlip"), CellGcmSys.CellGcmSetWaitFlip },
            { NIDManager.GetNID("cellGcmGetFlipStatus"), CellGcmSys.CellGcmGetFlipStatus },
            { NIDManager.GetNID("cellGcmGetTiledPitchSize"), CellGcmSys.CellGcmGetTiledPitchSize },
            { NIDManager.GetNID("cellGcmSetTileInfo"), CellGcmSys.CellGcmSetTileInfo },
            { NIDManager.GetNID("cellGcmBindTile"), CellGcmSys.CellGcmBindTile },
            { NIDManager.GetNID("cellGcmSetTile"), CellGcmSys.CellGcmSetTile },
            { NIDManager.GetNID("cellGcmBindZcull"), CellGcmSys.CellGcmBindZcull },
            { NIDManager.GetNID("cellGcmSetDebugOutputLevel"), CellGcmSys.CellGcmSetDebugOutputLevel },
            { NIDManager.GetNID("cellGcmMapMainMemory"), CellGcmSys.CellGcmMapMainMemory },
            { NIDManager.GetNID("cellGcmMapEaIoAddress"), CellGcmSys.CellGcmMapEaIoAddress },
            { NIDManager.GetNID("cellGcmMapEaIoAddressWithFlags"), CellGcmSys.CellGcmMapEaIoAddressWithFlags },
            { NIDManager.GetNID("cellGcmGetOffsetTable"), CellGcmSys.CellGcmGetOffsetTable },
            { NIDManager.GetNID("cellGcmSetVBlankFrequency"), CellGcmSys.CellGcmSetVBlankFrequency },
            { NIDManager.GetNID("cellGcmSetSecondVFrequency"), CellGcmSys.CellGcmSetSecondVFrequency },
            { NIDManager.GetNID("cellGcmSetVBlankHandler"), CellGcmSys.CellGcmSetVBlankHandler },
            { NIDManager.GetNID("cellGcmSetFlipHandler"), CellGcmSys.CellGcmSetFlipHandler },
            { NIDManager.GetNID("cellGcmGetLastFlipTime"), CellGcmSys.CellGcmGetLastFlipTime },
            { NIDManager.GetNID("cellGcmGetLastSecondVTime"), CellGcmSys.CellGcmGetLastSecondVTime },
            { NIDManager.GetNID("cellGcmGetDefaultCommandWordSize"), CellGcmSys.CellGcmGetDefaultCommandWordSize },
            { NIDManager.GetNID("cellGcmGetDefaultSegmentWordSize"), CellGcmSys.CellGcmGetDefaultSegmentWordSize },

            // CellAudio
            { NIDManager.GetNID("cellAudioInit"), CellAudio.CellAudioInit },
            { NIDManager.GetNID("cellAudioPortOpen"), CellAudio.CellAudioPortOpen },
            { NIDManager.GetNID("cellAudioGetPortConfig"), CellAudio.CellAudioGetPortConfig },
            { NIDManager.GetNID("cellAudioPortStart"), CellAudio.CellAudioPortStart },
            { NIDManager.GetNID("cellAudioPortStop"), CellAudio.CellAudioPortStop },
            { NIDManager.GetNID("cellAudioQuit"), CellAudio.CellAudioQuit },
            { NIDManager.GetNID("cellAudioCreateNotifyEventQueue"), CellAudio.CellAudioCreateNotifyEventQueue },
            { NIDManager.GetNID("cellAudioSetNotifyEventQueue"), CellAudio.CellAudioSetNotifyEventQueue },

            // CellVideoOut
            { NIDManager.GetNID("cellVideoOutGetResolutionAvailability"), CellVideoOut.CellVideoOutGetResolutionAvailability },
            { NIDManager.GetNID("cellVideoOutConfigure"), CellVideoOut.CellVideoOutConfigure },
            { NIDManager.GetNID("cellVideoOutGetResolution"), CellVideoOut.CellVideoOutGetResolution },
            { NIDManager.GetNID("cellVideoOutGetState"), CellVideoOut.CellVideoOutGetState },
            { NIDManager.GetNID("cellVideoOutGetNumberOfDevice"), CellVideoOut.CellVideoOutGetNumberOfDevice },
            { NIDManager.GetNID("cellVideoOutGetDeviceInfo"), CellVideoOut.CellVideoOutGetDeviceInfo },

             // SysUtil / AudioOut / SaveData / MsgDialog
            { NIDManager.GetNID("cellSysutilRegisterCallback"), SysUtil.CellSysutilRegisterCallback },
            { NIDManager.GetNID("cellSysutilUnregisterCallback"), SysUtil.CellSysutilUnregisterCallback },
            { NIDManager.GetNID("cellSysutilCheckCallback"), SysUtil.CellSysutilCheckCallback },

            { NIDManager.GetNID("cellAudioOutGetSoundAvailability"), SysUtil.CellAudioOutGetSoundAvailability },
            { NIDManager.GetNID("cellAudioOutConfigure"), SysUtil.CellAudioOutConfigure },
            { NIDManager.GetNID("cellAudioOutGetState"), SysUtil.CellAudioOutGetState },

            { NIDManager.GetNID("cellSysutilGetSystemParamInt"), SysUtil.CellSysutilGetSystemParamInt },
            { NIDManager.GetNID("cellSysutilGetSystemParamString"), SysUtil.CellSysutilGetSystemParamString },

            { NIDManager.GetNID("cellSaveDataUserAutoLoad"), SysUtil.CellSaveDataUserAutoLoad },
            { NIDManager.GetNID("cellSaveDataAutoLoad2"), SysUtil.CellSaveDataAutoLoad2 },
            { NIDManager.GetNID("cellSaveDataAutoSave2"), SysUtil.CellSaveDataAutoSave2 },

            { NIDManager.GetNID("cellMsgDialogOpen2"), SysUtil.CellMsgDialogOpen2 },
            { NIDManager.GetNID("cellMsgDialogProgressBarSetMsg"), SysUtil.CellMsgDialogProgressBarSetMsg },
            { NIDManager.GetNID("cellMsgDialogProgressBarInc"), SysUtil.CellMsgDialogProgressBarInc },
            { NIDManager.GetNID("cellMsgDialogClose"), SysUtil.CellMsgDialogClose },

            { NIDManager.GetNID("cellSaveDataEnableOverlay"), SysUtil.CellSaveDataEnableOverlay },

            // CellSysModule
            { NIDManager.GetNID("cellSysmoduleLoadModule"), CellSysModule.CellSysmoduleLoadModule },
            { NIDManager.GetNID("cellSysmoduleUnloadModule"), CellSysModule.CellSysmoduleUnloadModule },
            { NIDManager.GetNID("cellSysmoduleIsLoaded"), CellSysModule.CellSysmoduleIsLoaded },

            // CellPngDec
            { NIDManager.GetNID("cellPngDecCreate"), CellPNGDec.CellPngDecCreate },
            { NIDManager.GetNID("cellPngDecOpen"), CellPNGDec.CellPngDecOpen },
            { NIDManager.GetNID("cellPngDecReadHeader"), CellPNGDec.CellPngDecReadHeader },
            { NIDManager.GetNID("cellPngDecSetParameter"), CellPNGDec.CellPngDecSetParameter },
            { NIDManager.GetNID("cellPngDecDecodeData"), CellPNGDec.CellPngDecDecodeData },
            { NIDManager.GetNID("cellPngDecClose"), CellPNGDec.CellPngDecClose },
            { NIDManager.GetNID("cellPngDecDestroy"), CellPNGDec.CellPngDecDestroy },

            // CellResc
            { NIDManager.GetNID("cellRescInit"), CellResc.CellRescInit },
            { NIDManager.GetNID("cellRescVideoOutResolutionId2RescBufferMode"), CellResc.CellRescVideoOutResolutionId2RescBufferMode },
            { NIDManager.GetNID("cellRescSetDsts"), CellResc.CellRescSetDsts },
            { NIDManager.GetNID("cellRescSetDisplayMode"), CellResc.CellRescSetDisplayMode },
            { NIDManager.GetNID("cellRescGetBufferSize"), CellResc.CellRescGetBufferSize },
            { NIDManager.GetNID("cellRescSetBufferAddress"), CellResc.CellRescSetBufferAddress },
            { NIDManager.GetNID("cellRescGcmSurface2RescSrc"), CellResc.CellRescGcmSurface2RescSrc },
            { NIDManager.GetNID("cellRescSetSrc"), CellResc.CellRescSetSrc },
            { NIDManager.GetNID("cellRescGetFlipStatus"), CellResc.CellRescGetFlipStatus },
            { NIDManager.GetNID("cellRescResetFlipStatus"), CellResc.CellRescResetFlipStatus },
            { NIDManager.GetNID("cellRescSetConvertAndFlip"), CellResc.CellRescSetConvertAndFlip },
            { NIDManager.GetNID("cellRescSetWaitFlip"), CellResc.CellRescSetWaitFlip },

            // CellGame
            { NIDManager.GetNID("cellGameBootCheck"), CellGame.CellGameBootCheck },
            { NIDManager.GetNID("cellGameContentPermit"), CellGame.CellGameContentPermit },
            { NIDManager.GetNID("cellGameGetParamInt"), CellGame.CellGameGetParamInt },
            { NIDManager.GetNID("cellGameGetParamString"), CellGame.CellGameGetParamString },
            { NIDManager.GetNID("cellGameDataCheck"), CellGame.CellGameDataCheck },
            { NIDManager.GetNID("cellGameGetDiscContentInfoUpdatePath"), CellGame.CellGameGetDiscContentInfoUpdatePath },
            { NIDManager.GetNID("cellGamePatchCheck"), CellGame.CellGamePatchCheck },

            // CellRtc
            { NIDManager.GetNID("cellRtcGetCurrentTick"), CellRtc.CellRtcGetCurrentTick },
            { NIDManager.GetNID("cellRtcGetCurrentClockLocalTime"), CellRtc.CellRtcGetCurrentClockLocalTime },

            // CellFs
            { NIDManager.GetNID("cellFsAioInit"), CellFs.CellFsAioInit },
            { NIDManager.GetNID("cellFsStat"), CellFs.CellFsStat },
            { NIDManager.GetNID("cellFsFstat"), CellFs.CellFsFstat },
            { NIDManager.GetNID("cellFsOpen"), CellFs.CellFsOpen },
            { NIDManager.GetNID("cellFsLseek"), CellFs.CellFsLseek },
            { NIDManager.GetNID("cellFsRead"), CellFs.CellFsRead },
            { NIDManager.GetNID("cellFsWrite"), CellFs.CellFsWrite },
            { NIDManager.GetNID("cellFsClose"), CellFs.CellFsClose },
            { NIDManager.GetNID("cellFsMkdir"), CellFs.CellFsMakedir },
            { NIDManager.GetNID("cellFsReaddir"), CellFs.CellFsReaddir },
            { NIDManager.GetNID("cellFsOpendir"), CellFs.CellFsOpenDir },
            { NIDManager.GetNID("cellFsClosedir"), CellFs.CellFsCloseDir },
            { NIDManager.GetNID("cellFsGetFreeSize"), CellFs.CellFsGetFreeSize },

            // SCENpTrophy
            { NIDManager.GetNID("sceNpTrophyInit"), SCENPTrophy.SCENpTrophyInit },
            { NIDManager.GetNID("sceNpTrophyCreateContext"), SCENPTrophy.SCENpTrophyCreateContext },
            { NIDManager.GetNID("sceNpTrophyCreateHandle"), SCENPTrophy.SCENpTrophyCreateHandle },
            { NIDManager.GetNID("sceNpTrophyGetRequiredDiskSpace"), SCENPTrophy.SCENpTrophyGetRequiredDiskSpace },
            { NIDManager.GetNID("sceNpTrophyRegisterContext"), SCENPTrophy.SCENpTrophyRegisterContext },
            { NIDManager.GetNID("sceNpTrophyGetGameInfo"), SCENPTrophy.SCENpTrophyGetGameInfo },
            { NIDManager.GetNID("sceNpTrophyGetTrophyUnlockState"), SCENPTrophy.SCENpTrophyGetTrophyUnlockState },
            { NIDManager.GetNID("sceNpTrophyUnlockTrophy"), SCENPTrophy.SCENpTrophyUnlockTrophy },

            // CellPad
            { NIDManager.GetNID("cellPadInit"), CellPad.CellPadInit },
            { NIDManager.GetNID("cellPadSetPortSetting"), CellPad.CellPadSetPortSetting },
            { NIDManager.GetNID("cellPadGetInfo2"), CellPad.CellPadGetInfo2 },
            { NIDManager.GetNID("cellPadGetData"), CellPad.CellPadGetData },
            { NIDManager.GetNID("cellPadGetInfo"), CellPad.CellPadGetInfo },

            // SCENP
            { NIDManager.GetNID("sceNpInit"), SCENP.SCENpInit },
            { NIDManager.GetNID("sceNpScoreInit"), SCENP.SCENpScoreInit },
            { NIDManager.GetNID("sceNpManagerGetContentRatingFlag"), SCENP.SCENpManagerGetContentRatingFlag },
            { NIDManager.GetNID("sceNpBasicGetEvent"), SCENP.SCENpBasicGetEvent },
            { NIDManager.GetNID("sceNpManagerGetNpId"), SCENP.SCENpManagerGetNpId },
            { NIDManager.GetNID("sceNpManagerGetStatus"), SCENP.SCENpManagerGetStatus },
            { NIDManager.GetNID("sceNpScoreTerm"), SCENP.SCENpScoreTerm },
            { NIDManager.GetNID("sceNpDrmIsAvailable"), SCENP.SCENpDrmIsAvailable },
            { NIDManager.GetNID("sceNpBasicRegisterContextSensitiveHandler"), SCENP.SCENpBasicRegisterContextSensitiveHandler },
            { NIDManager.GetNID("sceNpManagerRegisterCallback"), SCENP.SCENpManagerRegisterCallback },

            // CellNetCtl
            { NIDManager.GetNID("cellNetCtlGetState"), CellNetCtl.CellNetCtlGetState },

            // CellIO
            { NIDManager.GetNID("cellMouseGetInfo"), CellIO.CellMouseGetInfo },
            { NIDManager.GetNID("cellPadPeriphGetInfo"), CellIO.CellPadPeriphGetInfo },


            // Stubs
            { NIDManager.GetNID("cellSysmoduleInitialize"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellSysutilEnableBgmPlayback"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellGcmInitDefaultFifoMode"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("sceNp2Init"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("sceNpDrmIsAvailable2"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellUserInfoGetStat"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellUserInfoGetList"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellRtcSetTick"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("sceNpBasicRegisterHandler"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("sys_net_initialize_network_ex"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellNetCtlInit"), ModuleHandler.FunctionStub },
            { NIDManager.GetNID("cellNetCtlAddHandler"), ModuleHandler.FunctionStub },
        };
    }
}
