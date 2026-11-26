using Microsoft.Win32.SafeHandles;

namespace PS3Sharp.HostMemory
{
    internal sealed unsafe class VirtualMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void* NativePointer => (void*)handle; // Should return nullptr if released
        internal nuint Size { get; private set; }

        internal VirtualMemoryHandle(nuint size, HostVirtualMemory.PageAccess access) : base(true)
        {
            unsafe
            {
                Size = size;
                nint alloc = (nint)HostVirtualMemory.Reserve(size, access);
                SetHandle(alloc);
            }

            Console.WriteLine($"[VirtualMemoryHandle] Allocated memory at 0x{handle:X}");
        }

        protected override bool ReleaseHandle()
        {
            Console.WriteLine($"[VirtualMemoryHandle] Freeing memory at 0x{handle:X}");

            unsafe
            {
                bool ok = HostVirtualMemory.Free(handle);

                if (ok)
                {
                    SetHandle(nint.Zero);  // Set handle to nullptr
                    Size = 0;
                    return true;
                }

                return false;
            }
        }
    }
}