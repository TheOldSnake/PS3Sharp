using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    public static partial class VertexShaderGenerator
    {

        // Based on nv40_vertprog.h in PSL1GHT

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexInstruction
        {
            public uint Raw_0;
            public uint Raw_1;
            public uint Raw_2;
            public uint Raw_3;

            //Raw 0

            //Dest swizzle
            public uint Addr_Reg_Swizzle => (Raw_0 >> 00) & 0x3;

            // Condition register swizzle
            public uint Cond_Swizzle_W => (Raw_0 >> 02) & 0x3;
            public uint Cond_Swizzle_Z => (Raw_0 >> 04) & 0x3;
            public uint Cond_Swizzle_Y => (Raw_0 >> 06) & 0x3;
            public uint Cond_Swizzle_X => (Raw_0 >> 08) & 0x3;
            public uint Cond => (Raw_0 >> 10) & 0x7;                // condition type (lessThan, greaterThan, etc..) 
            public bool CondEnabled => ((Raw_0 >> 13) & 1) == 1;    // if enabled assign conditionally

            public uint Vec_Dest_Temp => (Raw_0 >> 15) & 0x3F;
            public bool Src0Abs => ((Raw_0 >> 21) & 1) == 1;
            public bool Src1Abs => ((Raw_0 >> 22) & 1) == 1;
            public bool Src2Abs => ((Raw_0 >> 23) & 1) == 1;
            public uint Addr_Reg_Sel => (Raw_0 >> 24) & 0x1;        // addr0 / addr1 selector
            public uint Cond_Reg_Sel => (Raw_0 >> 25) & 0x1;        // cc0 / cc1 selector
            public bool Saturate => ((Raw_0 >> 26) & 1) == 1;
            public bool IsVectorResult => ((Raw_0 >> 30) & 1) == 1;

            //Raw 1
            public uint Src0_Hi => Raw_1 & 0xFF;
            public uint VectorOp => (Raw_1 >> 22) & 0x1F;
            public uint ScalarOp => (Raw_1 >> 27) & 0x1F;
            public uint ConstSrc => (Raw_1 >> 12) & 0x3FF;
            public uint InputSrc => (Raw_1 >> 08) & 0xF;        //Index into InputNames array


            //Raw 2
            public uint Src2_Hi => Raw_2 & 0x3F;
            public uint Src1 => (Raw_2 >> 6) & 0x1FFFF;
            public uint Src0_Lo => (Raw_2 >> 23) & 0x1FF;


            //Raw 3
            public bool End => (Raw_3 & 1) == 1;            // End of shader program
            public bool IsIndexedConst => ((Raw_3 >> 1) & 1) == 1;
            public uint Dest => (Raw_3 >> 2) & 0x1F;
            public uint Sca_Dest_Temp => (Raw_3 >> 7) & 0x3F;
            public bool W_Vec_Mask => ((Raw_3 >> 13) & 1) == 1;
            public bool Z_Vec_Mask => ((Raw_3 >> 14) & 1) == 1;
            public bool Y_Vec_Mask => ((Raw_3 >> 15) & 1) == 1;
            public bool X_Vec_Mask => ((Raw_3 >> 16) & 1) == 1;
            public bool W_Sca_Mask => ((Raw_3 >> 17) & 1) == 1;
            public bool Z_Sca_Mask => ((Raw_3 >> 18) & 1) == 1;
            public bool Y_Sca_Mask => ((Raw_3 >> 19) & 1) == 1;
            public bool X_Sca_Mask => ((Raw_3 >> 20) & 1) == 1;

            public uint Src2_Lo => (Raw_3 >> 21) & 0x7FF;

            public uint Src0 => Src0_Lo | (Src0_Hi << 9);
            public uint Src2 => Src2_Lo | (Src2_Hi << 11);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexSrc
        {
            public uint Raw;
            public uint Type => Raw & 0x3;
            public uint TempID => (Raw >> 2) & 0x1F;

            //Source swizzle
            public uint Src_Swizzle_W => (Raw >> 08) & 0x3;
            public uint Src_Swizzle_Z => (Raw >> 10) & 0x3;
            public uint Src_Swizzle_Y => (Raw >> 12) & 0x3;
            public uint Src_Swizzle_X => (Raw >> 14) & 0x3;
            public bool Negate => ((Raw >> 16) & 1) == 1;
        }

        public static readonly string[] InputNames = [
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

        public static readonly string[] OutputNames = [
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


        public const uint NVFX_VP_INST_SCA_OP_NOP = 0x00;
        public const uint NVFX_VP_INST_SCA_OP_MOV = 0x01;
        public const uint NVFX_VP_INST_SCA_OP_RCP = 0x02;
        public const uint NVFX_VP_INST_SCA_OP_RCC = 0x03;
        public const uint NVFX_VP_INST_SCA_OP_RSQ = 0x04;
        public const uint NVFX_VP_INST_SCA_OP_EXP = 0x05;
        public const uint NVFX_VP_INST_SCA_OP_LOG = 0x06;
        public const uint NVFX_VP_INST_SCA_OP_LIT = 0x07;
        public const uint NVFX_VP_INST_SCA_OP_BRA = 0x09;
        public const uint NVFX_VP_INST_SCA_OP_CAL = 0x0B;
        public const uint NVFX_VP_INST_SCA_OP_RET = 0x0C;
        public const uint NVFX_VP_INST_SCA_OP_LG2 = 0x0D;
        public const uint NVFX_VP_INST_SCA_OP_EX2 = 0x0E;
        public const uint NVFX_VP_INST_SCA_OP_SIN = 0x0F;
        public const uint NVFX_VP_INST_SCA_OP_COS = 0x10;

        public const uint NV40_VP_INST_SCA_OP_PUSHA = 0x13;
        public const uint NV40_VP_INST_SCA_OP_POPA = 0x14;

        public const uint NVFX_VP_INST_VEC_OP_NOP = 0x00;
        public const uint NVFX_VP_INST_VEC_OP_MOV = 0x01;
        public const uint NVFX_VP_INST_VEC_OP_MUL = 0x02;
        public const uint NVFX_VP_INST_VEC_OP_ADD = 0x03;
        public const uint NVFX_VP_INST_VEC_OP_MAD = 0x04;
        public const uint NVFX_VP_INST_VEC_OP_DP3 = 0x05;
        public const uint NVFX_VP_INST_VEC_OP_DPH = 0x06;
        public const uint NVFX_VP_INST_VEC_OP_DP4 = 0x07;
        public const uint NVFX_VP_INST_VEC_OP_DST = 0x08;
        public const uint NVFX_VP_INST_VEC_OP_MIN = 0x09;
        public const uint NVFX_VP_INST_VEC_OP_MAX = 0x0A;
        public const uint NVFX_VP_INST_VEC_OP_SLT = 0x0B;
        public const uint NVFX_VP_INST_VEC_OP_SGE = 0x0C;
        public const uint NVFX_VP_INST_VEC_OP_ARL = 0x0D; // Load to address register
        public const uint NVFX_VP_INST_VEC_OP_FRC = 0x0E;
        public const uint NVFX_VP_INST_VEC_OP_FLR = 0x0F;
        public const uint NVFX_VP_INST_VEC_OP_SEQ = 0x10;
        public const uint NVFX_VP_INST_VEC_OP_SFL = 0x11;
        public const uint NVFX_VP_INST_VEC_OP_SGT = 0x12;
        public const uint NVFX_VP_INST_VEC_OP_SLE = 0x13;
        public const uint NVFX_VP_INST_VEC_OP_SNE = 0x14;
        public const uint NVFX_VP_INST_VEC_OP_STR = 0x15;
        public const uint NVFX_VP_INST_VEC_OP_SSG = 0x16;
        public const uint NVFX_VP_INST_VEC_OP_ARR = 0x17;
        public const uint NVFX_VP_INST_VEC_OP_ARA = 0x18;
    }
}
