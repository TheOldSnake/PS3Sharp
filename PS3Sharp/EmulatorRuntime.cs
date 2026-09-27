using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPURecompiler;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Memory_Manager;

namespace PS3Sharp
{
    internal class EmulatorRuntime : IDisposable
    {
        private bool _disposedValue;
        // internal static MemoryManager MemoryManager;         // todo
        internal readonly ThreadManager ThreadManager;          // todo internal fixes
        internal static RealitySynthesizer RSX;                 // temporary, todo remove static
        private readonly CancellationToken _shutdownToken;      // Shutdown signal

        internal EmulatorRuntime()
        {
            ThreadManager = new();
            RSX = new(ThreadManager);
            _shutdownToken = new CancellationToken();
        }

        internal void RunEmulator(string elfDirectory, bool isSimpleElf)
        {
            MemoryManager.Initialize();

            if (!isSimpleElf)
            {
                uint entryPointer = ElfLoader.LoadMainElf(elfDirectory);
                PPU ppu_main = new("main_thread", entryPointer, path_to_executable: elfDirectory, threadManager: ThreadManager, rsx: RSX);
                PRXManager.PRXManager.InitializeLibraries(ppu_main);
                ThreadManager.CreateNativePPUThread(ppu_main);

                ThreadManager.RunPPUThread(ppu_main);
                ThreadManager.JoinThread(ppu_main.NativeThread);
                ThreadManager.StopAll();
            }
            else
            {
                RunSimpleElf(elfDirectory);
            }
        }

        internal void RunSimpleElf(string path)
        {
            uint entryPointer = ElfLoader.LoadMainElf(path);

            // Create the PPU thread
            uint main_thread_id = ThreadManager.GetNewID();

            // ulong is probably wrong for PS3 ELFs, but correct for generic PPC64 ELFs?
            // uint is correct for PS3 elfs

            uint pc = MemoryManager.Read<uint>(entryPointer);
            uint toc = MemoryManager.Read<uint>(entryPointer + 4);

            PPU ppu_main = new("main_thread", (uint)pc, main_thread_id, MemoryManager.DEFAULT_STACK_SIZE);
            ppu_main.WriteReg<ulong>(2, toc);

            PPURecompiler ppu_recompiler = new();

            while (true)
            {
                try
                {
                    //PPUInterpreter.Step(ppu_main);
                    ppu_recompiler.RunPPU(ppu_main);
                }
                catch (Exception e)
                {

                    Console.WriteLine($"Exception: {e.Message}");
                    Console.WriteLine($"Trace: {e.StackTrace}");
                    Environment.Exit(1);
                }
            }
        }

        internal void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~EmulatorRuntime()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
