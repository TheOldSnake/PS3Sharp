namespace PS3Sharp.HostMemory
{
    internal static partial class HostVirtualMemory
    {
        private interface IHostVirtualMemoryHandler
        {
            internal unsafe void* Reserve(nuint size, PageAccess access);
            internal unsafe void* Commit(nint address, nuint size, PageAccess access);
            internal unsafe void* Decommit(nint address, nuint size);
            internal bool Free(nint address);
            internal uint GetHostPageSize();
        }
    }
}
