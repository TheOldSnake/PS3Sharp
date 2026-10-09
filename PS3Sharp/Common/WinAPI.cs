using System.Runtime.InteropServices;

namespace PS3Sharp.Common
{
    internal static partial class NativeMethods
    {
        // Constants
        internal const uint WIN_API_PAGE_EXECUTE_READWRITE = 0x00000040;
        internal const uint WIN_API_MEM_COMMIT = 0x00001000;
        internal const uint WIN_API_MEM_RESERVE = 0x00002000;
        internal const uint WIN_API_MEM_DECOMMIT = 0x00004000;
        internal const uint WIN_API_MEM_RELEASE = 0x00008000;
        internal const uint WIN_API_PAGE_NOACCESS = 0x00000001;
        internal const uint WIN_API_PAGE_READWRITE = 0x00000004;

        // Import needed kernel functions

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial int VirtualProtect(nint lpAddress, nuint dwSize, uint flNewProtect, nint lpflOldProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial int VirtualFree(nint lpAddress, nuint dwSize, uint dwFreeType);
    }
}

