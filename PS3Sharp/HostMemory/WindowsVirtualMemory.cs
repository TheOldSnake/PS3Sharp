using Iced.Intel;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using static PS3Sharp.HostMemory.HostVirtualMemory;

namespace PS3Sharp.Common
{
    internal static partial class WindowsVirtualMemory
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
        private const uint WinApiPageExecuteReadwrite = 0x00000040;
        private const uint WinApiMemCommit = 0x00001000;
        private const uint WinApiMemReserve = 0x00002000;
        private const uint WinApiMemDecommit = 0x00004000;
        private const uint WinApiMemRelease = 0x00008000;
        private const uint WinApiPageNoaccess = 0x00000001;
        private const uint WinApiPageReadwrite = 0x00000004;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial int VirtualProtect(nint lpAddress, nuint dwSize, uint flNewProtect, nint lpflOldProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial int VirtualFree(nint lpAddress, nuint dwSize, uint dwFreeType);

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

        internal unsafe static void* Reserve(nuint size, PageAccess access)
        {
            uint type = GetPageAccessType(access);
            nint alloc = VirtualAlloc(0, size, WinApiMemReserve, type);

            if (alloc == nint.Zero)
            {
                int error = Marshal.GetLastPInvokeError();
                Console.WriteLine($"Could not allocate virtual memory - error: 0x{error:X8}");
                throw new Win32Exception(error);
            }

            return (void*)alloc;
        }

        internal unsafe static void* Commit(nint address, nuint size, PageAccess access)
        {
            // Note: VirtualAlloc will round size to 4 KB pages,
            // and will return the input address rounded down to nearest page boundary

            uint type = GetPageAccessType(access);
            void* result = (void*)VirtualAlloc(address, size, WinApiMemCommit, type);

            if (result == null)
            {
                int error = Marshal.GetLastPInvokeError();
                Console.WriteLine($"[WindowsNativeMemory] Could not commit memory - error: 0x{error:X8}");
                throw new Win32Exception(error);
            }

            NativeMemory.Clear((void*)address, size);
            return (void*)address;
        }

        internal unsafe static void* Decommit(nint address, nuint size)
        {
            // Note: VirtualFree will round size to 4 KB pages
            int ok = VirtualFree(address, size, WinApiMemDecommit);

            if (ok == 0)
            {
                int error = Marshal.GetLastPInvokeError();
                Console.WriteLine($"[WindowsNativeMemory] Could not release decommit memory - error: 0x{error:X8}");
                throw new Win32Exception(error);
            }

            return (void*)address;
        }

        internal static bool Unreserve(nint address)
        {
            // Pass size = 0 for MEM_RELEASE
            bool ok = VirtualFree(address, 0, WinApiMemRelease) != 0;

            if (!ok)
            {
                int error = Marshal.GetLastPInvokeError();
                Console.WriteLine($"Could not free virtual memory - error: 0x{error:X8}");
                // Do not throw an exception
            }

            return ok;
        }

        private static uint GetPageAccessType(PageAccess access)
        {
            switch (access)
            {
                case PageAccess.NoAccess:
                    return WinApiPageNoaccess;

                case PageAccess.ReadWrite:
                    return WinApiPageReadwrite;

                case PageAccess.ReadWriteExecute:
                    return WinApiPageExecuteReadwrite;

                default:
                    throw new NotImplementedException($"Unhandled access = {access}");
            }
        }
    }
}
