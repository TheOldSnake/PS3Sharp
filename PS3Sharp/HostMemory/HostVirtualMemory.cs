using System.Runtime.InteropServices;

namespace PS3Sharp.HostMemory
{
    internal static partial class HostVirtualMemory
    {
        internal enum PageAccess
        {
            NoAccess,
            ReadWrite,
            ReadWriteExecute
        }

        private static readonly IHostVirtualMemoryHandler _hostVirtualMemoryHandler = GetVirtualMemoryHandler();

        internal static readonly uint HostPageSize = _hostVirtualMemoryHandler.GetHostPageSize();
        internal unsafe static void* Reserve(nuint size, PageAccess access) => _hostVirtualMemoryHandler.Reserve(size, access);
        internal unsafe static void* Commit(nint address, nuint size, PageAccess access) => _hostVirtualMemoryHandler.Commit(address, size, access);
        internal unsafe static void* Decommit(nint address, nuint size) => _hostVirtualMemoryHandler.Decommit(address, size);
        internal static bool Free(nint address) => _hostVirtualMemoryHandler.Free(address);

        private static IHostVirtualMemoryHandler GetVirtualMemoryHandler()
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsVirtualMemory();
            } 
            else if (OperatingSystem.IsLinux())
            {
                return new LinuxVirtualMemory();
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }
    }
}
