using PS3Sharp.Common;

namespace PS3Sharp.ELFLoader
{
    internal static partial class ElfLoader
    {
        internal struct PRXRelocation
        {
            internal BigEndian<ulong> offs;
            internal BigEndian<ushort> unk;
            internal byte data_idx;
            internal byte addr_idx;
            internal BigEndian<uint> type;
            internal BigEndian<ulong> ptr;
        }
    }
}
