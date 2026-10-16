using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    internal partial class FragmentShaderGenerator
    {
        // Based on nvfx_shader.h in PSL1GHT

        [StructLayout(LayoutKind.Sequential)]
        internal struct FragmentInstruction
        {
            internal uint OPDEST_Raw;
            internal uint SRC0_Raw;
            internal uint SRC1_Raw;
            internal uint SRC2_Raw;

            //OPDEST
            internal bool ProgramEnd => (OPDEST_Raw & 1) == 1;
            internal uint DestinationIndex => (OPDEST_Raw >> 1) & 0x3F;

            internal bool IsDestinationFp16 => ((OPDEST_Raw >> 7) & 1) == 1;
            internal bool SetCC => ((OPDEST_Raw >> 8) & 1) == 1;
            internal bool X_Mask => ((OPDEST_Raw >> 9) & 1) == 1;
            internal bool Y_Mask => ((OPDEST_Raw >> 10) & 1) == 1;
            internal bool Z_Mask => ((OPDEST_Raw >> 11) & 1) == 1;
            internal bool W_Mask => ((OPDEST_Raw >> 12) & 1) == 1;
            internal uint SourceIndex => (OPDEST_Raw >> 13) & 0xF; //Index into InputNames array
            internal uint TextureIndex => (OPDEST_Raw >> 17) & 0xF; //sampler number
            internal uint Opcode => (OPDEST_Raw >> 24) & 0x3F;
            internal bool NoDest => ((OPDEST_Raw >> 30) & 1) == 1;
            internal bool Saturate => ((OPDEST_Raw >> 31) & 1) == 1;

            //SRC common 
            internal SRC SRC0 => new() { Raw = SRC0_Raw };
            internal SRC SRC1 => new() { Raw = SRC1_Raw };
            internal SRC SRC2 => new() { Raw = SRC2_Raw };

            internal struct SRC
            {
                //Bits 0-17
                internal uint Raw;
                internal uint Type => (Raw & 0x3);
                internal uint TempIndex => (Raw >> 2) & 0x3F;
                internal bool IsFp16 => ((Raw >> 8) & 1) == 1;
                internal uint SwizzleX => (Raw >> 9) & 0x3;
                internal uint SwizzleY => (Raw >> 11) & 0x3;
                internal uint SwizzleZ => (Raw >> 13) & 0x3;
                internal uint SwizzleW => (Raw >> 15) & 0x3;
                internal bool Negate => ((Raw >> 17) & 1) == 1;
            }

            // TODO: Special bits in each source 

            internal bool ExecuteIfLess => ((SRC0_Raw >> 18) & 1) == 1;
            internal bool ExecuteIfEqual => ((SRC0_Raw >> 19) & 1) == 1;
            internal bool ExecuteIfGreater => ((SRC0_Raw >> 20) & 1) == 1;
            internal bool IsConditional => !ExecuteIfLess || !ExecuteIfEqual || !ExecuteIfGreater;
            internal uint CC_SwizzleX => (SRC0_Raw >> 21) & 0x3;
            internal uint CC_SwizzleY => (SRC0_Raw >> 23) & 0x3;
            internal uint CC_SwizzleZ => (SRC0_Raw >> 25) & 0x3;
            internal uint CC_SwizzleW => (SRC0_Raw >> 27) & 0x3;
            internal bool SRC0_Abs => ((SRC0_Raw >> 29) & 1) == 1;
            internal uint CC_Index => ((SRC0_Raw >> 31) & 1); //In PSL1GHT this was stated to be "always 0"

            internal bool SRC1_Abs => ((SRC1_Raw >> 18) & 1) == 1;


            // This scale if for the destination despite being in "SRC1"
            internal uint Scale => (SRC1_Raw >> 28) & 0x7; // (0 = 1x, 1 = 2x, 2 = 4x, 3 = 8x, 4 = ???, 5, = 1/2, 6 = 1/4, 7 = 1/8)

            internal bool SRC2_Abs => ((SRC2_Raw >> 18) & 1) == 1;
            internal bool Perspective_Correction => ((SRC2_Raw >> 31) & 1) == 1;

        }

        internal static readonly string[] InputNames = [
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
        internal const uint NVFX_FP_OP_OPCODE_NOP = 0x00;
        internal const uint NVFX_FP_OP_OPCODE_MOV = 0x01;
        internal const uint NVFX_FP_OP_OPCODE_MUL = 0x02;
        internal const uint NVFX_FP_OP_OPCODE_ADD = 0x03;
        internal const uint NVFX_FP_OP_OPCODE_MAD = 0x04;
        internal const uint NVFX_FP_OP_OPCODE_DP3 = 0x05;
        internal const uint NVFX_FP_OP_OPCODE_DP4 = 0x06;
        internal const uint NVFX_FP_OP_OPCODE_DST = 0x07;
        internal const uint NVFX_FP_OP_OPCODE_MIN = 0x08;
        internal const uint NVFX_FP_OP_OPCODE_MAX = 0x09;
        internal const uint NVFX_FP_OP_OPCODE_SLT = 0x0A;
        internal const uint NVFX_FP_OP_OPCODE_SGE = 0x0B;
        internal const uint NVFX_FP_OP_OPCODE_SLE = 0x0C;
        internal const uint NVFX_FP_OP_OPCODE_SGT = 0x0D;
        internal const uint NVFX_FP_OP_OPCODE_SNE = 0x0E;
        internal const uint NVFX_FP_OP_OPCODE_SEQ = 0x0F;
        internal const uint NVFX_FP_OP_OPCODE_FRC = 0x10;
        internal const uint NVFX_FP_OP_OPCODE_FLR = 0x11;
        internal const uint NVFX_FP_OP_OPCODE_KIL = 0x12;
        internal const uint NVFX_FP_OP_OPCODE_PK4B = 0x13;
        internal const uint NVFX_FP_OP_OPCODE_UP4B = 0x14;
        internal const uint NVFX_FP_OP_OPCODE_DDX = 0x15; /* can only write XY */
        internal const uint NVFX_FP_OP_OPCODE_DDY = 0x16; /* can only write XY */
        internal const uint NVFX_FP_OP_OPCODE_TEX = 0x17;
        internal const uint NVFX_FP_OP_OPCODE_TXP = 0x18;
        internal const uint NVFX_FP_OP_OPCODE_TXD = 0x19;
        internal const uint NVFX_FP_OP_OPCODE_RCP = 0x1A;
        internal const uint NVFX_FP_OP_OPCODE_RSQ = 0x1B;
        internal const uint NVFX_FP_OP_OPCODE_EX2 = 0x1C;
        internal const uint NVFX_FP_OP_OPCODE_LG2 = 0x1D;
        internal const uint NVFX_FP_OP_OPCODE_STR = 0x20;
        internal const uint NVFX_FP_OP_OPCODE_SFL = 0x21;
        internal const uint NVFX_FP_OP_OPCODE_COS = 0x22;
        internal const uint NVFX_FP_OP_OPCODE_SIN = 0x23;
        internal const uint NVFX_FP_OP_OPCODE_PK2H = 0x24;
        internal const uint NVFX_FP_OP_OPCODE_UP2H = 0x25;
        internal const uint NVFX_FP_OP_OPCODE_PK4UB = 0x27;
        internal const uint NVFX_FP_OP_OPCODE_UP4UB = 0x28;
        internal const uint NVFX_FP_OP_OPCODE_PK2US = 0x29;
        internal const uint NVFX_FP_OP_OPCODE_UP2US = 0x2A;
        internal const uint NVFX_FP_OP_OPCODE_DP2A = 0x2E;
        internal const uint NVFX_FP_OP_OPCODE_TXB = 0x31;
        internal const uint NVFX_FP_OP_OPCODE_DP2 = 0x38;
        internal const uint NVFX_FP_OP_OPCODE_NRM = 0x39;
        internal const uint NVFX_FP_OP_OPCODE_DIV = 0x3A;
        internal const uint NVFX_FP_OP_OPCODE_DIVSQ = 0x3B;
        internal const uint NVFX_FP_OP_OPCODE_LIF = 0x3C;
        internal const uint NVFX_FP_OP_OPCODE_FENCT = 0x3D;
        internal const uint NVFX_FP_OP_OPCODE_FENCB = 0x3E;
        internal const uint NVFX_FP_OP_OPCODE_BRK = 0x40;
        internal const uint NVFX_FP_OP_OPCODE_CAL = 0x41;
        internal const uint NVFX_FP_OP_OPCODE_IFE = 0x42;
        internal const uint NVFX_FP_OP_OPCODE_LOOP = 0x43;
        internal const uint NVFX_FP_OP_OPCODE_REP = 0x44;
        internal const uint NVFX_FP_OP_OPCODE_RET = 0x45;
    }
}
