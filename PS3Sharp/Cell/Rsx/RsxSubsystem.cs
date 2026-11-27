using PS3Sharp.FileSystem;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS.Modules;

namespace PS3Sharp.Cell.Rsx
{
    internal sealed class RSXSubsystem
    {
        public RealitySynthesizer RSX { get; }
        public CellGcmSys GCM { get; }
        public EmulatorWindow Widnow { get; }

        public RSXSubsystem(ThreadManager threadManager, MemoryManager memoryManager, FileSystemManager fileSystemManager)
        {
            // TODO:
            // Remove RSX → CellGcmSysHandler dependency by moving the required GCM state into RSX and synchronizing it through explicit setters.

            Widnow = new(threadManager, fileSystemManager);
            GCM = new(memoryManager);
            RSX = threadManager.CreateRSXThread(Widnow, GCM);
            GCM.SetRSX(RSX);
        }
    }
}
