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

        private unsafe delegate void* NativeReserveDelegate(nuint size, PageAccess access);
        private unsafe delegate void* NativeCommitDelegate(nint address, nuint size, PageAccess access);
        private unsafe delegate void* NativeDecommitDelegate(nint address, nuint size);
        private unsafe delegate bool NativeFreeDelegate(nint address);

        private static readonly NativeReserveDelegate NativeReserve = ResolveReserve();
        private static readonly NativeFreeDelegate NativeFree = ResolveFree();
        private static readonly NativeDecommitDelegate NativeDecommit = ResolveDecommit();
        private static readonly NativeCommitDelegate NativeCommit = ResolveCommit();
        internal static uint NativePageSize = GetNativePageSize();

        internal unsafe static void* Reserve(nuint size, PageAccess access) => NativeReserve(size, access);
        internal unsafe static void* Commit(nint address, nuint size, PageAccess access) => NativeCommit(address, size, access);
        internal unsafe static void* Decommit(nint address, nuint size) => NativeDecommit(address, size);
        internal unsafe static bool Free(nint address) => NativeFree(address);

        private static uint GetNativePageSize()
        {
            if (OperatingSystem.IsWindows())
            {
                return WindowsVirtualMemory.GetNativePageSize();
            }
            else if (OperatingSystem.IsLinux())
            {
                return LinuxVirtualMemory.GetNativePageSize();
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }

        private static unsafe NativeReserveDelegate ResolveReserve()
        {
            if (OperatingSystem.IsWindows())
            {
                return WindowsVirtualMemory.Reserve;
            }
            else if (OperatingSystem.IsLinux())
            {
                return LinuxVirtualMemory.Reserve;
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }

        private static unsafe NativeFreeDelegate ResolveFree()
        {
            if (OperatingSystem.IsWindows())
            {
                return WindowsVirtualMemory.Unreserve;
            }
            else if (OperatingSystem.IsLinux())
            {
                return LinuxVirtualMemory.Unreserve;
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }

        private static unsafe NativeCommitDelegate ResolveCommit()
        {
            if (OperatingSystem.IsWindows())
            {
                return WindowsVirtualMemory.Commit;
            }
            else if (OperatingSystem.IsLinux())
            {
                return LinuxVirtualMemory.Commit;
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }

        private static unsafe NativeDecommitDelegate ResolveDecommit()
        {
            if (OperatingSystem.IsWindows())
            {
                return WindowsVirtualMemory.Decommit;
            }
            else if (OperatingSystem.IsLinux())
            {
                return LinuxVirtualMemory.Decommit;
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }
    }
}
