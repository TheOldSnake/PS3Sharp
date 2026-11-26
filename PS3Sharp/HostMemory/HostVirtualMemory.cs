using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp.HostMemory
{
    internal static class HostVirtualMemory
    {
        internal enum PageAccess
        {
            NoAccess,
            ReadWrite,
            ReadWriteExecute
        }

        private static readonly IHostVirtualMemory _hostVirtualMemory = GetVirtualMemoryHandler();

        internal static readonly uint HostPageSize = _hostVirtualMemory.GetHostPageSize();
        internal unsafe static void* Reserve(nuint size, PageAccess access) => _hostVirtualMemory.Reserve(size, access);
        internal unsafe static void* Commit(nint address, nuint size, PageAccess access) => _hostVirtualMemory.Commit(address, size, access);
        internal unsafe static void* Decommit(nint address, nuint size) => _hostVirtualMemory.Decommit(address, size);
        internal static bool Free(nint address) => _hostVirtualMemory.Free(address);

        private static IHostVirtualMemory GetVirtualMemoryHandler()
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
