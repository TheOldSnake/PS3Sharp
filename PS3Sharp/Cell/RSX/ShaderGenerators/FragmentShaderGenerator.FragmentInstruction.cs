using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class FragmentShaderGenerator
    {
        // Based on nvfx_shader.h in PSL1GHT

        [StructLayout(LayoutKind.Sequential)]
        internal struct FragmentInstruction
        {
            internal uint OpDestRaw;
            internal uint Src0Raw;
            internal uint Src1Raw;
            internal uint Src2Raw;

            //OPDEST
            internal bool ProgramEnd => (OpDestRaw & 1) == 1;
            internal uint DestinationIndex => (OpDestRaw >> 1) & 0x3F;

            internal bool IsDestinationFp16 => ((OpDestRaw >> 7) & 1) == 1;
            internal bool SetCC => ((OpDestRaw >> 8) & 1) == 1;
            internal bool XMask => ((OpDestRaw >> 9) & 1) == 1;
            internal bool YMask => ((OpDestRaw >> 10) & 1) == 1;
            internal bool ZMask => ((OpDestRaw >> 11) & 1) == 1;
            internal bool WMask => ((OpDestRaw >> 12) & 1) == 1;
            internal uint SourceIndex => (OpDestRaw >> 13) & 0xF; //Index into InputNames array
            internal uint TextureIndex => (OpDestRaw >> 17) & 0xF; //sampler number
            internal uint Opcode => (OpDestRaw >> 24) & 0x3F;
            internal bool NoDest => ((OpDestRaw >> 30) & 1) == 1;
            internal bool Saturate => ((OpDestRaw >> 31) & 1) == 1;

            //SRC common 
            internal SRC SRC0 => new() { Raw = Src0Raw };
            internal SRC SRC1 => new() { Raw = Src1Raw };
            internal SRC SRC2 => new() { Raw = Src2Raw };

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

            internal bool ExecuteIfLess => ((Src0Raw >> 18) & 1) == 1;
            internal bool ExecuteIfEqual => ((Src0Raw >> 19) & 1) == 1;
            internal bool ExecuteIfGreater => ((Src0Raw >> 20) & 1) == 1;
            internal bool IsConditional => !ExecuteIfLess || !ExecuteIfEqual || !ExecuteIfGreater;
            internal uint CCSwizzleX => (Src0Raw >> 21) & 0x3;
            internal uint CCSwizzleY => (Src0Raw >> 23) & 0x3;
            internal uint CCSwizzleZ => (Src0Raw >> 25) & 0x3;
            internal uint CCSwizzleW => (Src0Raw >> 27) & 0x3;
            internal bool SRC0Abs => ((Src0Raw >> 29) & 1) == 1;
            internal uint CCIndex => ((Src0Raw >> 31) & 1); //In PSL1GHT this was stated to be "always 0"

            internal bool SRC1Abs => ((Src1Raw >> 18) & 1) == 1;


            // This scale if for the destination despite being in "SRC1"
            internal uint Scale => (Src1Raw >> 28) & 0x7; // (0 = 1x, 1 = 2x, 2 = 4x, 3 = 8x, 4 = ???, 5, = 1/2, 6 = 1/4, 7 = 1/8)

            internal bool SRC2Abs => ((Src2Raw >> 18) & 1) == 1;
            internal bool PerspectiveCorrection => ((Src2Raw >> 31) & 1) == 1;

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
        internal const uint NvfxFpOpOpcodeNop = 0x00;
        internal const uint NvfxFpOpOpcodeMov = 0x01;
        internal const uint NvfxFpOpOpcodeMul = 0x02;
        internal const uint NvfxFpOpOpcodeAdd = 0x03;
        internal const uint NvfxFpOpOpcodeMad = 0x04;
        internal const uint NvfxFpOpOpcodeDp3 = 0x05;
        internal const uint NvfxFpOpOpcodeDp4 = 0x06;
        internal const uint NvfxFpOpOpcodeDst = 0x07;
        internal const uint NvfxFpOpOpcodeMin = 0x08;
        internal const uint NvfxFpOpOpcodeMax = 0x09;
        internal const uint NvfxFpOpOpcodeSlt = 0x0A;
        internal const uint NvfxFpOpOpcodeSge = 0x0B;
        internal const uint NvfxFpOpOpcodeSle = 0x0C;
        internal const uint NvfxFpOpOpcodeSgt = 0x0D;
        internal const uint NvfxFpOpOpcodeSne = 0x0E;
        internal const uint NvfxFpOpOpcodeSeq = 0x0F;
        internal const uint NvfxFpOpOpcodeFrc = 0x10;
        internal const uint NvfxFpOpOpcodeFlr = 0x11;
        internal const uint NvfxFpOpOpcodeKil = 0x12;
        internal const uint NvfxFpOpOpcodePk4b = 0x13;
        internal const uint NvfxFpOpOpcodeUp4b = 0x14;
        internal const uint NvfxFpOpOpcodeDdx = 0x15; /* can only write XY */
        internal const uint NvfxFpOpOpcodeDdy = 0x16; /* can only write XY */
        internal const uint NvfxFpOpOpcodeTex = 0x17;
        internal const uint NvfxFpOpOpcodeTxp = 0x18;
        internal const uint NvfxFpOpOpcodeTxd = 0x19;
        internal const uint NvfxFpOpOpcodeRcp = 0x1A;
        internal const uint NvfxFpOpOpcodeRsq = 0x1B;
        internal const uint NvfxFpOpOpcodeEx2 = 0x1C;
        internal const uint NvfxFpOpOpcodeLg2 = 0x1D;
        internal const uint NvfxFpOpOpcodeStr = 0x20;
        internal const uint NvfxFpOpOpcodeSfl = 0x21;
        internal const uint NvfxFpOpOpcodeCos = 0x22;
        internal const uint NvfxFpOpOpcodeSin = 0x23;
        internal const uint NvfxFpOpOpcodePk2h = 0x24;
        internal const uint NvfxFpOpOpcodeUp2h = 0x25;
        internal const uint NvfxFpOpOpcodePk4ub = 0x27;
        internal const uint NvfxFpOpOpcodeUp4ub = 0x28;
        internal const uint NvfxFpOpOpcodePk2us = 0x29;
        internal const uint NvfxFpOpOpcodeUp2us = 0x2A;
        internal const uint NvfxFpOpOpcodeDp2a = 0x2E;
        internal const uint NvfxFpOpOpcodeTxb = 0x31;
        internal const uint NvfxFpOpOpcodeDp2 = 0x38;
        internal const uint NvfxFpOpOpcodeNrm = 0x39;
        internal const uint NvfxFpOpOpcodeDiv = 0x3A;
        internal const uint NvfxFpOpOpcodeDivsq = 0x3B;
        internal const uint NvfxFpOpOpcodeLif = 0x3C;
        internal const uint NvfxFpOpOpcodeFenct = 0x3D;
        internal const uint NvfxFpOpOpcodeFencb = 0x3E;
        internal const uint NvfxFpOpOpcodeBrk = 0x40;
        internal const uint NvfxFpOpOpcodeCal = 0x41;
        internal const uint NvfxFpOpOpcodeIfe = 0x42;
        internal const uint NvfxFpOpOpcodeLoop = 0x43;
        internal const uint NvfxFpOpOpcodeRep = 0x44;
        internal const uint NvfxFpOpOpcodeRet = 0x45;
    }
}
