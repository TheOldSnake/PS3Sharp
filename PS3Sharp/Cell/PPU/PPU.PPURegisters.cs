using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.PPU {
    public unsafe partial class PPU {
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct PPUGPR {
            //Unsigned
            [FieldOffset(0)] public byte Value8;
            [FieldOffset(0)] public ushort Value16;
            [FieldOffset(0)] public uint Value32;
            [FieldOffset(0)] public ulong Value64;

            //Signed
            [FieldOffset(0)] public sbyte SValue8;
            [FieldOffset(0)] public short SValue16;
            [FieldOffset(0)] public int SValue32;
            [FieldOffset(0)] public long SValue64;

            //As array
            [FieldOffset(0)] public fixed byte Bytes[8];
            [FieldOffset(0)] public fixed ushort UShorts[4];
            [FieldOffset(0)] public fixed uint UInts[2];

            public override string ToString() {
                return $"{Value64:x16}";
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct PPUFPR {
            //Raw
            //[FieldOffset(0)] public ulong Value32;
            [FieldOffset(0)] public ulong Value64;
            [FieldOffset(0)] public long SValue64;

           // [FieldOffset(0)] public float Float32;
            [FieldOffset(0)] public double Float64;

            public override string ToString() {
                return $"{Value64:x16} - F: {Float64}";
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct PPUVR {
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
            [FieldOffset(0)] public float Float32;
            [FieldOffset(0)] public long SValue64;
            [FieldOffset(0)] public double Float64;
            [FieldOffset(0)] public Int128 SValue128;

            //As array
            [FieldOffset(0)] public fixed byte Bytes[16];
            [FieldOffset(0)] public fixed ushort UShorts[8];
            [FieldOffset(0)] public fixed uint UInts[4];
            [FieldOffset(0)] public fixed float Floats[4];
            [FieldOffset(0)] public fixed ulong ULongs[2];
            [FieldOffset(0)] public fixed double Doubles[2];

            public override string ToString() {
                return $"{Value128:X16}";
            }
        }
    }
}
