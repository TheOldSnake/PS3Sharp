using System.Runtime.InteropServices;

namespace PS3Sharp.Common
{
    internal static class WinAPI
    {
        // Constants
        internal const uint PAGE_EXECUTE_READWRITE = 0x00000040;
        internal const uint MEM_COMMIT = 0x00001000;
        internal const uint MEM_RESERVE = 0x00002000;
        internal const uint MEM_DECOMMIT = 0x00004000;
        internal const uint MEM_RELEASE = 0x00008000;
        internal const uint PAGE_NOACCESS = 0x00000001;
        internal const uint PAGE_READWRITE = 0x00000004;

        // Import needed kernel functions

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualProtect(nint lpAddress, nuint dwSize, uint flNewProtect, nint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualFree(nint lpAddress, nuint dwSize, uint dwFreeType);
    }
}

