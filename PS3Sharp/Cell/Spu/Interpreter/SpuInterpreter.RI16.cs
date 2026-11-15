using PS3Sharp.Common;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        // RI16: 9-bit opcode group
        private static uint GetRelativeOffsetTarget(uint pc, SpuInstruction instruction)
        {
            // Offset is signed 18-bits relative to the address of the current instruction
            int offset = instruction.I16 << 2;
            int shift = 32 - 18;
            int seOffset = (offset << shift) >> shift;

            uint currentPC = pc - 4;
            uint target = currentPC + (uint)seOffset;

            return target;
        }

        private static uint GetRelativeBranchTarget(uint pc, SpuInstruction instruction)
        {
            return GetRelativeOffsetTarget(pc, instruction) & 0x3FFFC;
        }

        private static uint GetRelativeMemoryTarget(uint pc, SpuInstruction instruction)
        {
            return GetRelativeOffsetTarget(pc, instruction) & 0x3FFF0;
        }

        private static void Brz(SpuThread spu, SpuInstruction instruction)
        {
            // Branch If Zero Word
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;

                if (state->GPR[rt].UInts[PreferredSlot] == 0)
                {
                    state->PC = GetRelativeBranchTarget(state->PC, instruction);
                }
            }
        }

        private static void Stqa(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword (a-form)
            int rt = instruction.RtLo;
            uint target = (uint)((instruction.I16 << 2) & 0x3FFF0);

            unsafe
            {
                SpuState* state = spu.State;
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(target, value);
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
                    state->PC = GetRelativeBranchTarget(state->PC, instruction);
                }
            }
        }

        private static void Brhz(SpuThread spu, SpuInstruction instruction)
        {
            // Branch If Zero Halfword
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;

                if ((state->GPR[rt].UInts[PreferredSlot] & 0xFFFF) == 0)
                {
                    state->PC = GetRelativeBranchTarget(state->PC, instruction);
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
                    state->PC = GetRelativeBranchTarget(state->PC, instruction);
                }
            }
        }

        private static void Stqr(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword Instruction Relative (a-form)
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                uint target = GetRelativeMemoryTarget(state->PC, instruction);
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(target, value);
            }
        }

        private static void Br(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Relative
            unsafe
            {
                SpuState* state = spu.State;
                state->PC = GetRelativeBranchTarget(state->PC, instruction);
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

            unsafe
            {
                SpuState* state = spu.State;

                // Link pc
                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = state->PC;

                // Branch
                state->PC = GetRelativeBranchTarget(state->PC, instruction);
            }
        }

        private static void Lqa(SpuThread spu, SpuInstruction instruction)
        {
            // Load Quadword (a-form)
            int rt = instruction.RtLo;
            uint target = (uint)((instruction.I16 << 2) & 0x3FFF0);

            unsafe
            {
                SpuState* state = spu.State;
                BigEndian<UInt128> value = spu.ReadStorage<BigEndian<UInt128>>(target);
                state->GPR[rt].Value128 = value;
            }
        }

        private static void Lqr(SpuThread spu, SpuInstruction instruction)
        {
            // Load Quadword Instruction Relative (a-form)
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                uint target = GetRelativeMemoryTarget(state->PC, instruction);
                BigEndian<UInt128> value = spu.ReadStorage<BigEndian<UInt128>>(target);
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
                Vector128<int> vi = Vector128.Create(seI16);
                vi.CopyTo(rtLanes);
            }
        }

        private static void Ilhu(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Load Halfword Upper
            int rt = instruction.RtLo;
            int imm = instruction.I16 << 16;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> vi = Vector128.Create(imm);
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

        private static void Iohl(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Or Halfword Lower
            int rt = instruction.RtLo;
            int i16 = instruction.I16;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> vt = Vector128.Create(rtLanes);
                Vector128<int> vi = Vector128.Create(i16);
                Vector128<int> result = vt | vi;

                result.CopyTo(rtLanes);
            }
        }
    }
}
