namespace PS3Sharp.Memory_Manager
{
    public static partial class MemoryManager
    {
        public readonly ref struct MemoryLock : IDisposable
        {
            public MemoryLock()
            {
                Monitor.Enter(_managerLock);
            }

            public void Dispose()
            {
                Monitor.Exit(_managerLock);
            }
        }
    }
}