using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    public static partial class FragmentShaderGenerator
    {
        // Based on nvfx_shader.h in PSL1GHT

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FragmentInstruction
        {
            public uint OPDEST_Raw;
            public uint SRC0_Raw;
            public uint SRC1_Raw;
            public uint SRC2_Raw;

            //OPDEST
            public bool ProgramEnd => (OPDEST_Raw & 1) == 1;
            public uint DestinationIndex => (OPDEST_Raw >> 1) & 0x3F;

            public bool IsDestinationFp16 => ((OPDEST_Raw >> 7) & 1) == 1;
            public bool SetCC => ((OPDEST_Raw >> 8) & 1) == 1;
            public bool X_Mask => ((OPDEST_Raw >> 9) & 1) == 1;
            public bool Y_Mask => ((OPDEST_Raw >> 10) & 1) == 1;
            public bool Z_Mask => ((OPDEST_Raw >> 11) & 1) == 1;
            public bool W_Mask => ((OPDEST_Raw >> 12) & 1) == 1;
            public uint SourceIndex => (OPDEST_Raw >> 13) & 0xF; //Index into InputNames array
            public uint TextureIndex => (OPDEST_Raw >> 17) & 0xF; //sampler number
            public uint Opcode => (OPDEST_Raw >> 24) & 0x3F;
            public bool NoDest => ((OPDEST_Raw >> 30) & 1) == 1;
            public bool Saturate => ((OPDEST_Raw >> 31) & 1) == 1;

            //SRC common 
            public SRC SRC0 => new SRC { Raw = SRC0_Raw };
            public SRC SRC1 => new SRC { Raw = SRC1_Raw };
            public SRC SRC2 => new SRC { Raw = SRC2_Raw };

            public struct SRC
            {
                //Bits 0-17
                public uint Raw;
                public uint Type => (Raw & 0x3);
                public uint TempIndex => (Raw >> 2) & 0x3F;
                public bool IsFp16 => ((Raw >> 8) & 1) == 1;
                public uint SwizzleX => (Raw >> 9) & 0x3;
                public uint SwizzleY => (Raw >> 11) & 0x3;
                public uint SwizzleZ => (Raw >> 13) & 0x3;
                public uint SwizzleW => (Raw >> 15) & 0x3;
                public bool Negate => ((Raw >> 17) & 1) == 1;
            }

            // TODO: Special bits in each source 

            public bool ExecuteIfLess => ((SRC0_Raw >> 18) & 1) == 1;
            public bool ExecuteIfEqual => ((SRC0_Raw >> 19) & 1) == 1;
            public bool ExecuteIfGreater => ((SRC0_Raw >> 20) & 1) == 1;
            public bool IsConditional => !ExecuteIfLess || !ExecuteIfEqual || !ExecuteIfGreater;
            public uint CC_SwizzleX => (SRC0_Raw >> 21) & 0x3;
            public uint CC_SwizzleY => (SRC0_Raw >> 23) & 0x3;
            public uint CC_SwizzleZ => (SRC0_Raw >> 25) & 0x3;
            public uint CC_SwizzleW => (SRC0_Raw >> 27) & 0x3;
            public bool SRC0_Abs => ((SRC0_Raw >> 29) & 1) == 1;
            public uint CC_Index => ((SRC0_Raw >> 31) & 1); //In PSL1GHT this was stated to be "always 0"

            public bool SRC1_Abs => ((SRC1_Raw >> 18) & 1) == 1;


            // This scale if for the destination despite being in "SRC1"
            public uint Scale => (SRC1_Raw >> 28) & 0x7; // (0 = 1x, 1 = 2x, 2 = 4x, 3 = 8x, 4 = ???, 5, = 1/2, 6 = 1/4, 7 = 1/8)

            public bool SRC2_Abs => ((SRC2_Raw >> 18) & 1) == 1;
            public bool Perspective_Correction => ((SRC2_Raw >> 31) & 1) == 1;

        }


        public static readonly string[] InputNames = [
            "fs_wpos",
            "fs_col0",
            "fs_col1",
            "fs_fog",
            "fs_tex0",
            "fs_tex1",
            "fs_tex2",
            "fs_tex3",
            "fs_tex4",
            "fs_tex5",
            "fs_tex6",
            "fs_tex7",
            "fs_tex8",
            "fs_tex9",
            "fs_ssa"
        ];



        /* NV30/NV40 fragment program opcodes */
        public const uint NVFX_FP_OP_OPCODE_NOP = 0x00;
        public const uint NVFX_FP_OP_OPCODE_MOV = 0x01;
        public const uint NVFX_FP_OP_OPCODE_MUL = 0x02;
        public const uint NVFX_FP_OP_OPCODE_ADD = 0x03;
        public const uint NVFX_FP_OP_OPCODE_MAD = 0x04;
        public const uint NVFX_FP_OP_OPCODE_DP3 = 0x05;
        public const uint NVFX_FP_OP_OPCODE_DP4 = 0x06;
        public const uint NVFX_FP_OP_OPCODE_DST = 0x07;
        public const uint NVFX_FP_OP_OPCODE_MIN = 0x08;
        public const uint NVFX_FP_OP_OPCODE_MAX = 0x09;
        public const uint NVFX_FP_OP_OPCODE_SLT = 0x0A;
        public const uint NVFX_FP_OP_OPCODE_SGE = 0x0B;
        public const uint NVFX_FP_OP_OPCODE_SLE = 0x0C;
        public const uint NVFX_FP_OP_OPCODE_SGT = 0x0D;
        public const uint NVFX_FP_OP_OPCODE_SNE = 0x0E;
        public const uint NVFX_FP_OP_OPCODE_SEQ = 0x0F;
        public const uint NVFX_FP_OP_OPCODE_FRC = 0x10;
        public const uint NVFX_FP_OP_OPCODE_FLR = 0x11;
        public const uint NVFX_FP_OP_OPCODE_KIL = 0x12;
        public const uint NVFX_FP_OP_OPCODE_PK4B = 0x13;
        public const uint NVFX_FP_OP_OPCODE_UP4B = 0x14;
        public const uint NVFX_FP_OP_OPCODE_DDX = 0x15; /* can only write XY */
        public const uint NVFX_FP_OP_OPCODE_DDY = 0x16; /* can only write XY */
        public const uint NVFX_FP_OP_OPCODE_TEX = 0x17;
        public const uint NVFX_FP_OP_OPCODE_TXP = 0x18;
        public const uint NVFX_FP_OP_OPCODE_TXD = 0x19;
        public const uint NVFX_FP_OP_OPCODE_RCP = 0x1A;
        public const uint NVFX_FP_OP_OPCODE_RSQ = 0x1B;
        public const uint NVFX_FP_OP_OPCODE_EX2 = 0x1C;
        public const uint NVFX_FP_OP_OPCODE_LG2 = 0x1D;
        public const uint NVFX_FP_OP_OPCODE_STR = 0x20;
        public const uint NVFX_FP_OP_OPCODE_SFL = 0x21;
        public const uint NVFX_FP_OP_OPCODE_COS = 0x22;
        public const uint NVFX_FP_OP_OPCODE_SIN = 0x23;
        public const uint NVFX_FP_OP_OPCODE_PK2H = 0x24;
        public const uint NVFX_FP_OP_OPCODE_UP2H = 0x25;
        public const uint NVFX_FP_OP_OPCODE_PK4UB = 0x27;
        public const uint NVFX_FP_OP_OPCODE_UP4UB = 0x28;
        public const uint NVFX_FP_OP_OPCODE_PK2US = 0x29;
        public const uint NVFX_FP_OP_OPCODE_UP2US = 0x2A;
        public const uint NVFX_FP_OP_OPCODE_DP2A = 0x2E;
        public const uint NVFX_FP_OP_OPCODE_TXB = 0x31;
        public const uint NVFX_FP_OP_OPCODE_DP2 = 0x38;
        public const uint NVFX_FP_OP_OPCODE_NRM = 0x39;
        public const uint NVFX_FP_OP_OPCODE_DIV = 0x3A;
        public const uint NVFX_FP_OP_OPCODE_DIVSQ = 0x3B;
        public const uint NVFX_FP_OP_OPCODE_LIF = 0x3C;
        public const uint NVFX_FP_OP_OPCODE_FENCT = 0x3D;
        public const uint NVFX_FP_OP_OPCODE_FENCB = 0x3E;
        public const uint NVFX_FP_OP_OPCODE_BRK = 0x40;
        public const uint NVFX_FP_OP_OPCODE_CAL = 0x41;
        public const uint NVFX_FP_OP_OPCODE_IFE = 0x42;
        public const uint NVFX_FP_OP_OPCODE_LOOP = 0x43;
        public const uint NVFX_FP_OP_OPCODE_REP = 0x44;
        public const uint NVFX_FP_OP_OPCODE_RET = 0x45;
    }
}
