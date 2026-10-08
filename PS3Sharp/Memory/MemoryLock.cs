namespace PS3Sharp.Memory
{
    internal partial class MemoryManager
    {
        internal readonly ref struct MemoryLock : IDisposable
        {
            private readonly object _lock;

            public MemoryLock(object managerLock)
            {
                _lock = managerLock;
                Monitor.Enter(_lock);
            }

            public void Dispose()
            {
                Monitor.Exit(_lock);
            }
        }
    }
}