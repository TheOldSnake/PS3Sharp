using OpenTK.Graphics.ES11;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Modules;
using PS3Sharp.OS.Syscall;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.Syscall
{
    public static class SyscallHandler
    {
        // Constants

        private const uint SysProcessGetPID = 0x001;
        private const uint SysProcessExit = 0x003;
        private const uint SysProcessExit2 = 0x016;
        private const uint SysProcessIsSPULockLineReservationAddress = 0x00E;

        private const uint SysProcessGetSDKVersion = 0x019;

        private const uint SysPPUThreadYield = 0x02B;
        private const uint SysPPUThreadJoin = 0x02C;
        private const uint SysPPUThreadSetPriority = 0x02F;
        private const uint SysPPUThreadGetPriority = 0x030;
        private const uint SysPPUThreadGetStackInfo = 0x031;


        // sys_event_flag
        private const uint SysEventFlagCreate = 0x052;
        private const uint SysEventFlagDestroy = 0x053;
        private const uint SysEventFlagWait = 0x055;
        private const uint SysEventFlagTryWait = 0x056;
        private const uint SysEventFlagSet = 0x057;
        private const uint SysEventFlagClear = 0x076;


        // sys_semaphore
        private const uint SysSemaphoreCreate = 0x05A;
        private const uint SysSemaphoreDestroy = 0x05B;
        private const uint SysSemaphoreWait = 0x05C;
        private const uint SysSemaphoreTryWait = 0x05D;
        private const uint SysSemaphorePost = 0x05E;

        // sys_lwmutex
        private const uint SysLWMutexCreate = 0x05F;
        private const uint SysLWMutexDestroy = 0x060;
        private const uint SysLWMutexLock = 0x061;
        private const uint SysLWMutexTryLock = 0x062;
        private const uint SysLWMutexUnlock = 0x063;

        // sys_mutex
        private const uint SysMutexCreate = 0x064;
        private const uint SysMutexDestroy = 0x065;
        private const uint SysMutexLock = 0x066;
        private const uint SysMutexTryLock = 0x067;
        private const uint SysMutexUnlock = 0x068;

        // sys_cond
        private const uint SysCondCreate = 0x069;
        private const uint SysCondDestroy = 0x06A;
        private const uint SysCondWait = 0x06B;
        private const uint SysCondSignal = 0x06C;
        private const uint SysCondSignalAll = 0x06D;
        private const uint SysCondSignalTo = 0x06E;

        // sys_lwcond
        private const uint SysLWCondCreate = 0x06F;
        private const uint SysLWCondDestroy = 0x070;
        private const uint SysLWCondQueuqWait = 0x071;

        // sys_rwlock
        private const uint SysRWLockCreate = 0x078;
        private const uint SysRWLockDestroy = 0x079;
        private const uint SysRWLockRLock = 0x07A;
        private const uint SysRWLockTryRLock = 0x07B;
        private const uint SysRWLockRUnlock = 0x07C;
        private const uint SysRWLockWLock = 0x07D;
        private const uint SysRWLockTryWLock = 0x07E;
        private const uint SysRWLockWUnlock = 0x07F;

        // sys_event
        private const uint SysEventQueueCreate = 0x080;
        private const uint SysEventQueueDestroy = 0x081;
        private const uint SysEventQueueReceive = 0x082;
        private const uint SysEventQueueTryReceive = 0x083;
        private const uint SysEventFlagCancel = 0x084;
        private const uint SysEventQueueDrain = 0x085;
        private const uint SysEventPortCreate = 0x086;
        private const uint SysEventPortDestroy = 0x087;
        private const uint SysEventPortConnectLocal = 0x088;
        private const uint SysEventPortDisconnect = 0x089;
        private const uint SysEventPortSend = 0x08A;
        private const uint SysEventFlagGet = 0x08B;
        private const uint SysEventPortConnectIPC = 0x08C;

        // sys_timer
        private const uint SysTimerUSleep = 0x08D;
        private const uint SysTimerSleep = 0x08E;

        // sys_time
        private const uint SysTimeSetTimeZone = 0x08F;
        private const uint SysTimeGetTimeZone = 0x090;
        private const uint SysTimeGetCurrentTime = 0x091;
        private const uint SysTimeGetTimebaseFrequency = 0x093;

        // sys_spu
        private const uint SysSPUInitialize = 0x0A9;
        private const uint SysSPUThreadGroupCreate = 0xAA;
        private const uint SysSPUThreadInitialize = 0xAC;
        private const uint SysSPUThreadReadLS = 0x0B6;
        private const uint SysSPUThreadWriteSPUMB = 0x0BE;
        private const uint SysSpuThreadGroupConnectEventAllThreads = 0x0FB;

        // sys_vm
        private const uint SysVmMemoryMap = 0x12C;

        // memory / mapper
        private const uint SysMMapperAllocateAddress = 0x14A;
        private const uint SysMMapperFreeAddress = 0x14B;
        private const uint SysMMapperAllocateMemory = 0x14C;
        private const uint SysMMapperSearchAndMap = 0x151;
        private const uint SysMemoryAllocate = 0x15C;
        private const uint SysMemoryFree = 0x15D;
        private const uint SysMemoryGetUserMemorySize = 0x160;

        // tty
        private const uint SysTTYWrite = 0x193;

        // sys_prx
        private const uint SysPRXLoadModule = 0x1E0;
        private const uint SysPRXGetModuleIDByName = 0x1F0;

        // sys_storage
        private const uint SysStorageOpen = 0x258;
        private const uint SysStorageClose = 0x259;
        private const uint SysStorageRead = 0x25A;
        private const uint SysStorageGetDeviceInfo = 0x261;


        // sys_fs
        private const uint SysFsOpen = 0x321;
        private const uint SysFsRead = 0x322;
        private const uint SysFsWrite = 0x323;
        private const uint SysFsClose = 0x324;
        private const uint SysFsFstat = 0x329;
        private const uint SysFsLSeek = 0x332;

        // debug
        private const uint SysDbgPpuExceptionHandler = 0x3DC;

        private static readonly Func<PPU, ulong>[] SyscallTable = new Func<PPU, ulong>[0x1000];
        const int SYSCALL_ID_REG = 11;      //r11 contains syscall number

        static SyscallHandler()
        {
            //Set up syscall table
            Array.Fill(SyscallTable, Unknown);
        }

        public static void HandleSyscall(PPU ppu, uint lev)
        {
            uint sys_id = ppu.ReadArg<uint>(SYSCALL_ID_REG);

            /*if (ppu.IsMainThread) {
                Console.WriteLine($"Syscall: 0x{sys_id:X3} lr = 0x{ppu.GetLR():X8}");
            }*/

            //if (sys_id != ModulePatcher.ModuleCallSysID) {
            //    Console.WriteLine($"[{ppu.ThreadName}] Syscall: 0x{sys_id:X3}");
            //}

            // Ignore SPU syscalls
            //if (sys_id >= 0x96 && sys_id <= 0x104) {
            //    ppu.Return(CELL_OK);
            //    Console.WriteLine($"[{ppu.ThreadName}] Ignored SPU syscall: 0x{sys_id:X3}");
            //    return;
            //}

            // Module call
            if (sys_id == ModulePatcher.ModuleCallSysID)
            {
                ModuleHandler.HandleModuleCall(ppu);
                return;
            }

            ulong result;
            switch (sys_id)
            {

                // RSX Callback
                case CellGcmSys.RSXCallbackSysID:
                    result = CellGcmSys.CellGcmCallback(ppu);
                    break;

                case SysProcessGetPID: result = SysProcess.SysProcessGetPID(ppu); break;
                case SysProcessExit: result = SysProcess.SysProcessExit(ppu); break;
                case SysProcessExit2: result = SysProcess.SysProcessExit2(ppu); break;
                case SysProcessIsSPULockLineReservationAddress: result = SysProcess.SysProcessIsSPULockLineReservationAddress(ppu); break;
                case SysProcessGetSDKVersion: result = SysProcess.SysProcessGetSDKVersion(ppu); break;

                case SysPPUThreadYield: result = SysPPUThread.SysPPUThreadYield(ppu); break;
                case SysPPUThreadJoin: result = SysPPUThread.SysPPUThreadJoin(ppu); break;
                case SysPPUThreadSetPriority: result = SysPPUThread.SysPPUThreadSetPriority(ppu); break;
                case SysPPUThreadGetPriority: result = SysPPUThread.SysPPUThreadGetPriority(ppu); break;
                case SysPPUThreadGetStackInfo: result = SysPPUThread.SysPPUThreadGetStackInfo(ppu); break;

                // sys_event_flag
                case SysEventFlagCreate: result = SysEventFlag.SysEventFlagCreate(ppu); break;
                case SysEventFlagDestroy: result = SysEventFlag.SysEventFlagDestroy(ppu); break;
                case SysEventFlagWait: result = SysEventFlag.SysEventFlagWait(ppu); break;
                case SysEventFlagSet: result = SysEventFlag.SysEventFlagSet(ppu); break;
                case SysEventFlagClear: result = SysEventFlag.SysEventFlagClear(ppu); break;


                // sys_semaphore
                case SysSemaphoreCreate: result = SysSemaphore.SysSemaphoreCreate(ppu); break;
                case SysSemaphoreDestroy: result = SysSemaphore.SysSemaphoreDestroy(ppu); break;
                case SysSemaphoreWait: result = SysSemaphore.SysSemaphoreWait(ppu); break;
                case SysSemaphoreTryWait: result = SysSemaphore.SysSemaphoreTryWait(ppu); break;
                case SysSemaphorePost: result = SysSemaphore.SysSemaphorePost(ppu); break;

                // _sys_lwmutex
                case SysLWMutexCreate: result = _SysLWMutex._SysLWmutexCreate(ppu); break;
                case SysLWMutexDestroy: result = _SysLWMutex._SysLWmutexDestroy(ppu); break;
                case SysLWMutexLock: result = _SysLWMutex._SysLWmutexLock(ppu); break;
                case SysLWMutexTryLock: result = _SysLWMutex._SysLWmutexTryLock(ppu); break;
                case SysLWMutexUnlock: result = _SysLWMutex._SysLWmutexUnlock(ppu); break;

                // sys_mutex
                case SysMutexCreate: result = SysMutex.SysMutexCreate(ppu); break;
                case SysMutexDestroy: result = SysMutex.SysMutexDestroy(ppu); break;
                case SysMutexLock: result = SysMutex.SysMutexLock(ppu); break;
                case SysMutexTryLock: result = SysMutex.SysMutexTryLock(ppu); break;
                case SysMutexUnlock: result = SysMutex.SysMutexUnlock(ppu); break;

                // sys_cond
                case SysCondCreate: result = SysCond.SysCondCreate(ppu); break;
                case SysCondDestroy: result = SysCond.SysCondDestroy(ppu); break;
                case SysCondWait: result = SysCond.SysCondWait(ppu); break;
                case SysCondSignal: result = SysCond.SysCondSignal(ppu); break;
                case SysCondSignalAll: result = SysCond.SysCondSignalAll(ppu); break;
                case SysCondSignalTo: result = SysCond.SysCondSignalTo(ppu); break;

                // sys_lwcond
                // case SysLWCondCreate: result = SysLWCond.SysLWCondCreate(ppu); break;

                case SysRWLockCreate: result = SysRWLock.SysRWLockCreate(ppu); break;
                case SysRWLockDestroy: result = SysRWLock.SysRWLockDestroy(ppu); break;
                case SysRWLockRLock: result = SysRWLock.SysRWLockRLock(ppu); break;
                case SysRWLockTryRLock: result = SysRWLock.SysRWLockTryRLock(ppu); break;
                case SysRWLockRUnlock: result = SysRWLock.SysRWLockRUnlock(ppu); break;
                case SysRWLockWLock: result = SysRWLock.SysRWLockWLock(ppu); break;
                case SysRWLockTryWLock: result = SysRWLock.SysRWLockTryWLock(ppu); break;
                case SysRWLockWUnlock: result = SysRWLock.SysRWLockWUnlock(ppu); break;

                // sys_event
                case SysEventQueueCreate: result = SysEvent.SysEventQueueCreate(ppu); break;
                case SysEventQueueDestroy: result = SysEvent.SysEventQueueDestroy(ppu); break;
                case SysEventQueueReceive: result = SysEvent.SysEventQueueReceive(ppu); break;
                case SysEventQueueTryReceive: result = SysEvent.SysEventQueueTryReceive(ppu); break;
                case SysEventFlagCancel: result = Syscallstub("sys_event_flag_cancel"); break;
                case SysEventQueueDrain: result = SysEvent.SysEventQueueDrain(ppu); break;
                case SysEventPortCreate: result = SysEvent.SysEventPortCreate(ppu); break;
                case SysEventPortDestroy: result = SysEvent.SysEventPortDestroy(ppu); break;
                case SysEventPortConnectLocal: result = SysEvent.SysEventPortConnectLocal(ppu); break;
                case SysEventPortDisconnect: result = SysEvent.SysEventPortDisconnect(ppu); break;
                case SysEventPortSend: result = SysEvent.SysEventPortSend(ppu); break;
                case SysEventFlagGet: result = Syscallstub("sys_event_flag_get"); break;
                case SysEventPortConnectIPC: result = SysEvent.SysEventPortConnectIPC(ppu); break;

                // sys_timer
                case SysTimerUSleep: result = SysTimer.SysTimerUSleep(ppu); break;
                case SysTimerSleep: result = SysTimer.SysTimerSleep(ppu); break;

                // sys_time
                case SysTimeGetTimeZone: result = SysTime.SysTimeGetTimeZone(ppu); break;
                case SysTimeGetCurrentTime: result = SysTime.SysTimeGetCurrentTime(ppu); break;
                case SysTimeGetTimebaseFrequency: result = SysTime.SysTimeGetTimebaseFrequency(ppu); break;

                // sys_spu
                case SysSPUInitialize: result = SysSPU.SysSPUInitialize(ppu); break;
                case SysSPUThreadGroupCreate: result = SysSPU.SysSPUThreadGroupCreate(ppu); break;
                case SysSPUThreadInitialize: result = SysSPU.SysSPUThreadInitialize(ppu); break;
                case SysSpuThreadGroupConnectEventAllThreads: result = SysSPU.SysSPUThreadGroupConnectEventAllThreads(ppu); break;



                // sys_vm
                case SysVmMemoryMap: result = SysVm.SysVmMemoryMap(ppu); break;

                // memory / mapper
                case SysMMapperAllocateAddress: result = SysMMapper.SysMMapperAllocateAddress(ppu); break;
                case SysMMapperFreeAddress: result = SysMMapper.SysMMapperFreeAddress(ppu); break;
                case SysMMapperAllocateMemory: result = SysMMapper.SysMMapperAllocateMemory(ppu); break;
                case SysMMapperSearchAndMap: result = SysMMapper.SysMMapperSearchAndMap(ppu); break;
                case SysMemoryAllocate: result = SysMemory.SysMemoryAllocate(ppu); break;
                case SysMemoryFree: result = SysMemory.SysMemoryFree(ppu); break;
                case SysMemoryGetUserMemorySize: result = SysMemory.SysMemoryGetUserMemorySize(ppu); break;

                // sys_tty
                case SysTTYWrite: result = SysTTY.SysTTYWrite(ppu); break;

                // sys_prx
                case SysPRXLoadModule: result = SysPRX.SysPRXLoadModule(ppu); break;
                case SysPRXGetModuleIDByName: result = SysPRX.SysPRXGetModuleIDByName(ppu); break;

                //sys_storage
                case SysStorageOpen: result = SysStorage.SysStorageOpen(ppu); break;
                case SysStorageClose: result = SysStorage.SysStorageClose(ppu); break;
                case SysStorageRead: result = SysStorage.SysStorageRead(ppu); break;
                case SysStorageGetDeviceInfo: result = SysStorage.SysStorageGetDeviceInfo(ppu); break;

                // sys_fs
                case SysFsOpen: result = SysFs.SysFsOpen(ppu); break;
                case SysFsRead: result = SysFs.SysFsRead(ppu); break;
                case SysFsWrite: result = SysFs.SysFsWrite(ppu); break;
                case SysFsClose: result = SysFs.SysFsClose(ppu); break;
                case SysFsFstat: result = SysFs.SysFsFstat(ppu); break;
                case SysFsLSeek: result = SysFs.SysFsLseek(ppu); break;

                case SysLWCondCreate: result = Syscallstub("SysLWCondCreate"); break;
                case SysLWCondDestroy: result = Syscallstub("SysLWCondDestroy"); break;
                case SysLWCondQueuqWait: result = Syscallstub("SysLWCondQueuqWait"); break;
                case SysDbgPpuExceptionHandler: result = Syscallstub("sys_dbg_(something)_ppu_exception_handler"); break;

                default:

                    Console.WriteLine($"[{ppu.ThreadName}] unhandled syscall : 0x{sys_id:X3} LEV = 0x{lev:X8}");
                    result = lev == 1 ? lev : CELL_OK;
                     break;
                    throw new Exception($"unhandled syscall : 0x{sys_id:X3} LEV = 0x{lev:X8}");
            }

            ppu.Return(result);

        }

        public static ulong Syscallstub(string name)
        {
            Console.WriteLine($"unimplemented syscall : {name}");
            return CELL_OK;
        }

        public static ulong Syscallstub(string name, ulong ret)
        {
            Console.WriteLine(name);
            return ret;
        }

        public static ulong Unknown(PPU ppu)
        {
            uint sys_id = ppu.ReadArg<uint>(SYSCALL_ID_REG);
            throw new Exception($"unhandled syscall : 0x{sys_id:X3}");
        }
    }
}
