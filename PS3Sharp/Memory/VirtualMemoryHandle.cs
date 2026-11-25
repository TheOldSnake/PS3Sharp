using Microsoft.Win32.SafeHandles;
using PS3Sharp.Common;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PS3Sharp.Memory
{
    internal sealed unsafe class VirtualMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void* NativePointer => (void*)handle; // Should return nullptr if released
        internal nuint Size { get; private set; }

        internal VirtualMemoryHandle(nuint size, uint type) : base(true)
        {
            unsafe
            {
                Size = size;
                nint alloc = NativeMethods.VirtualAlloc(0, size, NativeMethods.WinApiMemReserve, type);

                if (alloc == nint.Zero)
                {
                    int error = Marshal.GetLastPInvokeError();
                    Console.WriteLine($"Could not allocate virtual memory - error: 0x{error:X8}");
                    throw new Win32Exception(error);
                }

                SetHandle(alloc);
            }

            Console.WriteLine($"[VirtualMemoryHandle] Allocated memory at 0x{handle:X}");
        }

        protected override bool ReleaseHandle()
        {
            Console.WriteLine($"[VirtualMemoryHandle] Freeing memory at 0x{handle:X}");

            unsafe
            {
                // Pass 0 in size for MEM_RELEASE
                bool ok = NativeMethods.VirtualFree(handle, 0, NativeMethods.WinApiMemRelease) != 0;

                if (ok)
                {
                    SetHandle(nint.Zero);  // Set handle to nullptr
                    Size = 0;
                    return true;
                }
                else
                {
                    int error = Marshal.GetLastPInvokeError();
                    Console.WriteLine($"Could not free virtual memory - error: 0x{error:X8}");
                    // Do not throw an exception
                }

                return false;
            }
        }
    }
}