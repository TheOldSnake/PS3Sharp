using PS3Sharp.OS.Modules;

namespace PS3Sharp.OS
{
    internal partial class LV2SyscallDispatcher
    {
        public enum SyscallID : uint
        {
            SysProcessGetPID = 0x001,
            SysProcessExit = 0x003,
            SysProcessExit2 = 0x016,
            SysProcessIsSPULockLineReservationAddress = 0x00E,

            SysProcessGetSDKVersion = 0x019,

            SysPPUThreadYield = 0x02B,
            SysPPUThreadJoin = 0x02C,
            SysPPUThreadSetPriority = 0x02F,
            SysPPUThreadGetPriority = 0x030,
            SysPPUThreadGetStackInfo = 0x031,

            // sys_event_flag
            SysEventFlagCreate = 0x052,
            SysEventFlagDestroy = 0x053,
            SysEventFlagWait = 0x055,
            SysEventFlagTryWait = 0x056,
            SysEventFlagSet = 0x057,
            SysEventFlagClear = 0x076,

            // sys_semaphore
            SysSemaphoreCreate = 0x05A,
            SysSemaphoreDestroy = 0x05B,
            SysSemaphoreWait = 0x05C,
            SysSemaphoreTryWait = 0x05D,
            SysSemaphorePost = 0x05E,

            // sys_lwmutex
            SysLWMutexCreate = 0x05F,
            SysLWMutexDestroy = 0x060,
            SysLWMutexLock = 0x061,
            SysLWMutexTryLock = 0x062,
            SysLWMutexUnlock = 0x063,

            // sys_mutex
            SysMutexCreate = 0x064,
            SysMutexDestroy = 0x065,
            SysMutexLock = 0x066,
            SysMutexTryLock = 0x067,
            SysMutexUnlock = 0x068,

            // sys_cond
            SysCondCreate = 0x069,
            SysCondDestroy = 0x06A,
            SysCondWait = 0x06B,
            SysCondSignal = 0x06C,
            SysCondSignalAll = 0x06D,
            SysCondSignalTo = 0x06E,

            // sys_lwcond
            SysLWCondCreate = 0x06F,
            SysLWCondDestroy = 0x070,
            SysLWCondQueuqWait = 0x071,

            // sys_rwlock
            SysRWLockCreate = 0x078,
            SysRWLockDestroy = 0x079,
            SysRWLockRLock = 0x07A,
            SysRWLockTryRLock = 0x07B,
            SysRWLockRUnlock = 0x07C,
            SysRWLockWLock = 0x07D,
            SysRWLockTryWLock = 0x07E,
            SysRWLockWUnlock = 0x07F,

            // sys_event
            SysEventQueueCreate = 0x080,
            SysEventQueueDestroy = 0x081,
            SysEventQueueReceive = 0x082,
            SysEventQueueTryReceive = 0x083,
            SysEventFlagCancel = 0x084,
            SysEventQueueDrain = 0x085,
            SysEventPortCreate = 0x086,
            SysEventPortDestroy = 0x087,
            SysEventPortConnectLocal = 0x088,
            SysEventPortDisconnect = 0x089,
            SysEventPortSend = 0x08A,
            SysEventFlagGet = 0x08B,
            SysEventPortConnectIPC = 0x08C,

            // sys_timer
            SysTimerUSleep = 0x08D,
            SysTimerSleep = 0x08E,

            // sys_time
            SysTimeSetTimeZone = 0x08F,
            SysTimeGetTimeZone = 0x090,
            SysTimeGetCurrentTime = 0x091,
            SysTimeGetTimebaseFrequency = 0x093,

            // sys_spu
            SysSpuInitialize = 0x0A9,
            SysSpuThreadGroupCreate = 0x0AA,
            SysSpuThreadInitialize = 0x0AC,
            SysSpuThreadGroupStart = 0x0AD,
            SysSpuThreadReadLS = 0x0B6,
            SysSpuThreadGroupConnectEvent = 0x0B9,
            SysSpuThreadWriteSPUMB = 0x0BE,
            SysSpuThreadGroupConnectEventAllThreads = 0x0FB,

            // sys_vm
            SysVmMemoryMap = 0x12C,

            // memory / mapper
            SysMMapperAllocateAddress = 0x14A,
            SysMMapperFreeAddress = 0x14B,
            SysMMapperAllocateMemory = 0x14C,
            SysMMapperSearchAndMap = 0x151,
            SysMemoryAllocate = 0x15C,
            SysMemoryFree = 0x15D,
            SysMemoryGetUserMemorySize = 0x160,

            // tty
            SysTTYWrite = 0x193,

            // sys_prx
            SysPRXLoadModule = 0x1E0,
            SysPRXGetModuleIDByName = 0x1F0,

            // sys_storage
            SysStorageOpen = 0x258,
            SysStorageClose = 0x259,
            SysStorageRead = 0x25A,
            SysStorageGetDeviceInfo = 0x261,

            // sys_fs
            SysFsOpen = 0x321,
            SysFsRead = 0x322,
            SysFsWrite = 0x323,
            SysFsClose = 0x324,
            SysFsFstat = 0x329,
            SysFsLSeek = 0x332,

            // debug
            SysDbgPpuExceptionHandler = 0x3DC,
            CellGCMCallback = CellGcmSys.RSXCallbackSysID,
            ModuleCall = ModulePatcher.ModuleCallSysId
        }
    }
}
