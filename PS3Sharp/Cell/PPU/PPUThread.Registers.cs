using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.PPU
{
    internal unsafe sealed partial class PPUThread
    {
        // TODO: Refactor and don't use fixed buffers, and maybe don't use LayoutKind.Explicit too

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

            public override string ToString()
            {
                return $"{Value64:x16}";
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct PPUFPR
        {
            [FieldOffset(0)] internal ulong Value64;
            [FieldOffset(0)] internal long SValue64;
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
        //Fixed-Point Exception Register
        //internal XERRegister XER;
        internal struct XERRegister
        {
            internal const int SOBit = 31;
            internal const int OVBit = 30;
            internal const int CABit = 29;

            internal ulong Raw;
            internal uint SO => (uint)((Raw >> SOBit) & 1);
            internal uint OV => (uint)((Raw >> OVBit) & 1);
            internal uint CA => (uint)((Raw >> CABit) & 1);

            internal void SetSO(uint v)
            {
                Raw &= ~(1u << SOBit);
                Raw |= (v << SOBit);
            }

            internal void SetOV(uint v)
            {
                Raw &= ~(1u << OVBit);
                Raw |= (v << OVBit);
            }

            internal void SetCA(uint v)
            {
                Raw &= ~(1u << CABit);
                Raw |= (v << CABit);
            }
        }

        internal struct ConditionRegister
        {
            internal const uint Eq = 0b0010;
            internal const uint Gt = 0b0100;
            internal const uint Lt = 0b1000;

            internal uint Raw;

            internal void WriteCRField(CRNumber cr, uint value)
            {
                value &= 0xF;
                int bit = 28 - ((int)cr * 4);  // CR Fields are reversed
                uint clearMask = (uint)~(0xF << bit);
                Raw &= clearMask;
                Raw |= value << bit;
            }

            internal uint GetCRField(CRNumber cr)
            {
                int bit = 28 - ((int)cr * 4);  // CR Fields are reversed
                return (Raw >> bit) & 0xF;
            }

            internal void WriteCRBit(uint bit, uint value)
            {       //bit number is in big endian
                int position = (int)(31 - bit);
                Raw &= ((uint)~(1 << position));
                Raw |= (value << position);
            }

            internal uint ReadCRBit(uint bit)
            {                 //bit number is in big endian
                int position = (int)(31 - bit);
                return (Raw >> position) & 1;
            }
        }

        // The bits in the Condition Register are grouped into
        // eight 4-bit fields, named CR Field 0 (CR0), ..., CR Field 7 (CR7)
        internal enum CRNumber
        {
            CR0 = 0,
            CR1 = 1,
            CR2 = 2,
            CR3 = 3,
            CR4 = 4,
            CR5 = 5,
            CR6 = 6,
            CR7 = 7
        }
    }
}