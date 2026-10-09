namespace PS3Sharp.Memory
{
    internal unsafe partial class MemoryManager
    {
        internal struct MemoryBlock
        {
            internal uint VirtualAddress;
            internal uint Size;
            internal uint End => VirtualAddress + Size;
            internal uint Handle;
            internal void* NativeAddress;
            internal bool IsFastmem;
        }
    }
}