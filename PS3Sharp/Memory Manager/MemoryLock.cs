namespace PS3Sharp.Memory_Manager
{
    internal static partial class MemoryManager
    {
        internal readonly ref struct MemoryLock : IDisposable
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