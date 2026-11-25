using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Recompiler;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Cell.Spu;
using PS3Sharp.Elf;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using PS3Sharp.OS;
using PS3Sharp.OS.Modules;
using PS3Sharp.PRX;
using static PS3Sharp.Elf.ElfDefinitions;

namespace PS3Sharp
{
    internal sealed class Emulator : IDisposable
    {
        private readonly MainElfLoader _elfLoader;
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
            SpuTables.InitializeStaticTables();

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
            _lv2SyscallDispatcher = new(_prxManager, _modulePatcher, _threadManager, _memoryManager, _fileSystemManager, _rsxSubSystem.GCM, _cellPad, _cellGame);
        }

        internal void RunEmulator()
        {
            if (_fileSystemManager.TryGetRequestedExecutable(out string elfPath))
            {
                RunExecutable(elfPath);
            }
        }

        private void RunExecutable(string elfPath)
        {
            uint entryPointer = LoadExecutable(elfPath);
            _prxManager.LoadPRXLibraries(_modulePatcher);

            PPUThread ppu_main = _threadManager.CreateMainPPUThread(entryPointer, elfPath, _lv2SyscallDispatcher);
            _prxManager.InitializeLibraries(ppu_main);

            ppu_main.StartThread();
            _threadManager.WaitForProcessEnd();
            _threadManager.PrintExitCodes();
        }

        private uint LoadExecutable(string path)
        {
            // Load and patch elf
            BinaryReader reader = new(File.OpenRead(path));
            ElfHeader64 header = _elfLoader.LoadMainElf(reader, _memoryManager, out byte[]? tls);
            _memoryManager.SetTlsImage(tls);

            _modulePatcher.HandleMainElfImports(reader, header, _memoryManager, _elfLoader);
            ulong entry = header.e_entry;
            return (uint)entry;
        }

        public void Dispose()
        {
            _lv2SyscallDispatcher.Dispose();
            _threadManager.Dispose();
            _ppuRecompiler.Dispose();
            _memoryManager.Dispose();
        }
    }
}