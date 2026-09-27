using PS3Sharp.Common;

namespace PS3Sharp.ELFLoader
{
    public static partial class ElfLoader
    {
        public struct PRXRelocation
        {
            public BigEndian<ulong> offs;
            public BigEndian<ushort> unk;
            public byte data_idx;
            public byte addr_idx;
            public BigEndian<uint> type;
            public BigEndian<ulong> ptr;
        }
    }
}
