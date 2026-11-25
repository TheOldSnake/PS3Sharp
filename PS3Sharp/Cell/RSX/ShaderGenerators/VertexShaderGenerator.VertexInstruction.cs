using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class VertexShaderGenerator
    {

        // Based on nv40_vertprog.h in PSL1GHT

        [StructLayout(LayoutKind.Sequential)]
        internal struct VertexInstruction
        {
            internal uint Raw0;
            internal uint Raw1;
            internal uint Raw2;
            internal uint Raw3;

            //Raw 0

            //Dest swizzle
            internal uint AddrRegSwizzle => (Raw0 >> 00) & 0x3;

            // Condition register swizzle
            internal uint CondSwizzleW => (Raw0 >> 02) & 0x3;
            internal uint CondSwizzleZ => (Raw0 >> 04) & 0x3;
            internal uint CondSwizzleY => (Raw0 >> 06) & 0x3;
            internal uint CondSwizzleX => (Raw0 >> 08) & 0x3;
            internal uint Cond => (Raw0 >> 10) & 0x7;                // condition type (lessThan, greaterThan, etc..) 
            internal bool CondEnabled => ((Raw0 >> 13) & 1) == 1;    // if enabled assign conditionally

            internal uint VecDestTemp => (Raw0 >> 15) & 0x3F;
            internal bool Src0Abs => ((Raw0 >> 21) & 1) == 1;
            internal bool Src1Abs => ((Raw0 >> 22) & 1) == 1;
            internal bool Src2Abs => ((Raw0 >> 23) & 1) == 1;
            internal uint AddrRegSel => (Raw0 >> 24) & 0x1;        // addr0 / addr1 selector
            internal uint CondRegSel => (Raw0 >> 25) & 0x1;        // cc0 / cc1 selector
            internal bool Saturate => ((Raw0 >> 26) & 1) == 1;
            internal bool IsVectorResult => ((Raw0 >> 30) & 1) == 1;

            //Raw 1
            internal uint Src0Hi => Raw1 & 0xFF;
            internal uint VectorOp => (Raw1 >> 22) & 0x1F;
            internal uint ScalarOp => (Raw1 >> 27) & 0x1F;
            internal uint ConstSrc => (Raw1 >> 12) & 0x3FF;
            internal uint InputSrc => (Raw1 >> 08) & 0xF;        //Index into InputNames array


            //Raw 2
            internal uint Src2Hi => Raw2 & 0x3F;
            internal uint Src1 => (Raw2 >> 6) & 0x1FFFF;
            internal uint Src0Lo => (Raw2 >> 23) & 0x1FF;


            //Raw 3
            internal bool End => (Raw3 & 1) == 1;            // End of shader program
            internal bool IsIndexedConst => ((Raw3 >> 1) & 1) == 1;
            internal uint Dest => (Raw3 >> 2) & 0x1F;
            internal uint Sca_Dest_Temp => (Raw3 >> 7) & 0x3F;
            internal bool WVecMask => ((Raw3 >> 13) & 1) == 1;
            internal bool ZVecMask => ((Raw3 >> 14) & 1) == 1;
            internal bool YVecMask => ((Raw3 >> 15) & 1) == 1;
            internal bool XVecMask => ((Raw3 >> 16) & 1) == 1;
            internal bool WScaMask => ((Raw3 >> 17) & 1) == 1;
            internal bool ZScaMask => ((Raw3 >> 18) & 1) == 1;
            internal bool YScaMask => ((Raw3 >> 19) & 1) == 1;
            internal bool XScaMask => ((Raw3 >> 20) & 1) == 1;

            internal uint Src2Lo => (Raw3 >> 21) & 0x7FF;

            internal uint Src0 => Src0Lo | (Src0Hi << 9);
            internal uint Src2 => Src2Lo | (Src2Hi << 11);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct VertexSrc
        {
            internal uint Raw;
            internal uint Type => Raw & 0x3;
            internal uint TempID => (Raw >> 2) & 0x1F;

            //Source swizzle
            internal uint SrcSwizzleW => (Raw >> 08) & 0x3;
            internal uint SrcSwizzleZ => (Raw >> 10) & 0x3;
            internal uint SrcSwizzleY => (Raw >> 12) & 0x3;
            internal uint SrcSwizzleX => (Raw >> 14) & 0x3;
            internal bool Negate => ((Raw >> 16) & 1) == 1;
        }

        internal static readonly string[] InputNames = [
            "vs_pos",
            "vs_weight",
            "vs_normal",
            "vs_diff_color",
            "vs_spec_color",
            "vs_fog",
            "vs_point_size",
            "vs_unk7",
            "vs_tex0",
            "vs_tex1",
            "vs_tex2",
            "vs_tex3",
            "vs_tex4",
            "vs_tex5",
            "vs_tex6",
            "vs_tex7"
        ];

        internal static readonly string[] OutputNames = [
            "fs_wpos",
            "fs_col0",
            "fs_col1",
            "fs_bfc0",
            "fs_bfc1",
            "fs_fog",
            "fs_point_size",
            "fs_tex0",
            "fs_tex1",
            "fs_tex2",
            "fs_tex3",
            "fs_tex4",
            "fs_tex5",
            "fs_tex6",
            "fs_tex7",
            "fs_unk15"
        ];


        internal const uint NvfxVpInstScaOpNop = 0x00;
        internal const uint NvfxVpInstScaOpMov = 0x01;
        internal const uint NvfxVpInstScaOpRcp = 0x02;
        internal const uint NvfxVpInstScaOpRcc = 0x03;
        internal const uint NvfxVpInstScaOpRsq = 0x04;
        internal const uint NvfxVpInstScaOpExp = 0x05;
        internal const uint NvfxVpInstScaOpLog = 0x06;
        internal const uint NvfxVpInstScaOpLit = 0x07;
        internal const uint NvfxVpInstScaOpBra = 0x09;
        internal const uint NvfxVpInstScaOpCal = 0x0B;
        internal const uint NvfxVpInstScaOpRet = 0x0C;
        internal const uint NvfxVpInstScaOpLg2 = 0x0D;
        internal const uint NvfxVpInstScaOpEx2 = 0x0E;
        internal const uint NvfxVpInstScaOpSin = 0x0F;
        internal const uint NvfxVpInstScaOpCos = 0x10;

        internal const uint Nv40VpInstScaOpPusha = 0x13;
        internal const uint Nv40VpInstScaOpPopa = 0x14;

        internal const uint NvfxVpInstVecOpNop = 0x00;
        internal const uint NvfxVpInstVecOpMov = 0x01;
        internal const uint NvfxVpInstVecOpMul = 0x02;
        internal const uint NvfxVpInstVecOpAdd = 0x03;
        internal const uint NvfxVpInstVecOpMad = 0x04;
        internal const uint NvfxVpInstVecOpDp3 = 0x05;
        internal const uint NvfxVpInstVecOpDph = 0x06;
        internal const uint NvfxVpInstVecOpDp4 = 0x07;
        internal const uint NvfxVpInstVecOpDst = 0x08;
        internal const uint NvfxVpInstVecOpMin = 0x09;
        internal const uint NvfxVpInstVecOpMax = 0x0A;
        internal const uint NvfxVpInstVecOpSlt = 0x0B;
        internal const uint NvfxVpInstVecOpSge = 0x0C;
        internal const uint NvfxVpInstVecOpArl = 0x0D; // Load to address register
        internal const uint NvfxVpInstVecOpFrc = 0x0E;
        internal const uint NvfxVpInstVecOpFlr = 0x0F;
        internal const uint NvfxVpInstVecOpSeq = 0x10;
        internal const uint NvfxVpInstVecOpSfl = 0x11;
        internal const uint NvfxVpInstVecOpSgt = 0x12;
        internal const uint NvfxVpInstVecOpSle = 0x13;
        internal const uint NvfxVpInstVecOpSne = 0x14;
        internal const uint NvfxVpInstVecOpStr = 0x15;
        internal const uint NvfxVpInstVecOpSsg = 0x16;
        internal const uint NvfxVpInstVecOpArr = 0x17;
        internal const uint NvfxVpInstVecOpAra = 0x18;
    }
}
