namespace PS3Sharp.Cell.SPU
{
    internal struct SPUInstruction
    {
        internal uint Raw;

        // RR and R17 - 11 bits Opcode
        internal readonly uint RR17Op => (Raw >> 21) & 0x7FF;

        // RI16 - 9 bits Opcode
        internal readonly uint RI16Op => (Raw >> 23) & 0x1FF;

        // RI10 - 8 bits Opcode
        internal readonly uint RI10Op => (Raw >> 24) & 0xFF;

        // RI18 - 7 bits Opcode
        internal readonly uint RI18Op => (Raw >> 25) & 0x7F;

        // RRR - 4 bits Opcode
        internal readonly uint RRROp => (Raw >> 28) & 0xF;
    }
}
