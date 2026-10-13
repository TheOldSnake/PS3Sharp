using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using PS3Sharp.OS.Modules;
using PS3Sharp.OS.Syscall;
using PS3Sharp.Syscall;
using System.Collections.Immutable;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS
{
    internal partial class LV2SyscallDispatcher
    {
        private const int SYSCALL_ID_REG = 11;      // R11 contains syscall number

        // Syscall handlers
        internal SysTTY SysTTYHandler { get; }
        internal SysEvent SysEventHandler { get; }
        internal SysEventFlag SysEventFlagHandler { get; }
        internal SysFs SysFsHandler { get; }
        internal SysMemory SysMemoryHandler { get; }
        internal SysMMapper SysMMapperHandler { get; }
        internal SysMutex SysMutexHandler { get; }
        internal SysCond SysCondHandler { get; }
        internal SysPPUThread SysPPUThreadHandler { get; }
        internal SysProcess SysProcessHandler { get; }
        internal SysPRX SysPRXHandler { get; }
        internal SysRWLock SysRWLockHandler { get; }
        internal SysSemaphore SysSemaphoreHandler { get; }
        internal SysSPU SysSPUHandler { get; }   // TODO
        internal SysStorage SysStorageHandler { get; }
        internal SysTime SysTimeHandler { get; }
        internal SysTimer SysTimerHandler { get; }
        internal SysVm SysVmHandler { get; }
        internal SysUtil SysUtilHandler { get; }

        // Module handlers
        internal SysPrxForUser SysPrxForUserHandler { get; }
        internal SysLwMutex SysLwMutexHandler { get; }
        internal CellGcmSys CellGcmSysHandler { get; }
        internal CellAudio CellAudioHandler { get; }
        internal CellVideoOut CellVideoOutHandler { get; }
        internal CellSysModule CellSysModuleHandler { get; }
        internal CellPNGDec CellPNGDecHandler { get; }
        internal CellResc CellRescHandler { get; }
        internal CellGame CellGameHandler { get; }
        internal CellRtc CellRtcHandler { get; }
        internal CellFs CellFsHandler { get; }
        internal SCENPTrophy SceNPTrophyHandler { get; }
        internal CellPad CellPadHandler { get; }
        internal SCENP SceNPHandler { get; }
        internal CellNetCtl CellNetCtlHandler { get; }
        internal CellIO CellIOHandler { get; }

        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;
        private readonly ModulePatcher _modulePatcher;
        private readonly PRX.PRXManager _prxManager;
        private readonly ImmutableDictionary<SyscallID, Func<PPUThread, ulong>> _syscallTable;
        private readonly ImmutableDictionary<uint, Func<PPUThread, ulong>> _moduleTable;

        internal LV2SyscallDispatcher(
            ThreadManager threadManager, 
            MemoryManager memoryManager, 
            ModulePatcher modulePatcher, 
            PRX.PRXManager prxManager, 
            FileSystemManager fileSystemManager, 
            CellGcmSys cellGcmSys, 
            CellPad cellPad, 
            CellGame cellGame)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
            _modulePatcher = modulePatcher;
            _prxManager = prxManager;

            // Initialize all syscall handlers
            SysTTYHandler = new(_memoryManager);
            SysEventHandler = new(_memoryManager);
            SysEventFlagHandler = new(_memoryManager);
            SysFsHandler = new(_memoryManager, fileSystemManager);
            SysMemoryHandler = new(_memoryManager);
            SysMMapperHandler = new(_memoryManager);
            SysMutexHandler = new(_memoryManager);
            SysProcessHandler = new(_memoryManager);
            SysPRXHandler = new(_memoryManager);
            SysRWLockHandler = new(_memoryManager);
            SysSemaphoreHandler = new(_memoryManager);
            SysSPUHandler = new(_memoryManager);
            SysStorageHandler = new(_memoryManager);
            SysTimeHandler = new(_memoryManager);
            SysTimerHandler = new(_memoryManager);

            SysVmHandler = new(_memoryManager);
            SysUtilHandler = new(_memoryManager);
            SysPPUThreadHandler = new(_threadManager, _memoryManager);
            SysCondHandler = new(_memoryManager, SysMutexHandler);

            // Initialize all module handlers
            CellGcmSysHandler = cellGcmSys;
            CellPadHandler = cellPad;
            CellGameHandler = cellGame;
            SysPrxForUserHandler = new(_threadManager, _memoryManager);
            SysLwMutexHandler = new(_memoryManager);
            CellAudioHandler = new(_memoryManager, SysEventHandler);
            CellVideoOutHandler = new(_memoryManager);
            CellSysModuleHandler = new(_memoryManager);
            CellPNGDecHandler = new(_memoryManager);
            CellRescHandler = new(_memoryManager, CellGcmSysHandler);
            CellRtcHandler = new(_memoryManager);
            CellFsHandler = new(_memoryManager, SysFsHandler);
            SceNPTrophyHandler = new(_memoryManager);
            SceNPHandler = new(_memoryManager);
            CellNetCtlHandler = new(_memoryManager);
            CellIOHandler = new(_memoryManager);

            // Create dispatch tables
            _syscallTable = CreateSyscallTable();
            _moduleTable = CreateModuleTable();
        }

        internal void DispatchSyscall(PPUThread ppu, uint lev)
        {
            SyscallID id = (SyscallID)ppu.ReadArg<uint>(SYSCALL_ID_REG);

            if (id == SyscallID.ModuleCall)
            {
                DispatchModule(ppu);
                return;
            }

            if (id == SyscallID.CellGCMCallback)
            {
                ulong result = CellGcmSysHandler.CellGcmCallback(ppu);
                ppu.Return(result);
                return;
            }

            if (_syscallTable.TryGetValue(id, out Func<PPUThread, ulong>? handler))
            {
                ulong result = handler(ppu);
                ppu.Return(result);
            }
            else
            {
                Console.WriteLine($"Unknown syscall: 0x{(uint)id:X3}");
                ppu.Return(0);
            }
        }

        private void DispatchModule(PPUThread ppu)
        {
            uint stubAddress = ppu.GetPC() - 8; // The instruction before syscall
            ModuleStubInfo info = _modulePatcher.GetModuleInfo(stubAddress);

            // Check if function is imported
            if (_prxManager.TryGetFunction(info.NID, out uint ptr))
            {
                ulong returnedValue = PPUInterpreter.ExecuteFunction(ppu, ptr, toc: true, []);
                return;
            }

            ulong result;

            if (_moduleTable.TryGetValue(info.NID, out Func<PPUThread, ulong>? handler))
            {
                result = handler(ppu);
            }
            else
            {
                result = ModuleStub(info, ppu);
            }

            ppu.Return(result);
        }

        private ulong ModuleStub(ModuleStubInfo info, PPUThread ppu)
        {
            Console.WriteLine($"[{ppu.ThreadName}] unimplemented {info.ModuleName} -> {info.FunctionName}");
            return CELL_OK;
        }

        private void LogLLECall(PPUThread ppu, uint ptr, ModuleStubInfo info, ulong returnValue)
        {
            uint pc = _memoryManager.Read<uint>(ptr);
            uint arg0 = ppu.ReadArg<uint>(ARG0);
            uint arg1 = ppu.ReadArg<uint>(ARG1);
            uint arg2 = ppu.ReadArg<uint>(ARG2);
            uint lr = ppu.GetLR();
            Console.WriteLine($"[{ppu.ThreadName}] LLE function: {info.FunctionName} arg0 = 0x{arg0:X8} arg1 = 0x{arg1:X8} arg2 = 0x{arg2:X8} lr = 0x{lr:X8} pc: 0x{pc:X8} - reutned: 0x{returnValue}");
        }
    }
}