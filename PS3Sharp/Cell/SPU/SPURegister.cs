using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.SPU
{

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct SPURegister
    {
        //Unsigned
        [FieldOffset(0)] public byte Value8;
        [FieldOffset(0)] public ushort Value16;
        [FieldOffset(0)] public uint Value32;
        [FieldOffset(0)] public ulong Value64;
        [FieldOffset(0)] public UInt128 Value128;

        //Signed
        [FieldOffset(0)] public sbyte SValue8;
        [FieldOffset(0)] public short SValue16;
        [FieldOffset(0)] public int SValue32;
        [FieldOffset(0)] public long SValue64;
        [FieldOffset(0)] public Int128 SValue128;

        //As array
        [FieldOffset(0)] public fixed byte Bytes[16];
        [FieldOffset(0)] public fixed ushort UShorts[8];
        [FieldOffset(0)] public fixed uint UInts[4];
        [FieldOffset(0)] public fixed ulong ULongs[2];
    }
}
