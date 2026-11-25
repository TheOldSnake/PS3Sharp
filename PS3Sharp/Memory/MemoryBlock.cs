namespace PS3Sharp.Memory
{
    internal sealed unsafe partial class MemoryManager
    {
        internal readonly struct MemoryBlock
        {
            internal readonly uint Handle { init; get; }
            internal readonly uint VirtualAddress { init;  get; }
            internal readonly uint Size { init; get; }
            internal readonly uint End => VirtualAddress + Size;
            internal readonly void* NativeAddress { init; get; }

            internal static MemoryBlock Create(uint handle, uint address, uint size, void* nativeAddress)
            {
                MemoryBlock block = new()
                {
                    Handle = handle,
                    VirtualAddress = address,
                    Size = size,
                    NativeAddress = nativeAddress
                };

                return block;
            }
        }
    }
}