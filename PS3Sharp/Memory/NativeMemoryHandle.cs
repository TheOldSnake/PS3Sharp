using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace PS3Sharp.Memory
{
    internal sealed class NativeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal nuint Size { get; private set; }

        internal NativeMemoryHandle(nuint size) : base(true)
        {
            unsafe
            {
                Size = size;
                nint alloc = (nint)NativeMemory.AllocZeroed(size);

                if (alloc == nint.Zero)
                {
                    throw new Exception($"[NativeMemoryHandle] Allocation failed");
                }

                SetHandle(alloc);
                Console.WriteLine($"[NativeMemoryHandle] Allocated memory at 0x{handle:X}");
            }
        }

        protected override bool ReleaseHandle()
        {
            Console.WriteLine($"[NativeMemoryHandle] Freeing memory at 0x{handle:X}");

            unsafe
            {
                NativeMemory.Free((void*)handle);
                SetHandle(nint.Zero);
                Size = 0;
                return true;
            }
        }
    }
}
