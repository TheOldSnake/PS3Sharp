using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    internal partial class VertexShaderGenerator
    {

        // Based on nv40_vertprog.h in PSL1GHT

        [StructLayout(LayoutKind.Sequential)]
        internal struct VertexInstruction
        {
            internal uint Raw_0;
            internal uint Raw_1;
            internal uint Raw_2;
            internal uint Raw_3;

            //Raw 0

            //Dest swizzle
            internal uint Addr_Reg_Swizzle => (Raw_0 >> 00) & 0x3;

            // Condition register swizzle
            internal uint Cond_Swizzle_W => (Raw_0 >> 02) & 0x3;
            internal uint Cond_Swizzle_Z => (Raw_0 >> 04) & 0x3;
            internal uint Cond_Swizzle_Y => (Raw_0 >> 06) & 0x3;
            internal uint Cond_Swizzle_X => (Raw_0 >> 08) & 0x3;
            internal uint Cond => (Raw_0 >> 10) & 0x7;                // condition type (lessThan, greaterThan, etc..) 
            internal bool CondEnabled => ((Raw_0 >> 13) & 1) == 1;    // if enabled assign conditionally

            internal uint Vec_Dest_Temp => (Raw_0 >> 15) & 0x3F;
            internal bool Src0Abs => ((Raw_0 >> 21) & 1) == 1;
            internal bool Src1Abs => ((Raw_0 >> 22) & 1) == 1;
            internal bool Src2Abs => ((Raw_0 >> 23) & 1) == 1;
            internal uint Addr_Reg_Sel => (Raw_0 >> 24) & 0x1;        // addr0 / addr1 selector
            internal uint Cond_Reg_Sel => (Raw_0 >> 25) & 0x1;        // cc0 / cc1 selector
            internal bool Saturate => ((Raw_0 >> 26) & 1) == 1;
            internal bool IsVectorResult => ((Raw_0 >> 30) & 1) == 1;

            //Raw 1
            internal uint Src0_Hi => Raw_1 & 0xFF;
            internal uint VectorOp => (Raw_1 >> 22) & 0x1F;
            internal uint ScalarOp => (Raw_1 >> 27) & 0x1F;
            internal uint ConstSrc => (Raw_1 >> 12) & 0x3FF;
            internal uint InputSrc => (Raw_1 >> 08) & 0xF;        //Index into InputNames array


            //Raw 2
            internal uint Src2_Hi => Raw_2 & 0x3F;
            internal uint Src1 => (Raw_2 >> 6) & 0x1FFFF;
            internal uint Src0_Lo => (Raw_2 >> 23) & 0x1FF;


            //Raw 3
            internal bool End => (Raw_3 & 1) == 1;            // End of shader program
            internal bool IsIndexedConst => ((Raw_3 >> 1) & 1) == 1;
            internal uint Dest => (Raw_3 >> 2) & 0x1F;
            internal uint Sca_Dest_Temp => (Raw_3 >> 7) & 0x3F;
            internal bool W_Vec_Mask => ((Raw_3 >> 13) & 1) == 1;
            internal bool Z_Vec_Mask => ((Raw_3 >> 14) & 1) == 1;
            internal bool Y_Vec_Mask => ((Raw_3 >> 15) & 1) == 1;
            internal bool X_Vec_Mask => ((Raw_3 >> 16) & 1) == 1;
            internal bool W_Sca_Mask => ((Raw_3 >> 17) & 1) == 1;
            internal bool Z_Sca_Mask => ((Raw_3 >> 18) & 1) == 1;
            internal bool Y_Sca_Mask => ((Raw_3 >> 19) & 1) == 1;
            internal bool X_Sca_Mask => ((Raw_3 >> 20) & 1) == 1;

            internal uint Src2_Lo => (Raw_3 >> 21) & 0x7FF;

            internal uint Src0 => Src0_Lo | (Src0_Hi << 9);
            internal uint Src2 => Src2_Lo | (Src2_Hi << 11);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct VertexSrc
        {
            internal uint Raw;
            internal uint Type => Raw & 0x3;
            internal uint TempID => (Raw >> 2) & 0x1F;

            //Source swizzle
            internal uint Src_Swizzle_W => (Raw >> 08) & 0x3;
            internal uint Src_Swizzle_Z => (Raw >> 10) & 0x3;
            internal uint Src_Swizzle_Y => (Raw >> 12) & 0x3;
            internal uint Src_Swizzle_X => (Raw >> 14) & 0x3;
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


        internal const uint NVFX_VP_INST_SCA_OP_NOP = 0x00;
        internal const uint NVFX_VP_INST_SCA_OP_MOV = 0x01;
        internal const uint NVFX_VP_INST_SCA_OP_RCP = 0x02;
        internal const uint NVFX_VP_INST_SCA_OP_RCC = 0x03;
        internal const uint NVFX_VP_INST_SCA_OP_RSQ = 0x04;
        internal const uint NVFX_VP_INST_SCA_OP_EXP = 0x05;
        internal const uint NVFX_VP_INST_SCA_OP_LOG = 0x06;
        internal const uint NVFX_VP_INST_SCA_OP_LIT = 0x07;
        internal const uint NVFX_VP_INST_SCA_OP_BRA = 0x09;
        internal const uint NVFX_VP_INST_SCA_OP_CAL = 0x0B;
        internal const uint NVFX_VP_INST_SCA_OP_RET = 0x0C;
        internal const uint NVFX_VP_INST_SCA_OP_LG2 = 0x0D;
        internal const uint NVFX_VP_INST_SCA_OP_EX2 = 0x0E;
        internal const uint NVFX_VP_INST_SCA_OP_SIN = 0x0F;
        internal const uint NVFX_VP_INST_SCA_OP_COS = 0x10;

        internal const uint NV40_VP_INST_SCA_OP_PUSHA = 0x13;
        internal const uint NV40_VP_INST_SCA_OP_POPA = 0x14;

        internal const uint NVFX_VP_INST_VEC_OP_NOP = 0x00;
        internal const uint NVFX_VP_INST_VEC_OP_MOV = 0x01;
        internal const uint NVFX_VP_INST_VEC_OP_MUL = 0x02;
        internal const uint NVFX_VP_INST_VEC_OP_ADD = 0x03;
        internal const uint NVFX_VP_INST_VEC_OP_MAD = 0x04;
        internal const uint NVFX_VP_INST_VEC_OP_DP3 = 0x05;
        internal const uint NVFX_VP_INST_VEC_OP_DPH = 0x06;
        internal const uint NVFX_VP_INST_VEC_OP_DP4 = 0x07;
        internal const uint NVFX_VP_INST_VEC_OP_DST = 0x08;
        internal const uint NVFX_VP_INST_VEC_OP_MIN = 0x09;
        internal const uint NVFX_VP_INST_VEC_OP_MAX = 0x0A;
        internal const uint NVFX_VP_INST_VEC_OP_SLT = 0x0B;
        internal const uint NVFX_VP_INST_VEC_OP_SGE = 0x0C;
        internal const uint NVFX_VP_INST_VEC_OP_ARL = 0x0D; // Load to address register
        internal const uint NVFX_VP_INST_VEC_OP_FRC = 0x0E;
        internal const uint NVFX_VP_INST_VEC_OP_FLR = 0x0F;
        internal const uint NVFX_VP_INST_VEC_OP_SEQ = 0x10;
        internal const uint NVFX_VP_INST_VEC_OP_SFL = 0x11;
        internal const uint NVFX_VP_INST_VEC_OP_SGT = 0x12;
        internal const uint NVFX_VP_INST_VEC_OP_SLE = 0x13;
        internal const uint NVFX_VP_INST_VEC_OP_SNE = 0x14;
        internal const uint NVFX_VP_INST_VEC_OP_STR = 0x15;
        internal const uint NVFX_VP_INST_VEC_OP_SSG = 0x16;
        internal const uint NVFX_VP_INST_VEC_OP_ARR = 0x17;
        internal const uint NVFX_VP_INST_VEC_OP_ARA = 0x18;
    }
}
