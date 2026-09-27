using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.SPU
{

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct SPURegister
    {
        //Unsigned
        [FieldOffset(0)] internal byte Value8;
        [FieldOffset(0)] internal ushort Value16;
        [FieldOffset(0)] internal uint Value32;
        [FieldOffset(0)] internal ulong Value64;
        [FieldOffset(0)] internal UInt128 Value128;

        //Signed
        [FieldOffset(0)] internal sbyte SValue8;
        [FieldOffset(0)] internal short SValue16;
        [FieldOffset(0)] internal int SValue32;
        [FieldOffset(0)] internal long SValue64;
        [FieldOffset(0)] internal Int128 SValue128;

        //As array
        [FieldOffset(0)] internal fixed byte Bytes[16];
        [FieldOffset(0)] internal fixed ushort UShorts[8];
        [FieldOffset(0)] internal fixed uint UInts[4];
        [FieldOffset(0)] internal fixed ulong ULongs[2];
    }
}
