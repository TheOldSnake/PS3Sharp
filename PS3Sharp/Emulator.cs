using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Recompiler;
using PS3Sharp.Cell.RSX;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using PS3Sharp.OS;
using PS3Sharp.OS.Modules;
using PS3Sharp.PRX;
using static PS3Sharp.ElfLoader;

namespace PS3Sharp
{
    internal class Emulator : IDisposable
    {
        private readonly ElfLoader _elfLoader;
        private readonly ModulePatcher _modulePatcher;
        private readonly MemoryManager _memoryManager;
        private readonly ThreadManager _threadManager;
        private readonly PRXManager _prxManager;
        private readonly FileSystemManager _fileSystemManager;
        private readonly RSXSubsystem _rsxSubSystem;
        private readonly CellPad _cellPad;
        private readonly CellGame _cellGame;
        private readonly LV2SyscallDispatcher _lv2SyscallDispatcher;
        private readonly PPURecompiler _ppuRecompiler;

        internal Emulator()
        {
            _elfLoader            = new();
            _modulePatcher        = new();
            _memoryManager        = new();
            _ppuRecompiler        = new();
            _threadManager        = new(_memoryManager, _ppuRecompiler);
            _prxManager           = new(_memoryManager);
            _fileSystemManager    = new(_memoryManager);
            _rsxSubSystem         = new(_threadManager, _memoryManager, _fileSystemManager);
            _cellPad              = new(_memoryManager, _rsxSubSystem.Widnow.JoystickReader);
            _cellGame             = new(_memoryManager, _fileSystemManager);
            _lv2SyscallDispatcher = new(_threadManager, _memoryManager, _modulePatcher, _prxManager, _fileSystemManager, _rsxSubSystem.GCM, _cellPad, _cellGame);
        }

        internal void RunEmulator()
        {
            string elfDirectory = _fileSystemManager.GetRequestedExecutable();

            if (elfDirectory == string.Empty)
            {
                return;
            }

            uint entryPointer = LoadExecutable(elfDirectory);
            _prxManager.LoadPRXLibraries(_elfLoader, _modulePatcher);

            PPUThread ppu_main = _threadManager.CreateMainPPUThread(entryPointer, elfDirectory, _lv2SyscallDispatcher);
            _prxManager.InitializeLibraries(ppu_main);

            ppu_main.StartThread();
            _threadManager.WaitForProcessEnd();
            _threadManager.PrintExitCodes();
        }

        internal uint LoadExecutable(string path)
        {
            // Load and patch elf
            BinaryReader reader = new(File.OpenRead(path));
            ElfHeader header = _elfLoader.LoadMainElf(reader, _memoryManager);
            _modulePatcher.HandleMainElfImports(reader, header, _memoryManager, _elfLoader);
            ulong entry = header.e_entry;
            return (uint)entry;
        }

        public void Dispose()
        {
            _threadManager.Dispose();
            _ppuRecompiler.Dispose();
            _memoryManager.Dispose();
        }
    }
}