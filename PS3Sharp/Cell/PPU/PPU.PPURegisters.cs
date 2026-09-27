using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.PPU
{
    internal unsafe sealed partial class PPU
    {
        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct PPUGPR
        {
            //Unsigned
            [FieldOffset(0)] internal byte Value8;
            [FieldOffset(0)] internal ushort Value16;
            [FieldOffset(0)] internal uint Value32;
            [FieldOffset(0)] internal ulong Value64;

            //Signed
            [FieldOffset(0)] internal sbyte SValue8;
            [FieldOffset(0)] internal short SValue16;
            [FieldOffset(0)] internal int SValue32;
            [FieldOffset(0)] internal long SValue64;

            //As array
            [FieldOffset(0)] internal fixed byte Bytes[8];
            [FieldOffset(0)] internal fixed ushort UShorts[4];
            [FieldOffset(0)] internal fixed uint UInts[2];

            public override string ToString()
            {
                return $"{Value64:x16}";
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct PPUFPR
        {
            //Raw
            //[FieldOffset(0)] internal ulong Value32;
            [FieldOffset(0)] internal ulong Value64;
            [FieldOffset(0)] internal long SValue64;

            // [FieldOffset(0)] internal float Float32;
            [FieldOffset(0)] internal double Float64;

            public override string ToString()
            {
                return $"{Value64:x16} - F: {Float64}";
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct PPUVR
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
            [FieldOffset(0)] internal float Float32;
            [FieldOffset(0)] internal long SValue64;
            [FieldOffset(0)] internal double Float64;
            [FieldOffset(0)] internal Int128 SValue128;

            //As array
            [FieldOffset(0)] internal fixed byte Bytes[16];
            [FieldOffset(0)] internal fixed ushort UShorts[8];
            [FieldOffset(0)] internal fixed uint UInts[4];
            [FieldOffset(0)] internal fixed float Floats[4];
            [FieldOffset(0)] internal fixed ulong ULongs[2];
            [FieldOffset(0)] internal fixed double Doubles[2];

            public override string ToString()
            {
                return $"{Value128:X16}";
            }
        }
    }
}
