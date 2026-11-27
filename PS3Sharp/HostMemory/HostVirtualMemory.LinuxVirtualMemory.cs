using System.Runtime.InteropServices;

namespace PS3Sharp.HostMemory
{
    internal static partial class HostVirtualMemory
    {
        private sealed partial class LinuxVirtualMemory : IHostVirtualMemoryHandler
        {
            // TODO

            // Constants
            private const int ProtNone = 0x0;
            private const int ProtRead = 0x1;
            private const int ProtWrite = 0x2;
            private const int ProtExec = 0x4;

            private const int MapPrivate = 0x02;
            private const int MapAnonymous = 0x20;

            [LibraryImport("libc.so.6", SetLastError = true)]
            private static partial nint mmap(nint addr, nuint length, int prot, int flags, int fd, nint offset);

            public unsafe void* Reserve(nuint size, PageAccess access)
            {
                throw new NotImplementedException("todo");
            }

            public unsafe void* Commit(nint address, nuint size, PageAccess access)
            {
                throw new NotImplementedException("todo");
            }

            public unsafe void* Decommit(nint address, nuint size)
            {
                throw new NotImplementedException("todo");
            }

            public bool Free(nint address)
            {
                throw new NotImplementedException("todo");
            }

            public uint GetHostPageSize()
            {
                throw new NotImplementedException("todo");
            }

            private static uint GetPageAccessType(PageAccess access)
            {
                throw new NotImplementedException("todo");
            }
        }
    }
}
