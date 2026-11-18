using PS3Sharp.Common;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        // RI16: 8-bit opcode group
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

        private static void Orhi(SpuThread spu, SpuInstruction instruction)
        {
            // Or Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int imm = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create((short)imm);
                Vector128<short> result = va | vi;

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

        private static void Sfhi(SpuThread spu, SpuInstruction instruction)
        {
            // Subtract from Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create((short)seI10);
                Vector128<short> result = vi - va;

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
            int imm = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create((short)imm);
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

        private static void Ahi(SpuThread spu, SpuInstruction instruction)
        {
            // Add Halfword Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create((short)seI10);
                Vector128<short> result = va + vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Stqd(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            // Offset is signed 14-bits
            int offset = instruction.I10 << 4;
            int shift = 32 - 14;
            int seOffset = (offset << shift) >> shift;

            unsafe
            {
                SpuState* state = spu.State;
                uint baseAddress = state->GPR[ra].UScalarValue;
                uint target = (baseAddress + (uint)seOffset) & 0x3FFF0;
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(target, value);
            }
        }

        private static void Lqd(SpuThread spu, SpuInstruction instruction)
        {
            // Load Quadword (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            // Offset is signed 14-bits
            int offset = instruction.I10 << 4;
            int shift = 32 - 14;
            int seOffset = (offset << shift) >> shift;

            unsafe
            {
                SpuState* state = spu.State;
                uint baseAddress = state->GPR[ra].UScalarValue;
                uint target = (baseAddress + (uint)seOffset) & 0x3FFF0;

                BigEndian<UInt128> value = spu.ReadStorage<BigEndian<UInt128>>(target);
                state->GPR[rt] = SpuRegister.Create(value);
            }
        }

        private static void Xori(SpuThread spu, SpuInstruction instruction)
        {
            // Exclusive Or Word Immediate
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
                Vector128<int> result = va ^ vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Xorhi(SpuThread spu, SpuInstruction instruction)
        {
            // Exclusive Or Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int imm = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create((short)imm);
                Vector128<short> result = va ^ vi;

                result.CopyTo(rtLanes);
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

        private static void Cgthi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Greater Than Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int imm = instruction.I10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vi = Vector128.Create((short)imm);
                Vector128<short> result = Vector128.GreaterThan(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Cgtbi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Greater Than Byte Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int imm = instruction.I10 & 0xFF;

            unsafe
            {
                SpuState* state = spu.State;
                Span<sbyte> raLanes = state->GPR[ra].SBytes;
                Span<sbyte> rtLanes = state->GPR[rt].SBytes;

                Vector128<sbyte> va = Vector128.Create(raLanes);
                Vector128<sbyte> vi = Vector128.Create((sbyte)imm);
                Vector128<sbyte> result = Vector128.GreaterThan(va, vi);

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

        private static void Clgthi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                Vector128<ushort> va = Vector128.Create(raLanes);
                Vector128<ushort> vi = Vector128.Create((ushort)seI10);
                Vector128<ushort> result = Vector128.GreaterThan(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Clgtbi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Byte Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int imm = instruction.I10 & 0xFF;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vi = Vector128.Create((byte)imm);
                Vector128<byte> result = Vector128.GreaterThan(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Hlgti(SpuThread spu, SpuInstruction instruction)
        {
            // Halt If Logically Greater Than Immediate
            int ra = instruction.Ra;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;

                if (state->GPR[ra].UScalarValue > (uint)seI10)
                {
                    // TODO
                    throw new Exception($"SPU thread halted on Hlgti");
                }
            }
        }

        private static void Mpyi(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = (Vector128.Create(raLanes) << 16) >> 16;
                Vector128<int> vi = Vector128.Create(seI10);
                Vector128<int> result = va * vi;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyui(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply Unsigned Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10 & 0xFFFF;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> mask = Vector128.Create(0xFFFFU);
                Vector128<uint> va = Vector128.Create(raLanes) & mask;
                Vector128<uint> vi = Vector128.Create((uint)seI10);
                Vector128<uint> result = va * vi;

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

        private static void Ceqhi(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Equal Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int i10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                Vector128<ushort> va = Vector128.Create(raLanes);
                Vector128<ushort> vi = Vector128.Create((ushort)i10);
                Vector128<ushort> result = Vector128.Equals(va, vi);

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

        private static void Heqi(SpuThread spu, SpuInstruction instruction)
        {
            // Halt If Equal Immediate
            int ra = instruction.Ra;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SpuState* state = spu.State;

                if (state->GPR[ra].ScalarValue == seI10)
                {
                    // TODO
                    throw new Exception($"SPU thread halted on Heqi");
                }
            }
        }
    }
}
