namespace PS3Sharp.Memory_Manager {
    public static unsafe partial class MemoryManager {
        public struct MemoryBlock {
            public uint VirtualAddress;
            public uint Size;
            public uint End => VirtualAddress + Size;
            public uint Handle;
            public void* NativeAddress;
            public bool IsFastmem;
        }
    }
}