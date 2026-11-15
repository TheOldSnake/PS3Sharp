using PS3Sharp.Cell.PPU;
using PS3Sharp.OS.NID;
using PS3Sharp.OS.Syscall;
using System.Collections.Immutable;

namespace PS3Sharp.OS
{
    internal partial class LV2SyscallDispatcher
    {
        private ImmutableDictionary<SyscallID, Func<PPUThread, ulong>> CreateSyscallTable()
        {
            // Create a syscall table
            ImmutableDictionary<SyscallID, Func<PPUThread, ulong>> syscallTable = new Dictionary<SyscallID, Func<PPUThread, ulong>>()
            {
                [SyscallID.SysTTYWrite] = SysTTYHandler.SysTTYWrite,

                [SyscallID.SysEventQueueCreate] = SysEventHandler.SysEventQueueCreate,
                [SyscallID.SysEventQueueDestroy] = SysEventHandler.SysEventQueueDestroy,
                [SyscallID.SysEventQueueDrain] = SysEventHandler.SysEventQueueDrain,
                [SyscallID.SysEventQueueReceive] = SysEventHandler.SysEventQueueReceive,
                [SyscallID.SysEventQueueTryReceive] = SysEventHandler.SysEventQueueTryReceive,

                [SyscallID.SysEventPortCreate] = SysEventHandler.SysEventPortCreate,
                [SyscallID.SysEventPortDestroy] = SysEventHandler.SysEventPortDestroy,
                [SyscallID.SysEventPortConnectIPC] = SysEventHandler.SysEventPortConnectIPC,
                [SyscallID.SysEventPortConnectLocal] = SysEventHandler.SysEventPortConnectLocal,
                [SyscallID.SysEventPortSend] = SysEventHandler.SysEventPortSend,
                [SyscallID.SysEventPortDisconnect] = SysEventHandler.SysEventPortDisconnect,

                [SyscallID.SysEventFlagCreate] = SysEventFlagHandler.SysEventFlagCreate,
                [SyscallID.SysEventFlagDestroy] = SysEventFlagHandler.SysEventFlagDestroy,
                [SyscallID.SysEventFlagSet] = SysEventFlagHandler.SysEventFlagSet,
                [SyscallID.SysEventFlagWait] = SysEventFlagHandler.SysEventFlagWait,
                [SyscallID.SysEventFlagClear] = SysEventFlagHandler.SysEventFlagClear,

                [SyscallID.SysFsOpen] = SysFsHandler.SysFsOpen,
                [SyscallID.SysFsClose] = SysFsHandler.SysFsClose,
                [SyscallID.SysFsFstat] = SysFsHandler.SysFsFstat,
                [SyscallID.SysFsRead] = SysFsHandler.SysFsRead,
                [SyscallID.SysFsWrite] = SysFsHandler.SysFsWrite,
                [SyscallID.SysFsLSeek] = SysFsHandler.SysFsLseek,

                [SyscallID.SysMemoryAllocate] = SysMemoryHandler.SysMemoryAllocate,
                [SyscallID.SysMemoryFree] = SysMemoryHandler.SysMemoryFree,
                [SyscallID.SysMemoryGetUserMemorySize] = SysMemoryHandler.SysMemoryGetUserMemorySize,

                [SyscallID.SysMMapperAllocateAddress] = SysMMapperHandler.SysMMapperAllocateAddress,
                [SyscallID.SysMMapperFreeAddress] = SysMMapperHandler.SysMMapperFreeAddress,
                [SyscallID.SysMMapperAllocateMemory] = SysMMapperHandler.SysMMapperAllocateMemory,
                [SyscallID.SysMMapperSearchAndMap] = SysMMapperHandler.SysMMapperSearchAndMap,

                [SyscallID.SysMutexCreate] = SysMutexHandler.SysMutexCreate,
                [SyscallID.SysMutexDestroy] = SysMutexHandler.SysMutexDestroy,
                [SyscallID.SysMutexLock] = SysMutexHandler.SysMutexLock,
                [SyscallID.SysMutexTryLock] = SysMutexHandler.SysMutexTryLock,
                [SyscallID.SysMutexUnlock] = SysMutexHandler.SysMutexUnlock,

                [SyscallID.SysCondCreate] = SysCondHandler.SysCondCreate,
                [SyscallID.SysCondDestroy] = SysCondHandler.SysCondDestroy,
                [SyscallID.SysCondSignal] = SysCondHandler.SysCondSignal,
                [SyscallID.SysCondSignalAll] = SysCondHandler.SysCondSignalAll,
                [SyscallID.SysCondSignalTo] = SysCondHandler.SysCondSignalTo,
                [SyscallID.SysCondWait] = SysCondHandler.SysCondWait,

                [SyscallID.SysPPUThreadSetPriority] = SysPPUThreadHandler.SysPPUThreadSetPriority,
                [SyscallID.SysPPUThreadGetPriority] = SysPPUThreadHandler.SysPPUThreadGetPriority,
                [SyscallID.SysPPUThreadGetStackInfo] = SysPPUThreadHandler.SysPPUThreadGetStackInfo,
                [SyscallID.SysPPUThreadJoin] = SysPPUThreadHandler.SysPPUThreadJoin,
                [SyscallID.SysPPUThreadYield] = SysPPUThreadHandler.SysPPUThreadYield,

                [SyscallID.SysProcessExit] = SysProcessHandler.SysProcessExit,
                [SyscallID.SysProcessExit2] = SysProcessHandler.SysProcessExit2,
                [SyscallID.SysProcessGetPID] = SysProcessHandler.SysProcessGetPID,
                [SyscallID.SysProcessGetSDKVersion] = SysProcessHandler.SysProcessGetSDKVersion,
                [SyscallID.SysProcessIsSPULockLineReservationAddress] = SysProcessHandler.SysProcessIsSPULockLineReservationAddress,

                [SyscallID.SysPRXGetModuleIDByName] = SysPRXHandler.SysPRXGetModuleIDByName,
                [SyscallID.SysPRXLoadModule] = SysPRXHandler.SysPRXLoadModule,

                [SyscallID.SysRWLockCreate] = SysRWLockHandler.SysRWLockCreate,
                [SyscallID.SysRWLockDestroy] = SysRWLockHandler.SysRWLockDestroy,
                [SyscallID.SysRWLockRLock] = SysRWLockHandler.SysRWLockRLock,
                [SyscallID.SysRWLockRUnlock] = SysRWLockHandler.SysRWLockRUnlock,
                [SyscallID.SysRWLockWLock] = SysRWLockHandler.SysRWLockWLock,
                [SyscallID.SysRWLockWUnlock] = SysRWLockHandler.SysRWLockWUnlock,
                [SyscallID.SysRWLockTryRLock] = SysRWLockHandler.SysRWLockTryRLock,
                [SyscallID.SysRWLockTryWLock] = SysRWLockHandler.SysRWLockTryWLock,

                [SyscallID.SysSemaphoreCreate] = SysSemaphoreHandler.SysSemaphoreCreate,
                [SyscallID.SysSemaphoreDestroy] = SysSemaphoreHandler.SysSemaphoreDestroy,
                [SyscallID.SysSemaphorePost] = SysSemaphoreHandler.SysSemaphorePost,
                [SyscallID.SysSemaphoreWait] = SysSemaphoreHandler.SysSemaphoreWait,
                [SyscallID.SysSemaphoreTryWait] = SysSemaphoreHandler.SysSemaphoreTryWait,

                [SyscallID.SysStorageOpen] = SysStorageHandler.SysStorageOpen,
                [SyscallID.SysStorageClose] = SysStorageHandler.SysStorageClose,
                [SyscallID.SysStorageRead] = SysStorageHandler.SysStorageRead,
                [SyscallID.SysStorageGetDeviceInfo] = SysStorageHandler.SysStorageGetDeviceInfo,

                [SyscallID.SysTimeGetCurrentTime] = SysTimeHandler.SysTimeGetCurrentTime,
                [SyscallID.SysTimeGetTimeZone] = SysTimeHandler.SysTimeGetTimeZone,
                [SyscallID.SysTimeGetTimebaseFrequency] = SysTimeHandler.SysTimeGetTimebaseFrequency,

                [SyscallID.SysSpuImageOpen] = SysSPUHandler.SysSpuImageOpen,
                [SyscallID.SysSpuInitialize] = SysSPUHandler.SysSpuInitialize,
                [SyscallID.SysSpuThreadGroupCreate] = SysSPUHandler.SysSpuThreadGroupCreate,
                [SyscallID.SysSpuThreadInitialize] = SysSPUHandler.SysSpuThreadInitialize,
                [SyscallID.SysSpuThreadGroupStart] = SysSPUHandler.SysSpuThreadGroupStart,
                [SyscallID.SysSpuThreadGroupJoin] = SysSPUHandler.SysSpuThreadGroupJoin,
                [SyscallID.SysSpuThreadReadLS] = SysSPUHandler.SysSpuThreadReadLS,
                [SyscallID.SysSpuThreadGroupConnectEvent] = SysSPUHandler.SysSpuThreadGroupConnectEvent,
                [SyscallID.SysSpuThreadWriteSpuMb] = SysSPUHandler.SysSpuThreadWriteSpuMb,
                [SyscallID.SysSpuThreadConnectEvent] = SysSPUHandler.SysSpuThreadConnectEvent,
                [SyscallID.SysSpuThreadGroupConnectEventAllThreads] = SysSPUHandler.SysSpuThreadGroupConnectEventAllThreads,

                [SyscallID.SysTimerSleep] = SysTimerHandler.SysTimerSleep,
                [SyscallID.SysTimerUSleep] = SysTimerHandler.SysTimerUSleep,

                [SyscallID.SysVmMemoryMap] = SysVmHandler.SysVmMemoryMap
            }.ToImmutableDictionary();

            return syscallTable;
        }

