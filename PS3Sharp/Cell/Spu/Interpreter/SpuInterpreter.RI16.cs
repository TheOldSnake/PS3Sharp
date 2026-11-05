using PS3Sharp.Common;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        // RI16: 9-bit opcode group
        private static void Stqa(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword (a-form)
            int rt = instruction.RtLo;

            // Offset is 18-bits
            int i16 = instruction.I16 << 2;

            unsafe
            {
                SpuState* state = spu.State;
                uint address = (uint)(i16 & 0x3FFF0);
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(address, value);
            }
        }

        private static void Brnz(SpuThread spu, SpuInstruction instruction)
        {
            // Branch If Not Zero Word
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;

                if (state->GPR[rt].UInts[PreferredSlot] != 0)
                {
                    // Offset is signed 18-bits
                    int i16 = instruction.I16 << 2;
                    int shift = 32 - 18;
                    int seI16 = (i16 << shift) >> shift;

                    uint currentPC = state->PC - 4;
                    uint target = (currentPC + (uint)seI16) & 0x3FFFC;

                    state->PC = target;
                }
            }
        }

        private static void Brhnz(SpuThread spu, SpuInstruction instruction)
        {
            // Branch If Not Zero Halfword
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;

                if ((state->GPR[rt].UInts[PreferredSlot] & 0xFFFF) != 0)
                {
                    // Offset is signed 18-bits
                    int i16 = instruction.I16 << 2;
                    int shift = 32 - 18;
                    int seI16 = (i16 << shift) >> shift;

                    uint currentPC = state->PC - 4;
                    uint target = (currentPC + (uint)seI16) & 0x3FFFC;

                    state->PC = target;
                }
            }
        }

        private static void Stqr(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword Instruction Relative (a-form)
            int rt = instruction.RtLo;
            int i16 = instruction.I16 << 2;

            // Signed 18-bits (?)
            int shift = 32 - 18;
            int seI16 = (i16 << shift) >> shift;

            unsafe
            {
                SpuState* state = spu.State;
                uint address = (state->PC + (uint)seI16) & 0x3FFF0;
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(address, value);
            }
        }

        private static void Br(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Relative
            unsafe
            {
                SpuState* state = spu.State;

                // Offset is signed 18-bits
                int i16 = instruction.I16 << 2;
                int shift = 32 - 18;
                int seI16 = (i16 << shift) >> shift;

                uint currentPC = state->PC - 4;
                uint target = (currentPC + (uint)seI16) & 0x3FFFC;

                state->PC = target;
            }
        }

        private static void Fsmbi(SpuThread spu, SpuInstruction instruction)
        {
            // Form Select Mask for Bytes Immediate
            int rt = instruction.RtLo;
            int i16 = instruction.I16;

            // Look up the value in the table
            ReadOnlySpan<UInt128> lut = SpuTables.FsmbTable;

            unsafe
            {
                SpuState* state = spu.State;
                state->GPR[rt].Value128 = lut[i16];
            }
        }

        private static void Brsl(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Relative and Set Link
            int rt = instruction.RtLo;

            // Offset is signed 18-bits
            int i16 = instruction.I16 << 2;
            int shift = 32 - 18;
            int seI16 = (i16 << shift) >> shift;

            unsafe
            {
                SpuState* state = spu.State;
                uint currentPC = state->PC - 4;
                uint target = (currentPC + (uint)seI16) & 0x3FFFC;

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = currentPC;

                state->PC = target;
            }
        }

        private static void Lqa(SpuThread spu, SpuInstruction instruction)
        {
            // Load Quadword (a-form)
            int rt = instruction.RtLo;
            int i16 = instruction.I16;
            uint address = (uint)((i16 << 2) & 0x3FFF0);

            unsafe
            {
                SpuState* state = spu.State;
                BigEndian<UInt128> value = spu.ReadStorage<BigEndian<UInt128>>(address);
                state->GPR[rt].Value128 = value;
            }
        }

        private static void Lqr(SpuThread spu, SpuInstruction instruction)
        {
            // Load Quadword Instruction Relative (a-form)
            int rt = instruction.RtLo;
            int i16 = instruction.I16;

            unsafe
            {
                SpuState* state = spu.State;
                uint address = (uint)(state->PC + (i16 << 2)) & 0x3FFF0;
                BigEndian<UInt128> value = spu.ReadStorage<BigEndian<UInt128>>(address);
                state->GPR[rt].Value128 = value;
            }
        }

        private static void Il(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Load Word
            int rt = instruction.RtLo;
            int seI16 = instruction.SeI16;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> rtLanes = state->GPR[rt].Ints;
                Vector128<int> vi16 = Vector128.Create(seI16);
                vi16.CopyTo(rtLanes);
            }
        }

        private static void Ilhu(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Load Halfword Upper
            int rt = instruction.RtLo;
            int i16 = instruction.I16 << 16;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> vi = Vector128.Create(i16);
                vi.CopyTo(rtLanes);
            }
        }

        private static void Ilh(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Load Halfword
            int rt = instruction.RtLo;
            int i16 = instruction.I16;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> vi = Vector128.Create((short)i16);
                vi.CopyTo(rtLanes);
            }
        }
    }
}
