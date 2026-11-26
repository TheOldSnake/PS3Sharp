using System.Runtime.InteropServices;

namespace PS3Sharp.Common
{
    internal static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SystemInfo
        {
            internal uint dwOemId;
            internal uint dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal nuint dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal ushort wProcessorLevel;
            internal ushort wProcessorRevision;
        }

        // Constants
        internal const uint WinApiPageExecuteReadwrite = 0x00000040;
        internal const uint WinApiMemCommit = 0x00001000;
        internal const uint WinApiMemReserve = 0x00002000;
        internal const uint WinApiMemDecommit = 0x00004000;
        internal const uint WinApiMemRelease = 0x00008000;
        internal const uint WinApiPageNoaccess = 0x00000001;
        internal const uint WinApiPageReadwrite = 0x00000004;

        // Import needed kernel functions

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial int VirtualProtect(nint lpAddress, nuint dwSize, uint flNewProtect, nint lpflOldProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial int VirtualFree(nint lpAddress, nuint dwSize, uint dwFreeType);

        [LibraryImport("kernel32.dll")]
        private static unsafe partial void GetSystemInfo(SystemInfo* lpSystemInfo);

        internal static uint GetNativePageSize()
        {
            unsafe
            {
                SystemInfo info = default;
                GetSystemInfo(&info);
                return info.dwPageSize;
            }
        }
    }
}