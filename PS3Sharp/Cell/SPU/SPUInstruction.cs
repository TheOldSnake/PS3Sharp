namespace PS3Sharp.Cell.SPU
{
    internal readonly struct SPUInstruction
    {
        internal const int Se10Shift = 32 - 10;
        internal const int Se7Shift = 32 - 7;

        internal readonly int Raw { get; init; }

        // RR and R17 - 11 bits Opcode
        internal readonly RR17 RR17Op => (RR17)((Raw >> 21) & 0x7FF);

        // RI16 - 9 bits Opcode
        internal readonly RI16 RI16Op => (RI16)((Raw >> 23) & 0x1FF);

        // RI10 - 8 bits Opcode
        internal readonly RI10 RI10Op => (RI10)((Raw >> 24) & 0xFF);

        // RI18 - 7 bits Opcode
        internal readonly RI18 RI18Op => (RI18)((Raw >> 25) & 0x7F);

        // RRR - 4 bits Opcode
        internal readonly RRR RRROp => (RRR)((Raw >> 28) & 0xF);

        internal readonly int RtLo => Raw & 0x7F;
        internal readonly int Rc => Raw & 0x7F;
        internal readonly int Ra => (Raw >> 7) & 0x7F;
        internal readonly int Ca => (Raw >> 7) & 0x7F;
        internal readonly int I18 => (Raw >> 7) & 0x3FFFF;
        internal readonly int I16 => (Raw >> 7) & 0xFFFF;
        internal readonly int I10 => (Raw >> 14) & 0x3FF;
        internal readonly int Rb => (Raw >> 14) & 0x7F;
        internal readonly int I7 => (Raw >> 14) & 0x7F;
        internal readonly int E => (Raw >> 18) & 0x1;
        internal readonly int D => (Raw >> 19) & 0x1;
        internal readonly int RtHi => (Raw >> 21) & 0x7F;

        internal readonly int SeI16 => (short)I16;
        internal readonly int SeI10 => (I10 << Se10Shift) >> Se10Shift;
        internal readonly int SeI7 => (I7 << Se7Shift) >> Se7Shift;
    }

    // 11 bits Opcodes
    internal enum RR17 : int
    {
        Sync = 0x002,
        DSync = 0x003,
        Rdch = 0x00D,
        Sf = 0x040,
        Or = 0x041,
        Rotm = 0x059,
        Shl = 0x05B,
        Roti = 0x078,
        Rotmi = 0x079,
        Shli = 0x07B,
        A = 0x0C0,
        And = 0x0C1,
        Cg = 0x0C2,
        Wrch = 0x10D,
        Bisl = 0x1A9,
        Gb = 0x1B0,
        Gbb = 0x1B2,
        Fsm = 0x1B4,
        Fsmb = 0x1B6,
        Cbx = 0x1D4,
        Cbd = 0x1F4,
        Cwd = 0x1F6,
        Cdd = 0x1F7,
        Rotqbyi = 0x1FC,
        Rotqmbyi = 0x1FD,
        Shlqbyi = 0x1FF,
        Clz = 0x2A5,
        Xsbh = 0x2B6,
        Andc = 0x2C1,
        Clgtb = 0x2D0,
        Addx = 0x340,
        Ceq = 0x3C0,
        Ceqb = 0x3D0,
    }

    // 9 bits Opcodes
    internal enum RI16 : int
    {
        Stqa = 0x041,
        Brnz = 0x042,
        Stqr = 0x047,
        Fsmbi = 0x065,
        Brsl = 0x066,
        Lqa = 0x061,
        Lqr = 0x067,
        Il = 0x081,
        Ilhu = 0x082,
        Ilh = 0x083,
    }

    // 8 bits Opcodes
    internal enum RI10 : int
    {
        Ori = 0x04,
        Sfi = 0x0C,
        Andi = 0x14,
        Andhi = 0x15,
        Andbi = 0x16,
        Ai = 0x1C,
        Stqd = 0x24,
        Cgti = 0x4C,
        Clgti = 0x5C,
        Clgtbi = 0x5E,
        Ceqi = 0x7C,
        Ceqbi = 0x7E,
    }

    // 7 bits Opcodes
    internal enum RI18 : int
    {
        Hbrr = 0x09,
        Ila = 0x21,
    }

    // 4 bits Opcodes
    internal enum RRR : int
    {
        Selb = 0x8,
        ShufB = 0xB,
    }
}