        private ImmutableDictionary<uint, Func<PPUThread, ulong>> CreateModuleTable()
        {
            // Create a module table (TODO: ID table or enum)
            ImmutableDictionary<uint, Func<PPUThread, ulong>> moduleTable = new Dictionary<uint, Func<PPUThread, ulong>>()
            {
                // SysPrxForUser
                [NIDManager.GetNID("sys_process_exit")] = SysPrxForUserHandler.SysProcessExit,
                [NIDManager.GetNID("sys_initialize_tls")] = SysPrxForUserHandler.SysInitializeTLS,
                [NIDManager.GetNID("sys_mmapper_allocate_memory")] = SysPrxForUserHandler.SysMmapperAllocateMemory,
                [NIDManager.GetNID("sys_mmapper_free_memory")] = SysPrxForUserHandler.SysMmapperFreeMemory,
                [NIDManager.GetNID("sys_mmapper_unmap_memory")] = SysPrxForUserHandler.SysMmapperUnmapMemory,
                [NIDManager.GetNID("sys_spinlock_initialize")] = SysPrxForUserHandler.SysSpinlockInitialize,
                [NIDManager.GetNID("sys_spinlock_lock")] = SysPrxForUserHandler.SysSpinlockLock,
                [NIDManager.GetNID("sys_spinlock_unlock")] = SysPrxForUserHandler.SysSpinlockUnlock,
                [NIDManager.GetNID("_sys_memset")] = SysPrxForUserHandler._Sys_Memset,
                [NIDManager.GetNID("_sys_strncpy")] = SysPrxForUserHandler._Sys_Strncpy,

                // SysLwMutex
                [NIDManager.GetNID("sys_lwmutex_create")] = SysLwMutexHandler.SysLWmutexCreate,
                [NIDManager.GetNID("sys_lwmutex_lock")] = SysLwMutexHandler.SysLWmutexLock,
                [NIDManager.GetNID("sys_lwmutex_trylock")] = SysLwMutexHandler.SysLWmutexTryLock,
                [NIDManager.GetNID("sys_lwmutex_unlock")] = SysLwMutexHandler.SysLWmutexUnlock,
                [NIDManager.GetNID("sys_lwmutex_destroy")] = SysLwMutexHandler.SysLWmutexDestroy,

                // SysLwCond
                [NIDManager.GetNID("sys_lwcond_create")] = SysLwCondHandler.SysLwCondCreate,
                [NIDManager.GetNID("sys_lwcond_wait")] = SysLwCondHandler.SysLwCondWait,
                [NIDManager.GetNID("sys_lwcond_signal")] = SysLwCondHandler.SysLwCondSignal,
                [NIDManager.GetNID("sys_lwcond_signal_all")] = SysLwCondHandler.SysLwCondSignalAll,
                [NIDManager.GetNID("sys_lwcond_destroy")] = SysLwCondHandler.SysLwCondDestroy,

                // SysPPUThread
                [NIDManager.GetNID("sys_ppu_thread_get_id")] = SysPPUThreadHandler.SysPPUThreadGetID,
                [NIDManager.GetNID("sys_ppu_thread_create")] = SysPPUThreadHandler.SysPPUThreadCreate,
                [NIDManager.GetNID("sys_ppu_thread_exit")] = SysPPUThreadHandler.SysPPUThreadExit,
                [NIDManager.GetNID("sys_ppu_thread_once")] = SysPPUThreadHandler.SysPPUThreadOnce,

                // Sys SPU
                [NIDManager.GetNID("sys_spu_image_import")] = SysSPUHandler.SysSpuImageImport,

                // SysTime
                [NIDManager.GetNID("sys_time_get_system_time")] = SysTimeHandler.SysTimeGetSystemTime,

                // SysPrxForUser 
                [NIDManager.GetNID("_sys_process_atexitspawn")] = SysPrxForUserHandler._Sys_Process_atexitspawn,
                [NIDManager.GetNID("_sys_process_at_Exitspawn")] = SysPrxForUserHandler._Sys_Process_at_Exitspawn,
                [NIDManager.GetNID("sys_game_process_exitspawn2")] = SysPrxForUserHandler.Sys_game_process_exitspawn2,
                [NIDManager.GetNID("sys_prx_exitspawn_with_level")] = SysPrxForUserHandler.Sys_Prx_Exitspawn_With_Level,

                // CellGcmSys
                [NIDManager.GetNID("_cellGcmInitBody")] = CellGcmSysHandler.CellGcmInitBody,
                [NIDManager.GetNID("cellGcmSetDefaultCommandBuffer")] = CellGcmSysHandler.CellGcmSetDefaultCommandBuffer,
                [NIDManager.GetNID("cellGcmGetConfiguration")] = CellGcmSysHandler.CellGcmGetConfiguration,
                [NIDManager.GetNID("cellGcmGetControlRegister")] = CellGcmSysHandler.CellGcmGetControlRegister,
                [NIDManager.GetNID("cellGcmAddressToOffset")] = CellGcmSysHandler.CellGcmAddressToOffset,
                [NIDManager.GetNID("cellGcmGetLabelAddress")] = CellGcmSysHandler.CellGcmGetLabelAddress,
                [NIDManager.GetNID("cellGcmSetFlipMode")] = CellGcmSysHandler.CellGcmSetFlipMode,
                [NIDManager.GetNID("cellGcmSetDisplayBuffer")] = CellGcmSysHandler.CellGcmSetDisplayBuffer,
                [NIDManager.GetNID("cellGcmResetFlipStatus")] = CellGcmSysHandler.CellGcmResetFlipStatus,

                [NIDManager.GetNID("cellGcmSetFlip")] = CellGcmSysHandler.CellGcmSetFlip,
                [NIDManager.GetNID("_cellGcmSetFlipCommand")] = CellGcmSysHandler.CellGcmSetFlip,

                [NIDManager.GetNID("cellGcmSetWaitFlip")] = CellGcmSysHandler.CellGcmSetWaitFlip,
                [NIDManager.GetNID("cellGcmGetFlipStatus")] = CellGcmSysHandler.CellGcmGetFlipStatus,
                [NIDManager.GetNID("cellGcmGetTiledPitchSize")] = CellGcmSysHandler.CellGcmGetTiledPitchSize,
                [NIDManager.GetNID("cellGcmSetTileInfo")] = CellGcmSysHandler.CellGcmSetTileInfo,
                [NIDManager.GetNID("cellGcmBindTile")] = CellGcmSysHandler.CellGcmBindTile,
                [NIDManager.GetNID("cellGcmSetTile")] = CellGcmSysHandler.CellGcmSetTile,
                [NIDManager.GetNID("cellGcmBindZcull")] = CellGcmSysHandler.CellGcmBindZcull,
                [NIDManager.GetNID("cellGcmSetDebugOutputLevel")] = CellGcmSysHandler.CellGcmSetDebugOutputLevel,
                [NIDManager.GetNID("cellGcmMapMainMemory")] = CellGcmSysHandler.CellGcmMapMainMemory,
                [NIDManager.GetNID("cellGcmMapEaIoAddress")] = CellGcmSysHandler.CellGcmMapEaIoAddress,
                [NIDManager.GetNID("cellGcmMapEaIoAddressWithFlags")] = CellGcmSysHandler.CellGcmMapEaIoAddressWithFlags,
                [NIDManager.GetNID("cellGcmGetOffsetTable")] = CellGcmSysHandler.CellGcmGetOffsetTable,
                [NIDManager.GetNID("cellGcmSetVBlankFrequency")] = CellGcmSysHandler.CellGcmSetVBlankFrequency,
                [NIDManager.GetNID("cellGcmSetSecondVFrequency")] = CellGcmSysHandler.CellGcmSetSecondVFrequency,
                [NIDManager.GetNID("cellGcmSetVBlankHandler")] = CellGcmSysHandler.CellGcmSetVBlankHandler,
                [NIDManager.GetNID("cellGcmSetFlipHandler")] = CellGcmSysHandler.CellGcmSetFlipHandler,
                [NIDManager.GetNID("cellGcmGetLastFlipTime")] = CellGcmSysHandler.CellGcmGetLastFlipTime,
                [NIDManager.GetNID("cellGcmGetLastSecondVTime")] = CellGcmSysHandler.CellGcmGetLastSecondVTime,
                [NIDManager.GetNID("cellGcmGetDefaultCommandWordSize")] = CellGcmSysHandler.CellGcmGetDefaultCommandWordSize,
                [NIDManager.GetNID("cellGcmGetDefaultSegmentWordSize")] = CellGcmSysHandler.CellGcmGetDefaultSegmentWordSize,

                // CellAudio
                [NIDManager.GetNID("cellAudioInit")] = CellAudioHandler.CellAudioInit,
                [NIDManager.GetNID("cellAudioPortOpen")] = CellAudioHandler.CellAudioPortOpen,
                [NIDManager.GetNID("cellAudioGetPortConfig")] = CellAudioHandler.CellAudioGetPortConfig,
                [NIDManager.GetNID("cellAudioPortStart")] = CellAudioHandler.CellAudioPortStart,
                [NIDManager.GetNID("cellAudioPortStop")] = CellAudioHandler.CellAudioPortStop,
                [NIDManager.GetNID("cellAudioQuit")] = CellAudioHandler.CellAudioQuit,
                [NIDManager.GetNID("cellAudioCreateNotifyEventQueue")] = CellAudioHandler.CellAudioCreateNotifyEventQueue,
                [NIDManager.GetNID("cellAudioSetNotifyEventQueue")] = CellAudioHandler.CellAudioSetNotifyEventQueue,

                // CellVideoOut
                [NIDManager.GetNID("cellVideoOutGetResolutionAvailability")] = CellVideoOutHandler.CellVideoOutGetResolutionAvailability,
                [NIDManager.GetNID("cellVideoOutConfigure")] = CellVideoOutHandler.CellVideoOutConfigure,
                [NIDManager.GetNID("cellVideoOutGetResolution")] = CellVideoOutHandler.CellVideoOutGetResolution,
                [NIDManager.GetNID("cellVideoOutGetState")] = CellVideoOutHandler.CellVideoOutGetState,
                [NIDManager.GetNID("cellVideoOutGetNumberOfDevice")] = CellVideoOutHandler.CellVideoOutGetNumberOfDevice,
                [NIDManager.GetNID("cellVideoOutGetDeviceInfo")] = CellVideoOutHandler.CellVideoOutGetDeviceInfo,

                // SysUtil / AudioOut / SaveData / MsgDialog
                [NIDManager.GetNID("cellSysutilRegisterCallback")] = SysUtilHandler.CellSysutilRegisterCallback,
                [NIDManager.GetNID("cellSysutilUnregisterCallback")] = SysUtilHandler.CellSysutilUnregisterCallback,
                [NIDManager.GetNID("cellSysutilCheckCallback")] = SysUtilHandler.CellSysutilCheckCallback,

                [NIDManager.GetNID("cellAudioOutGetSoundAvailability")] = SysUtilHandler.CellAudioOutGetSoundAvailability,
                [NIDManager.GetNID("cellAudioOutConfigure")] = SysUtilHandler.CellAudioOutConfigure,
                [NIDManager.GetNID("cellAudioOutGetState")] = SysUtilHandler.CellAudioOutGetState,

                [NIDManager.GetNID("cellSysutilGetSystemParamInt")] = SysUtilHandler.CellSysutilGetSystemParamInt,
                [NIDManager.GetNID("cellSysutilGetSystemParamString")] = SysUtilHandler.CellSysutilGetSystemParamString,

                [NIDManager.GetNID("cellSaveDataUserAutoLoad")] = SysUtilHandler.CellSaveDataUserAutoLoad,
                [NIDManager.GetNID("cellSaveDataAutoLoad2")] = SysUtilHandler.CellSaveDataAutoLoad2,
                [NIDManager.GetNID("cellSaveDataAutoSave2")] = SysUtilHandler.CellSaveDataAutoSave2,

                [NIDManager.GetNID("cellMsgDialogOpen2")] = SysUtilHandler.CellMsgDialogOpen2,
                [NIDManager.GetNID("cellMsgDialogProgressBarSetMsg")] = SysUtilHandler.CellMsgDialogProgressBarSetMsg,
                [NIDManager.GetNID("cellMsgDialogProgressBarInc")] = SysUtilHandler.CellMsgDialogProgressBarInc,
                [NIDManager.GetNID("cellMsgDialogClose")] = SysUtilHandler.CellMsgDialogClose,

                [NIDManager.GetNID("cellSaveDataEnableOverlay")] = SysUtilHandler.CellSaveDataEnableOverlay,

                // CellSysModule
                [NIDManager.GetNID("cellSysmoduleLoadModule")] = CellSysModuleHandler.CellSysmoduleLoadModule,
                [NIDManager.GetNID("cellSysmoduleUnloadModule")] = CellSysModuleHandler.CellSysmoduleUnloadModule,
                [NIDManager.GetNID("cellSysmoduleIsLoaded")] = CellSysModuleHandler.CellSysmoduleIsLoaded,

                // CellPngDec
                [NIDManager.GetNID("cellPngDecCreate")] = CellPngDecHandler.CellPngDecCreate,
                [NIDManager.GetNID("cellPngDecOpen")] = CellPngDecHandler.CellPngDecOpen,
                [NIDManager.GetNID("cellPngDecReadHeader")] = CellPngDecHandler.CellPngDecReadHeader,
                [NIDManager.GetNID("cellPngDecSetParameter")] = CellPngDecHandler.CellPngDecSetParameter,
                [NIDManager.GetNID("cellPngDecDecodeData")] = CellPngDecHandler.CellPngDecDecodeData,
                [NIDManager.GetNID("cellPngDecClose")] = CellPngDecHandler.CellPngDecClose,
                [NIDManager.GetNID("cellPngDecDestroy")] = CellPngDecHandler.CellPngDecDestroy,

                // CellResc
                [NIDManager.GetNID("cellRescInit")] = CellRescHandler.CellRescInit,
                [NIDManager.GetNID("cellRescVideoOutResolutionId2RescBufferMode")] = CellRescHandler.CellRescVideoOutResolutionId2RescBufferMode,
                [NIDManager.GetNID("cellRescSetDsts")] = CellRescHandler.CellRescSetDsts,
                [NIDManager.GetNID("cellRescSetDisplayMode")] = CellRescHandler.CellRescSetDisplayMode,
                [NIDManager.GetNID("cellRescGetBufferSize")] = CellRescHandler.CellRescGetBufferSize,
                [NIDManager.GetNID("cellRescSetBufferAddress")] = CellRescHandler.CellRescSetBufferAddress,
                [NIDManager.GetNID("cellRescGcmSurface2RescSrc")] = CellRescHandler.CellRescGcmSurface2RescSrc,
                [NIDManager.GetNID("cellRescSetSrc")] = CellRescHandler.CellRescSetSrc,
                [NIDManager.GetNID("cellRescGetFlipStatus")] = CellRescHandler.CellRescGetFlipStatus,
                [NIDManager.GetNID("cellRescResetFlipStatus")] = CellRescHandler.CellRescResetFlipStatus,
                [NIDManager.GetNID("cellRescSetConvertAndFlip")] = CellRescHandler.CellRescSetConvertAndFlip,
                [NIDManager.GetNID("cellRescSetWaitFlip")] = CellRescHandler.CellRescSetWaitFlip,

                // CellGame
                [NIDManager.GetNID("cellGameBootCheck")] = CellGameHandler.CellGameBootCheck,
                [NIDManager.GetNID("cellGameContentPermit")] = CellGameHandler.CellGameContentPermit,
                [NIDManager.GetNID("cellGameGetParamInt")] = CellGameHandler.CellGameGetParamInt,
                [NIDManager.GetNID("cellGameGetParamString")] = CellGameHandler.CellGameGetParamString,
                [NIDManager.GetNID("cellGameDataCheck")] = CellGameHandler.CellGameDataCheck,
                [NIDManager.GetNID("cellGameGetDiscContentInfoUpdatePath")] = CellGameHandler.CellGameGetDiscContentInfoUpdatePath,
                [NIDManager.GetNID("cellGamePatchCheck")] = CellGameHandler.CellGamePatchCheck,

                // CellRtc
                [NIDManager.GetNID("cellRtcGetCurrentTick")] = CellRtcHandler.CellRtcGetCurrentTick,
                [NIDManager.GetNID("cellRtcGetCurrentClockLocalTime")] = CellRtcHandler.CellRtcGetCurrentClockLocalTime,

                // CellFs
                [NIDManager.GetNID("cellFsAioInit")] = CellFsHandler.CellFsAioInit,
                [NIDManager.GetNID("cellFsStat")] = CellFsHandler.CellFsStat,
                [NIDManager.GetNID("cellFsFstat")] = CellFsHandler.CellFsFstat,
                [NIDManager.GetNID("cellFsOpen")] = CellFsHandler.CellFsOpen,
                [NIDManager.GetNID("cellFsLseek")] = CellFsHandler.CellFsLseek,
                [NIDManager.GetNID("cellFsRead")] = CellFsHandler.CellFsRead,
                [NIDManager.GetNID("cellFsWrite")] = CellFsHandler.CellFsWrite,
                [NIDManager.GetNID("cellFsClose")] = CellFsHandler.CellFsClose,
                [NIDManager.GetNID("cellFsMkdir")] = CellFsHandler.CellFsMakedir,
                [NIDManager.GetNID("cellFsReaddir")] = CellFsHandler.CellFsReaddir,
                [NIDManager.GetNID("cellFsOpendir")] = CellFsHandler.CellFsOpenDir,
                [NIDManager.GetNID("cellFsClosedir")] = CellFsHandler.CellFsCloseDir,
                [NIDManager.GetNID("cellFsGetFreeSize")] = CellFsHandler.CellFsGetFreeSize,

                // SCENpTrophy
                [NIDManager.GetNID("sceNpTrophyInit")] = SceNPTrophyHandler.SCENpTrophyInit,
                [NIDManager.GetNID("sceNpTrophyCreateContext")] = SceNPTrophyHandler.SCENpTrophyCreateContext,
                [NIDManager.GetNID("sceNpTrophyCreateHandle")] = SceNPTrophyHandler.SCENpTrophyCreateHandle,
                [NIDManager.GetNID("sceNpTrophyGetRequiredDiskSpace")] = SceNPTrophyHandler.SCENpTrophyGetRequiredDiskSpace,
                [NIDManager.GetNID("sceNpTrophyRegisterContext")] = SceNPTrophyHandler.SCENpTrophyRegisterContext,
                [NIDManager.GetNID("sceNpTrophyGetGameInfo")] = SceNPTrophyHandler.SCENpTrophyGetGameInfo,
                [NIDManager.GetNID("sceNpTrophyGetTrophyUnlockState")] = SceNPTrophyHandler.SCENpTrophyGetTrophyUnlockState,
                [NIDManager.GetNID("sceNpTrophyUnlockTrophy")] = SceNPTrophyHandler.SCENpTrophyUnlockTrophy,

                // CellPad
                [NIDManager.GetNID("cellPadInit")] = CellPadHandler.CellPadInit,
                [NIDManager.GetNID("cellPadSetPortSetting")] = CellPadHandler.CellPadSetPortSetting,
                [NIDManager.GetNID("cellPadGetInfo2")] = CellPadHandler.CellPadGetInfo2,
                [NIDManager.GetNID("cellPadGetData")] = CellPadHandler.CellPadGetData,
                [NIDManager.GetNID("cellPadGetInfo")] = CellPadHandler.CellPadGetInfo,

                // SCENP
                [NIDManager.GetNID("sceNpInit")] = SceNPHandler.SCENpInit,
                [NIDManager.GetNID("sceNpScoreInit")] = SceNPHandler.SCENpScoreInit,
                [NIDManager.GetNID("sceNpManagerGetContentRatingFlag")] = SceNPHandler.SCENpManagerGetContentRatingFlag,
                [NIDManager.GetNID("sceNpBasicGetEvent")] = SceNPHandler.SCENpBasicGetEvent,
                [NIDManager.GetNID("sceNpManagerGetNpId")] = SceNPHandler.SCENpManagerGetNpId,
                [NIDManager.GetNID("sceNpManagerGetStatus")] = SceNPHandler.SCENpManagerGetStatus,
                [NIDManager.GetNID("sceNpScoreTerm")] = SceNPHandler.SCENpScoreTerm,
                [NIDManager.GetNID("sceNpDrmIsAvailable")] = SceNPHandler.SCENpDrmIsAvailable,
                [NIDManager.GetNID("sceNpBasicRegisterContextSensitiveHandler")] = SceNPHandler.SCENpBasicRegisterContextSensitiveHandler,
                [NIDManager.GetNID("sceNpManagerRegisterCallback")] = SceNPHandler.SCENpManagerRegisterCallback,

                // CellNetCtl
                [NIDManager.GetNID("cellNetCtlGetState")] = CellNetCtlHandler.CellNetCtlGetState,

                // CellIO
                [NIDManager.GetNID("cellMouseGetInfo")] = CellIOHandler.CellMouseGetInfo,
                [NIDManager.GetNID("cellPadPeriphGetInfo")] = CellIOHandler.CellPadPeriphGetInfo,
            }.ToImmutableDictionary();

            return moduleTable;
        }
    }
}
