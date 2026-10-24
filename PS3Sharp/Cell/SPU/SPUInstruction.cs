namespace PS3Sharp.Cell.SPU
{
    internal struct SPUInstruction
    {
        internal uint Raw;

        internal readonly uint Op => (Raw >> 21) & 0x7FF;
    }
}
