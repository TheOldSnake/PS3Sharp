using PS3Sharp.Common;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        private static void Ori(SpuThread spu, SpuInstruction instruction)
        {
            // Or Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vi = Vector128.Create(seI10);
                Vector128<int> result = va | vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Sfi(SpuThread spu, SpuInstruction instruction)
        {
            // Subtract from Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vi = Vector128.Create(seI10);
                Vector128<int> result = vi - va;

                result.CopyTo(rtLanes);
            }
        }

        private static void Andi(SpuThread spu, SpuInstruction instruction)
        {
            // And Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int imm = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vi = Vector128.Create(imm);
                Vector128<int> result = va & vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Andhi(SpuThread spu, SpuInstruction instruction)
        {
            // And Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            short imm = (short)instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create(imm);
                Vector128<short> result = va & vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Andbi(SpuThread spu, SpuInstruction instruction)
        {
            // And Byte Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            byte imm = (byte)(instruction.I10 & 0xFF);

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vi = Vector128.Create(imm);
                Vector128<byte> result = va & vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Ai(SpuThread spu, SpuInstruction instruction)
        {
            // Add Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vi = Vector128.Create(seI10);
                Vector128<int> result = va + vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Stqd(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            // Signed 14-bits
            int i10 = instruction.I10 << 4;
            int shift = 32 - 14;
            int seI10 = (i10 << shift) >> shift;

            unsafe
            {
                SpuState* state = spu.State;
                uint baseRa = state->GPR[ra].UInts[PreferredSlot];
                uint address = (baseRa + (uint)seI10) & 0x3FFF0;
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(address, value);
            }
        }

        private static void Cgti(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Greater Than Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Signed comparison
                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vi = Vector128.Create(seI10);
                Vector128<int> result = Vector128.GreaterThan(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Clgti(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                // Unsigned comparison
                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vi = Vector128.Create((uint)seI10);
                Vector128<uint> result = Vector128.GreaterThan(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Clgtbi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Byte Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int i10 = instruction.I10 & 0xFF;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vi = Vector128.Create((byte)i10);
                Vector128<byte> result = Vector128.GreaterThan(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Ceqi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Equal Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vi = Vector128.Create((uint)seI10);
                Vector128<uint> result = Vector128.Equals(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Ceqbi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Equal Byte Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            byte imm = (byte)(instruction.I10 & 0xFF);

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vi = Vector128.Create(imm);
                Vector128<byte> result = Vector128.Equals(va, vi);

                result.CopyTo(rtLanes);
            }
        }
    }
}
