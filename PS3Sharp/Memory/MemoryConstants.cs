namespace PS3Sharp.Memory
{
    internal static class MemoryConstants
    {
        // Memory related constants
        internal const ulong VirtualMemorySize = 0x1_0000_0000; // 32-bit - 4 GiB
        internal const uint RamSize = 256 * 1024 * 1024;
        internal const uint RamStart = 0x00010000;
        internal const uint RamEnd = RamStart + RamSize;
        internal const uint StackRegionStart = 0xD0000000;
        internal const uint StackRegionSize = 0x10000000;
        internal const uint StackRegionBase = StackRegionStart + StackRegionSize - 16; //must be 16-byte aligned?
        internal const uint DefaultStackSize = 0x40000;
        internal const uint RsxVideoMemSize = 256 * 1024 * 1024;
        internal const uint RsxVideoMemStart = 0xC0000000;
        internal const uint RsxVideoMemEnd = RsxVideoMemStart + RsxVideoMemSize;
        internal const uint SpuLsSize = 256 * 1024;
        internal const uint RawSpuMemStart = 0xE0000000;
        internal const uint RawSpuOffset = 0x100000;
        internal const uint RawSpuProblemOffset = 0x40000;
        internal const uint PageBits = 16;
        internal const uint PageShift = (32 - PageBits);
        internal const uint PageCount = 1 << (int)PageBits;
        internal const uint PageSize = 1 << (int)PageShift;
        internal const uint PageMask = PageSize - 1;
    }
}
