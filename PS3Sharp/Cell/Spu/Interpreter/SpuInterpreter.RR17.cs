using PS3Sharp.Common;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        // RR17: 11-bit opcode group
        private static void Stop(SpuThread spu, SpuInstruction instruction)
        {
            // Stop and Signal
            int type = instruction.Raw & 0x3FFF;
            spu.Stop(type);
        }

        private static void Rdch(SpuThread spu, SpuInstruction instruction)
        {
            // Read Channel
            int rt = instruction.RtLo;
            int ca = instruction.Ca;

            unsafe
            {
                SpuState* state = spu.State;
                uint value = spu.ReadChannel(ca);

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = value;
            }
        }

        private static void Rdchcnt(SpuThread spu, SpuInstruction instruction)
        {
            // Read Channel Count
            int rt = instruction.RtLo;
            int ca = instruction.Ca;

            unsafe
            {
                SpuState* state = spu.State;
                uint value = spu.ReadChannelCount(ca);

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = value;
            }
        }

        private static void Sf(SpuThread spu, SpuInstruction instruction)
        {
            // Subtract from Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = vb - va;

                result.CopyTo(rtLanes);
            }
        }

        private static void Or(SpuThread spu, SpuInstruction instruction)
        {
            // Or
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va | vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Bg(SpuThread spu, SpuInstruction instruction)
        {
            // Borrow Generate
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vb = Vector128.Create(rbLanes);

                Vector128<uint> mask = Vector128.Create(1u);
                Vector128<uint> result = Vector128.GreaterThanOrEqual(vb, va) & mask;

                result.CopyTo(rtLanes);
            }
        }

        private static void Sfh(SpuThread spu, SpuInstruction instruction)
        {
            // Subtract from Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rbLanes = state->GPR[rb].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vb = Vector128.Create(rbLanes);
                Vector128<short> result = vb - va;

                result.CopyTo(rtLanes);
            }
        }

        private static void Nor(SpuThread spu, SpuInstruction instruction)
        {
            // Nor
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = ~(va | vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Absdb(SpuThread spu, SpuInstruction instruction)
        {
            // Absolute Differences of Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);

                Vector128<byte> max = Vector128.Max(va, vb);
                Vector128<byte> min = Vector128.Min(va, vb);
                Vector128<byte> result = max - min;

                result.CopyTo(rtLanes);
            }
        }

        private static void Rot(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                int rotate0 = rbLanes[0] & 0x1F;
                int rotate1 = rbLanes[1] & 0x1F;
                int rotate2 = rbLanes[2] & 0x1F;
                int rotate3 = rbLanes[3] & 0x1F;

                rtLanes[0] = int.RotateLeft(raLanes[0], rotate0);
                rtLanes[1] = int.RotateLeft(raLanes[1], rotate1);
                rtLanes[2] = int.RotateLeft(raLanes[2], rotate2);
                rtLanes[3] = int.RotateLeft(raLanes[3], rotate3);
            }
        }

        private static void Rotm(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                // Logical right shift

                int shift0 = (0 - rbLanes[0]) & 0x3F;
                int shift1 = (0 - rbLanes[1]) & 0x3F;
                int shift2 = (0 - rbLanes[2]) & 0x3F;
                int shift3 = (0 - rbLanes[3]) & 0x3F;

                rtLanes[0] = shift0 > 31 ? 0 : raLanes[0] >> shift0;
                rtLanes[1] = shift1 > 31 ? 0 : raLanes[1] >> shift1;
                rtLanes[2] = shift2 > 31 ? 0 : raLanes[2] >> shift2;
                rtLanes[3] = shift3 > 31 ? 0 : raLanes[3] >> shift3;
            }
        }

        private static void Rotma(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Algebraic Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Arithmetic right shift

                int shift0 = int.Min((0 - rbLanes[0]) & 0x3F, 31);
                int shift1 = int.Min((0 - rbLanes[1]) & 0x3F, 31);
                int shift2 = int.Min((0 - rbLanes[2]) & 0x3F, 31);
                int shift3 = int.Min((0 - rbLanes[3]) & 0x3F, 31);

                rtLanes[0] = raLanes[0] >> shift0;
                rtLanes[1] = raLanes[1] >> shift1;
                rtLanes[2] = raLanes[2] >> shift2;
                rtLanes[3] = raLanes[3] >> shift3;
            }
        }

        private static void Shl(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                int shift0 = rbLanes[0] & 0x3F;
                int shift1 = rbLanes[1] & 0x3F;
                int shift2 = rbLanes[2] & 0x3F;
                int shift3 = rbLanes[3] & 0x3F;

                rtLanes[0] = shift0 > 31 ? 0 : raLanes[0] << shift0;
                rtLanes[1] = shift1 > 31 ? 0 : raLanes[1] << shift1;
                rtLanes[2] = shift2 > 31 ? 0 : raLanes[2] << shift2;
                rtLanes[3] = shift3 > 31 ? 0 : raLanes[3] << shift3;
            }
        }

        private static void Roth(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rbLanes = state->GPR[rb].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                int rotate0 = rbLanes[0] & 0xF;
                int rotate1 = rbLanes[1] & 0xF;
                int rotate2 = rbLanes[2] & 0xF;
                int rotate3 = rbLanes[3] & 0xF;
                int rotate4 = rbLanes[4] & 0xF;
                int rotate5 = rbLanes[5] & 0xF;
                int rotate6 = rbLanes[6] & 0xF;
                int rotate7 = rbLanes[7] & 0xF;

                rtLanes[0] = ushort.RotateLeft(raLanes[0], rotate0);
                rtLanes[1] = ushort.RotateLeft(raLanes[1], rotate1);
                rtLanes[2] = ushort.RotateLeft(raLanes[2], rotate2);
                rtLanes[3] = ushort.RotateLeft(raLanes[3], rotate3);
                rtLanes[4] = ushort.RotateLeft(raLanes[4], rotate4);
                rtLanes[5] = ushort.RotateLeft(raLanes[5], rotate5);
                rtLanes[6] = ushort.RotateLeft(raLanes[6], rotate6);
                rtLanes[7] = ushort.RotateLeft(raLanes[7], rotate7);
            }
        }

        private static void Rothm(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rbLanes = state->GPR[rb].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                // Logical right shift

                int shift0 = (0 - rbLanes[0]) & 0x1F;
                int shift1 = (0 - rbLanes[1]) & 0x1F;
                int shift2 = (0 - rbLanes[2]) & 0x1F;
                int shift3 = (0 - rbLanes[3]) & 0x1F;
                int shift4 = (0 - rbLanes[4]) & 0x1F;
                int shift5 = (0 - rbLanes[5]) & 0x1F;
                int shift6 = (0 - rbLanes[6]) & 0x1F;
                int shift7 = (0 - rbLanes[7]) & 0x1F;

                rtLanes[0] = (ushort)(shift0 > 15 ? 0 : raLanes[0] >> shift0);
                rtLanes[1] = (ushort)(shift1 > 15 ? 0 : raLanes[1] >> shift1);
                rtLanes[2] = (ushort)(shift2 > 15 ? 0 : raLanes[2] >> shift2);
                rtLanes[3] = (ushort)(shift3 > 15 ? 0 : raLanes[3] >> shift3);
                rtLanes[4] = (ushort)(shift4 > 15 ? 0 : raLanes[4] >> shift4);
                rtLanes[5] = (ushort)(shift5 > 15 ? 0 : raLanes[5] >> shift5);
                rtLanes[6] = (ushort)(shift6 > 15 ? 0 : raLanes[6] >> shift6);
                rtLanes[7] = (ushort)(shift7 > 15 ? 0 : raLanes[7] >> shift7);
            }
        }

        private static void Rotmah(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Algebraic Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rbLanes = state->GPR[rb].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                // Arithmetic right shift

                int shift0 = int.Min((0 - rbLanes[0]) & 0x1F, 15);
                int shift1 = int.Min((0 - rbLanes[1]) & 0x1F, 15);
                int shift2 = int.Min((0 - rbLanes[2]) & 0x1F, 15);
                int shift3 = int.Min((0 - rbLanes[3]) & 0x1F, 15);
                int shift4 = int.Min((0 - rbLanes[4]) & 0x1F, 15);
                int shift5 = int.Min((0 - rbLanes[5]) & 0x1F, 15);
                int shift6 = int.Min((0 - rbLanes[6]) & 0x1F, 15);
                int shift7 = int.Min((0 - rbLanes[7]) & 0x1F, 15);

                rtLanes[0] = (short)(raLanes[0] >> shift0);
                rtLanes[1] = (short)(raLanes[1] >> shift1);
                rtLanes[2] = (short)(raLanes[2] >> shift2);
                rtLanes[3] = (short)(raLanes[3] >> shift3);
                rtLanes[4] = (short)(raLanes[4] >> shift4);
                rtLanes[5] = (short)(raLanes[5] >> shift5);
                rtLanes[6] = (short)(raLanes[6] >> shift6);
                rtLanes[7] = (short)(raLanes[7] >> shift7);
            }
        }

        private static void Shlh(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rbLanes = state->GPR[rb].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                int shift0 = rbLanes[0] & 0x1F;
                int shift1 = rbLanes[1] & 0x1F;
                int shift2 = rbLanes[2] & 0x1F;
                int shift3 = rbLanes[3] & 0x1F;
                int shift4 = rbLanes[4] & 0x1F;
                int shift5 = rbLanes[5] & 0x1F;
                int shift6 = rbLanes[6] & 0x1F;
                int shift7 = rbLanes[7] & 0x1F;

                rtLanes[0] = (ushort)(shift0 > 15 ? 0 : raLanes[0] << shift0);
                rtLanes[1] = (ushort)(shift1 > 15 ? 0 : raLanes[1] << shift1);
                rtLanes[2] = (ushort)(shift2 > 15 ? 0 : raLanes[2] << shift2);
                rtLanes[3] = (ushort)(shift3 > 15 ? 0 : raLanes[3] << shift3);
                rtLanes[4] = (ushort)(shift4 > 15 ? 0 : raLanes[4] << shift4);
                rtLanes[5] = (ushort)(shift5 > 15 ? 0 : raLanes[5] << shift5);
                rtLanes[6] = (ushort)(shift6 > 15 ? 0 : raLanes[6] << shift6);
                rtLanes[7] = (ushort)(shift7 > 15 ? 0 : raLanes[7] << shift7);
            }
        }

        private static void Roti(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int rotation = instruction.SeI7 & 0x1F;

            unsafe
            {
                SpuState* state = spu.State;

                if (rotation != 0)
                {
                    Span<uint> raLanes = state->GPR[ra].UInts;
                    Span<uint> rtLanes = state->GPR[rt].UInts;

                    // ROL(x,n) = (x << n) | (x >> (32−n))
                    Vector128<uint> va = Vector128.Create(raLanes);
                    Vector128<uint> result = (va << rotation) | (va >> (32 - rotation));

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = state->GPR[ra].Value128;
                }
            }
        }

        private static void Rotmi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = (0 - instruction.SeI7) & 0x3F;

            unsafe
            {
                SpuState* state = spu.State;

                if (shiftCount < 32)
                {
                    Span<uint> raLanes = state->GPR[ra].UInts;
                    Span<uint> rtLanes = state->GPR[rt].UInts;

                    // Logical right shift
                    Vector128<uint> va = Vector128.Create(raLanes);
                    Vector128<uint> result = va >> shiftCount;

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Rotmai(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Algebraic Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = int.Min((0 - instruction.SeI7) & 0x3F, 31);

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Arithmetic right shift

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> result = va >> shiftCount;

                result.CopyTo(rtLanes);
            }
        }

        private static void Shli(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shift = instruction.I7 & 0x3F;

            unsafe
            {
                SpuState* state = spu.State;

                if (shift < 32)
                {
                    Span<int> raLanes = state->GPR[ra].Ints;
                    Span<int> rtLanes = state->GPR[rt].Ints;

                    Vector128<int> va = Vector128.Create(raLanes);
                    Vector128<int> result = va << shift;

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Rothi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int rotation = instruction.SeI7 & 0xF;

            unsafe
            {
                SpuState* state = spu.State;

                if (rotation != 0)
                {
                    Span<ushort> raLanes = state->GPR[ra].UShorts;
                    Span<ushort> rtLanes = state->GPR[rt].UShorts;

                    // ROL(x,n) = (x << n) | (x >> (16−n))
                    Vector128<ushort> va = Vector128.Create(raLanes);
                    Vector128<ushort> result = (va << rotation) | (va >> (16 - rotation));

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = state->GPR[ra].Value128;
                }
            }
        }

        private static void Rothmi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = (0 - instruction.SeI7) & 0x1F;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                if (shiftCount < 16)
                {
                    Vector128<ushort> va = Vector128.Create(raLanes);
                    Vector128<ushort> result = va >> shiftCount;

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Rotmahi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Algebraic Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = int.Min((0 - instruction.SeI7) & 0x1F, 15);

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                // Arithmetic right shift

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> result = va >> shiftCount;

                result.CopyTo(rtLanes);
            }
        }

        private static void Shlhi(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Halfword Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = instruction.SeI7 & 0x1F;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                if (shiftCount < 16)
                {
                    Vector128<ushort> va = Vector128.Create(raLanes);
                    Vector128<ushort> result = va << shiftCount;

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void A(SpuThread spu, SpuInstruction instruction)
        {
            // Add Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va + vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void And(SpuThread spu, SpuInstruction instruction)
        {
            // And
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va & vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Cg(SpuThread spu, SpuInstruction instruction)
        {
            // Carry Generate
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                // Add and detect (unsigned) 32-bit carry of va + vb
                Vector128<uint> mask = Vector128.Create(1u);
                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vb = Vector128.Create(rbLanes);

                // Carry if (a + b) < a
                Vector128<uint> added = va + vb;
                Vector128<uint> cmp = Vector128.LessThan(added, va);

                // Convert 0xFFFFFFFF / 0 into 1 / 0
                Vector128<uint> result = cmp & mask;

                result.CopyTo(rtLanes);
            }
        }

        private static void Ah(SpuThread spu, SpuInstruction instruction)
        {
            // Add Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rbLanes = state->GPR[rb].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vb = Vector128.Create(rbLanes);
                Vector128<short> result = va + vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Nand(SpuThread spu, SpuInstruction instruction)
        {
            // Nand
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = ~(va & vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Avgb(SpuThread spu, SpuInstruction instruction)
        {
            // Average Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);
                Vector128<byte> result = (va + vb + Vector128<byte>.One) >> 1;

                result.CopyTo(rtLanes);
            }
        }

        private static void Wrch(SpuThread spu, SpuInstruction instruction)
        {
            // Write Channel
            int rt = instruction.RtLo;
            int ca = instruction.Ca;

            unsafe
            {
                SpuState* state = spu.State;
                uint value = state->GPR[rt].UInts[PreferredSlot];
                spu.WriteChannel(ca, value);
            }
        }

        private static void Biz(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Indirect If Zero
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int e = instruction.E;
            int d = instruction.D;

            unsafe
            {
                SpuState* state = spu.State;

                if (state->GPR[rt].UInts[PreferredSlot] == 0)
                {
                    uint target = state->GPR[ra].UInts[PreferredSlot] & 0x3FFFC;
                    state->PC = target;
                }
            }

            // TODO: Enable/Disable interrupts
        }

        private static void Binz(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Indirect If Not Zero
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int e = instruction.E;
            int d = instruction.D;

            unsafe
            {
                SpuState* state = spu.State;

                if (state->GPR[rt].UInts[PreferredSlot] != 0)
                {
                    uint target = state->GPR[ra].UInts[PreferredSlot] & 0x3FFFC;
                    state->PC = target;
                }
            }

            // TODO: Enable/Disable interrupts
        }

        private static void Stqx(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword (x-form)
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                uint target = (state->GPR[ra].UInts[PreferredSlot] + state->GPR[rb].UInts[PreferredSlot]) & 0x3FFF0;
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(target, value);
            }
        }

        private static void Bi(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Indirect
            int ra = instruction.Ra;
            int e = instruction.E;
            int d = instruction.D;

            unsafe
            {
                SpuState* state = spu.State;
                uint target = state->GPR[ra].UInts[PreferredSlot] & 0x3FFFC;
                state->PC = target;
            }

            // TODO: Enable/Disable interrupts
        }

        private static void Bisl(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Indirect and Set Link
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int e = instruction.E;
            int d = instruction.D;

            unsafe
            {
                SpuState* state = spu.State;

                // Link
                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = state->PC;

                // Branch
                state->PC = state->GPR[ra].UInts[PreferredSlot] & 0x3FFFC;
            }

            // TODO: Enable/Disable interrupts
        }

        private static void Gb(SpuThread spu, SpuInstruction instruction)
        {
            // Gather Bits from Words
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;

                uint value =
                    (raLanes[0] & 1) |
                    ((raLanes[1] & 1) << 1) |
                    ((raLanes[2] & 1) << 2) |
                    ((raLanes[3] & 1) << 3);

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = value;
            }
        }

        private static void Gbh(SpuThread spu, SpuInstruction instruction)
        {
            // Gather Bits from Halfwords
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;

                uint value = (uint)
                   ((raLanes[0] & 1) |
                   ((raLanes[1] & 1) << 1) |
                   ((raLanes[2] & 1) << 2) |
                   ((raLanes[3] & 1) << 3) |
                   ((raLanes[4] & 1) << 4) |
                   ((raLanes[5] & 1) << 5) |
                   ((raLanes[6] & 1) << 6) |
                   ((raLanes[7] & 1) << 7));

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = value;
            }
        }

        private static void Gbb(SpuThread spu, SpuInstruction instruction)
        {
            // Gather Bits from Bytes
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;

                uint value = (uint)
                    ((raLanes[0] & 1) |
                    ((raLanes[1] & 1) << 1) |
                    ((raLanes[2] & 1) << 2) |
                    ((raLanes[3] & 1) << 3) |
                    ((raLanes[4] & 1) << 4) |
                    ((raLanes[5] & 1) << 5) |
                    ((raLanes[6] & 1) << 6) |
                    ((raLanes[7] & 1) << 7) |
                    ((raLanes[8] & 1) << 8) |
                    ((raLanes[9] & 1) << 9) |
                    ((raLanes[10] & 1) << 10) |
                    ((raLanes[11] & 1) << 11) |
                    ((raLanes[12] & 1) << 12) |
                    ((raLanes[13] & 1) << 13) |
                    ((raLanes[14] & 1) << 14) |
                    ((raLanes[15] & 1) << 15));

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = value;
            }
        }

        private static void Fsm(SpuThread spu, SpuInstruction instruction)
        {
            // Form Select Mask for Words
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            // Look up the value in the table
            ReadOnlySpan<UInt128> lut = SpuTables.FsmTable;

            unsafe
            {
                SpuState* state = spu.State;
                int index = state->GPR[ra].Ints[PreferredSlot] & 0xF;
                state->GPR[rt].Value128 = lut[index];
            }
        }

        private static void Fsmh(SpuThread spu, SpuInstruction instruction)
        {
            // Form Select Mask for Halfwords
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            // Look up the value in the table
            ReadOnlySpan<UInt128> lut = SpuTables.FsmhTable;

            unsafe
            {
                SpuState* state = spu.State;
                int index = state->GPR[ra].Ints[PreferredSlot] & 0xFF;
                state->GPR[rt].Value128 = lut[index];
            }
        }

        private static void Fsmb(SpuThread spu, SpuInstruction instruction)
        {
            // Form Select Mask for Bytes
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            // Look up the value in the table
            ReadOnlySpan<UInt128> lut = SpuTables.FsmbTable;

            unsafe
            {
                SpuState* state = spu.State;
                int index = state->GPR[ra].Ints[PreferredSlot] & 0xFFFF;
                state->GPR[rt].Value128 = lut[index];
            }
        }

        private static void Lqx(SpuThread spu, SpuInstruction instruction)
        {
            // Load Quadword (x-form)
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                uint target = (state->GPR[ra].UInts[PreferredSlot] + state->GPR[rb].UInts[PreferredSlot]) & 0x3FFF0;
                BigEndian<UInt128> value = spu.ReadStorage<BigEndian<UInt128>>(target);
                state->GPR[rt].Value128 = value;
            }
        }

        private static void Rotqbybi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Quadword by Bytes from Bit Shift Count
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                // The manual says [25,28], and later uses [24,28] ?
                int rotation = (state->GPR[rb].Ints[PreferredSlot] >> 3) & 0xF;
                int position = rotation * Vector128<byte>.Count; // rotation already masked

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.LeftRotationTableRef, (nuint)position);  // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                result.CopyTo(rtLanes);
            }
        }

        private static void Shlqbybi(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Quadword by Bytes from Bit Shift Count
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                int shiftCount = (state->GPR[rb].Ints[PreferredSlot] >> 3) & 0x1F;
                int position = (shiftCount & 0xF) * Vector128<byte>.Count;

                if (shiftCount < 16)
                {
                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.LeftShiftTableRef, (nuint)position);  // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                    Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Rotqmbybi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Quadword Bytes from Bit Shift Count
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                int shiftCount = (0 - (state->GPR[rb].Ints[PreferredSlot] >> 3)) & 0x1F;
                int position = (shiftCount & 0xF) * Vector128<byte>.Count;

                if (shiftCount < 16)
                {
                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.RightShiftTableRef, (nuint)position); // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                    Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Cbx(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Byte Insertion (x-form)
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + state->GPR[rb].Ints[PreferredSlot]) & 0xF;
                int bytePosition = 15 - t; // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].Bytes[bytePosition] = 0x03;
            }
        }

        private static void Chx(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Halfword Insertion (x-form)
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + state->GPR[rb].Ints[PreferredSlot]) & 0xE;
                int hwPosition = 7 - (t >> 1); // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].UShorts[hwPosition] = 0x0203;
            }
        }

        private static void Cwx(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Word Insertion (x-form)
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + state->GPR[rb].Ints[PreferredSlot]) & 0xC;
                int wPosition = 3 - (t >> 2); // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].UInts[wPosition] = 0x00010203;
            }
        }

        private static void Rotqbi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Quadword by Bits
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int rotation = state->GPR[rb].Ints[PreferredSlot] & 0x7;
                state->GPR[rt].Value128 = UInt128.RotateLeft(state->GPR[ra].Value128, rotation);
            }
        }
        
        private static void Rotqmbi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Quadword by Bits
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int shiftCount = (0 - state->GPR[rb].Ints[PreferredSlot]) & 0x7;
                state->GPR[rt].Value128 = state->GPR[ra].Value128 >> shiftCount;
            }
        }

        private static void Shlqbi(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Quadword by Bits
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int shiftCount = state->GPR[rb].Ints[PreferredSlot] & 0x7;
                state->GPR[rt].Value128 = state->GPR[ra].Value128 << shiftCount;
            }
        }

        private static void Rotqby(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Quadword by Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                int rotation = state->GPR[rb].Ints[PreferredSlot] & 0xF;
                int position = rotation * Vector128<byte>.Count; // rotation already masked

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.LeftRotationTableRef, (nuint)position);  // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                result.CopyTo(rtLanes);
            }
        }

        private static void Rotqmby(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Quadword by Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;

                int shiftCount = (0 - state->GPR[rb].Ints[PreferredSlot]) & 0x1F;
                int position = (shiftCount & 0xF) * Vector128<byte>.Count;

                if (shiftCount < 16)
                {
                    Span<byte> raLanes = state->GPR[ra].Bytes;
                    Span<byte> rtLanes = state->GPR[rt].Bytes;

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.RightShiftTableRef, (nuint)position);  // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                    Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Shlqby(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Quadword by Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                int shiftCount = state->GPR[rb].Ints[PreferredSlot] & 0x1F;
                int position = (shiftCount & 0xF) * Vector128<byte>.Count;

                if (shiftCount < 16)
                {
                    Span<byte> raLanes = state->GPR[ra].Bytes;
                    Span<byte> rtLanes = state->GPR[rt].Bytes;

                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.LeftShiftTableRef, (nuint)position); // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                    Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Orx(SpuThread spu, SpuInstruction instruction)
        {
            // Or Across
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].Ints[PreferredSlot] = raLanes[0] | raLanes[1] | raLanes[2] | raLanes[3];
            }
        }

        private static void Cbd(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Byte Insertion (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI7 = instruction.SeI7;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + seI7) & 0xF;
                int bytePosition = 15 - t; // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].Bytes[bytePosition] = 0x03;
            }
        }

        private static void Chd(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Halfword Insertion (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI7 = instruction.SeI7;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + seI7) & 0xE;
                int hwPosition = 7 - (t >> 1); // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].UShorts[hwPosition] = 0x0203;
            }
        }

        private static void Cwd(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Word Insertion (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI7 = instruction.SeI7;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + seI7) & 0xC;
                int wPosition = 3 - (t >> 2); // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].UInts[wPosition] = 0x00010203;
            }
        }

        private static void Cdd(SpuThread spu, SpuInstruction instruction)
        {
            // Generate Controls for Doubleword Insertion (d-form)
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI7 = instruction.SeI7;

            unsafe
            {
                SpuState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + seI7) & 0x8;
                int dwPosition = 1 - (t >> 3); // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].ULongs[dwPosition] = 0x0001020304050607;
            }
        }

        private static void Rotqbii(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Quadword by Bits Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int rotation = instruction.I7 & 0x7;
            
            unsafe
            {
                SpuState* state = spu.State;
                state->GPR[rt].Value128 = UInt128.RotateLeft(state->GPR[ra].Value128, rotation);
            }
        }

        private static void Rotqmbii(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Quadword by Bits Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = (0 - instruction.I7) & 0x7;

            unsafe
            {
                SpuState* state = spu.State;
                state->GPR[rt].Value128 = state->GPR[ra].Value128 >> shiftCount;
            }
        }

        private static void Shlqbii(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Quadword by Bits Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = instruction.I7 & 0x7;

            unsafe
            {
                SpuState* state = spu.State;
                state->GPR[rt].Value128 = state->GPR[ra].Value128 << shiftCount;
            }
        }

        private static void Rotqbyi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate Quadword by Bytes Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int rotation = instruction.I7 & 0xF;
            int position = rotation * Vector128<byte>.Count; // rotation already masked

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.LeftRotationTableRef, (nuint)position);  // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                result.CopyTo(rtLanes);
            }
        }

        private static void Rotqmbyi(SpuThread spu, SpuInstruction instruction)
        {
            // Rotate and Mask Quadword by Bytes Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shiftCount = (0 - instruction.I7) & 0x1F;
            int position = (shiftCount & 0xF) * Vector128<byte>.Count;

            unsafe
            {
                SpuState* state = spu.State;

                if (shiftCount < 16)
                {
                    Span<byte> raLanes = state->GPR[ra].Bytes;
                    Span<byte> rtLanes = state->GPR[rt].Bytes;

                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.RightShiftTableRef, (nuint)position); // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                    Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Shlqbyi(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Quadword by Bytes Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int i7 = instruction.I7;
            int shiftCount = i7 & 0x1F;
            int position = (shiftCount & 0xF) * Vector128<byte>.Count;

            unsafe
            {
                SpuState* state = spu.State;

                if (shiftCount < 16)
                {
                    Span<byte> raLanes = state->GPR[ra].Bytes;
                    Span<byte> rtLanes = state->GPR[rt].Bytes;

                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(in SpuTables.LeftShiftTableRef, (nuint)position); // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
                    Vector128<byte> result = Vector128.ShuffleNative(va, shuffle);

                    result.CopyTo(rtLanes);
                }
                else
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
            }
        }

        private static void Cgt(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Greater Than Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = Vector128.GreaterThan(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Xor(SpuThread spu, SpuInstruction instruction)
        {
            // Exclusive Or
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va ^ vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Eqv(SpuThread spu, SpuInstruction instruction)
        {
            // Equivalent
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va ^ (~vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Cgth(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Greater Than Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rbLanes = state->GPR[rb].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> vb = Vector128.Create(rbLanes);
                Vector128<short> result = Vector128.GreaterThan(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Cgtb(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Greater Than Byte
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<sbyte> raLanes = state->GPR[ra].SBytes;
                Span<sbyte> rbLanes = state->GPR[rb].SBytes;
                Span<sbyte> rtLanes = state->GPR[rt].SBytes;

                Vector128<sbyte> va = Vector128.Create(raLanes);
                Vector128<sbyte> vb = Vector128.Create(rbLanes);
                Vector128<sbyte> result = Vector128.GreaterThan(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Sumb(SpuThread spu, SpuInstruction instruction)
        {
            // Sum Bytes into Halfwords
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<int> rtLanes = state->GPR[rt].Ints;

                int sumA0 = raLanes[0] + raLanes[1] + raLanes[2] + raLanes[3];
                int sumB0 = rbLanes[0] + rbLanes[1] + rbLanes[2] + rbLanes[3];

                int sumA1 = raLanes[4] + raLanes[5] + raLanes[6] + raLanes[7];
                int sumB1 = rbLanes[4] + rbLanes[5] + rbLanes[6] + rbLanes[7];

                int sumA2 = raLanes[8] + raLanes[9] + raLanes[10] + raLanes[11];
                int sumB2 = rbLanes[8] + rbLanes[9] + rbLanes[10] + rbLanes[11];

                int sumA3 = raLanes[12] + raLanes[13] + raLanes[14] + raLanes[15];
                int sumB3 = rbLanes[12] + rbLanes[13] + rbLanes[14] + rbLanes[15];

                rtLanes[0] = (sumA0 & 0xFFFF) | (sumB0 << 16);
                rtLanes[1] = (sumA1 & 0xFFFF) | (sumB1 << 16);
                rtLanes[2] = (sumA2 & 0xFFFF) | (sumB2 << 16);
                rtLanes[3] = (sumA3 & 0xFFFF) | (sumB3 << 16);
            }
        }

        private static void Clz(SpuThread spu, SpuInstruction instruction)
        {
            // Count Leading Zeros
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                rtLanes[0] = uint.LeadingZeroCount(raLanes[0]);
                rtLanes[1] = uint.LeadingZeroCount(raLanes[1]);
                rtLanes[2] = uint.LeadingZeroCount(raLanes[2]);
                rtLanes[3] = uint.LeadingZeroCount(raLanes[3]);
            }
        }

        private static void Xswd(SpuThread spu, SpuInstruction instruction)
        {
            // Extend Sign Word to Doubleword
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<long> raLanes = state->GPR[ra].Longs;
                Span<long> rtLanes = state->GPR[rt].Longs;

                int shift = 64 - 32;
                Vector128<long> va = Vector128.Create(raLanes);
                Vector128<long> result = (va << shift) >> shift;

                result.CopyTo(rtLanes);
            }
        }

        private static void Xshw(SpuThread spu, SpuInstruction instruction)
        {
            // Extend Sign Halfword to Word
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                int shift = 32 - 16;
                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> result = (va << shift) >> shift;

                result.CopyTo(rtLanes);
            }
        }

        private static void Cntb(SpuThread spu, SpuInstruction instruction)
        {
            // Count Ones in Bytes
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                rtLanes[0] = byte.PopCount(raLanes[0]);
                rtLanes[1] = byte.PopCount(raLanes[1]);
                rtLanes[2] = byte.PopCount(raLanes[2]);
                rtLanes[3] = byte.PopCount(raLanes[3]);
                rtLanes[4] = byte.PopCount(raLanes[4]);
                rtLanes[5] = byte.PopCount(raLanes[5]);
                rtLanes[6] = byte.PopCount(raLanes[6]);
                rtLanes[7] = byte.PopCount(raLanes[7]);
                rtLanes[8] = byte.PopCount(raLanes[8]);
                rtLanes[9] = byte.PopCount(raLanes[9]);
                rtLanes[10] = byte.PopCount(raLanes[10]);
                rtLanes[11] = byte.PopCount(raLanes[11]);
                rtLanes[12] = byte.PopCount(raLanes[12]);
                rtLanes[13] = byte.PopCount(raLanes[13]);
                rtLanes[14] = byte.PopCount(raLanes[14]);
                rtLanes[15] = byte.PopCount(raLanes[15]);
            }
        }

        private static void Xsbh(SpuThread spu, SpuInstruction instruction)
        {
            // Extend Sign Byte to Halfword
            int ra = instruction.Ra;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<short> raLanes = state->GPR[ra].Shorts;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                int shift = 16 - 8;
                Vector128<short> va = Vector128.Create(raLanes);
                Vector128<short> result = (va << shift) >> shift;

                result.CopyTo(rtLanes);
            }
        }

        private static void Clgt(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vb = Vector128.Create(rbLanes);
                Vector128<uint> result = Vector128.GreaterThan(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Andc(SpuThread spu, SpuInstruction instruction)
        {
            // And with Complement
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va & (~vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Clgth(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rbLanes = state->GPR[rb].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                Vector128<ushort> va = Vector128.Create(raLanes);
                Vector128<ushort> vb = Vector128.Create(rbLanes);
                Vector128<ushort> result = Vector128.GreaterThan(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Orc(SpuThread spu, SpuInstruction instruction)
        {
            // Or with Complement
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va | (~vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Clgtb(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Logical Greater Than Byte
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);
                Vector128<byte> result = Vector128.GreaterThan(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Addx(SpuThread spu, SpuInstruction instruction)
        {
            // Add Extended
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> mask = Vector128.Create(1);
                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> vt = Vector128.Create(rtLanes) & mask;
                Vector128<int> result = va + vb + vt;

                result.CopyTo(rtLanes);
            }
        }

        private static void Sfx(SpuThread spu, SpuInstruction instruction)
        {
            // Subtract from Extended
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> mask = Vector128.Create(1);
                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> vt = Vector128.Create(rtLanes) & mask;
                Vector128<int> result = vb + (~va) + vt;

                result.CopyTo(rtLanes);
            }
        }

        private static void Cgx(SpuThread spu, SpuInstruction instruction)
        {
            // Carry Generate Extended
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                // Add and detect (unsigned) 32-bit carry of va + vb + (vt & 1)
                Vector128<uint> mask = Vector128.Create(1u);
                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vb = Vector128.Create(rbLanes);
                Vector128<uint> vt = Vector128.Create(rtLanes) & mask;

                // First addition and carry detection
                Vector128<uint> sum1 = va + vb;
                Vector128<uint> carry1 = Vector128.LessThan(sum1, va);

                // Second addition and carry detection
                Vector128<uint> sum2 = sum1 + vt;
                Vector128<uint> carry2 = Vector128.LessThan(sum2, sum1);

                // Combine carry conditions and convert 0xFFFFFFFF / 0 into 1 / 0
                Vector128<uint> result = (carry1 | carry2) & mask;

                result.CopyTo(rtLanes);
            }
        }

        private static void Bgx(SpuThread spu, SpuInstruction instruction)
        {
            // Borrow Generate Extended
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> mask = Vector128.Create(1);
                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> vt = Vector128.Create(rtLanes) & mask;

                Vector128<int> subtracted = vb - (va + (~vt));
                Vector128<int> result = Vector128.GreaterThanOrEqual(subtracted, Vector128<int>.Zero) & mask;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyhha(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply High High and Add
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Sign extend high 16-bit
                Vector128<int> va = Vector128.Create(raLanes) >> 16;
                Vector128<int> vb = Vector128.Create(rbLanes) >> 16;
                Vector128<int> vt = Vector128.Create(rtLanes);
                Vector128<int> result = (va * vb) + vt;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyhhau(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply High High Unsigned and Add
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> va = Vector128.Create(raLanes) >> 16;
                Vector128<uint> vb = Vector128.Create(rbLanes) >> 16;
                Vector128<uint> vt = Vector128.Create(rtLanes);
                Vector128<uint> result = (va * vb) + vt;

                result.CopyTo(rtLanes);
            }
        }

        private static void Fscrrd(SpuThread spu, SpuInstruction instruction)
        {
            // Floating-Point Status and Control Register Read
            int rt = instruction.RtLo;

            // TODO

            unsafe
            {
                SpuState* state = spu.State;
                state->GPR[rt].Value128 = UInt128.Zero;
            }
        }

        private static void Fscrwr(SpuThread spu, SpuInstruction instruction)
        {
            // Floating-Point Status and Control Register Write
            // TODO
        }

        private static void Ceq(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Equal Word
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = Vector128.Equals(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpy(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Sign extend low 16-bit
                Vector128<int> va = (Vector128.Create(raLanes) << 16) >> 16;
                Vector128<int> vb = (Vector128.Create(rbLanes) << 16) >> 16;
                Vector128<int> result = va * vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyh(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply High
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Sign extend high 16-bit of va and low 16-bit of vb
                Vector128<int> va = Vector128.Create(raLanes) >> 16;
                Vector128<int> vb = (Vector128.Create(rbLanes) << 16) >> 16;
                Vector128<int> result = (va * vb) << 16;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyhh(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply High High
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Sign extend high 16-bit
                Vector128<int> va = Vector128.Create(raLanes) >> 16;
                Vector128<int> vb = Vector128.Create(rbLanes) >> 16;
                Vector128<int> result = va * vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpys(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply and Shift Right
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Sign extend low 16-bit
                Vector128<int> va = (Vector128.Create(raLanes) << 16) >> 16;
                Vector128<int> vb = (Vector128.Create(rbLanes) << 16) >> 16;
                Vector128<int> result = (va * vb) >> 16;

                result.CopyTo(rtLanes);
            }
        }

        private static void Ceqh(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Equal Halfword
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<ushort> raLanes = state->GPR[ra].UShorts;
                Span<ushort> rbLanes = state->GPR[rb].UShorts;
                Span<ushort> rtLanes = state->GPR[rt].UShorts;

                Vector128<ushort> va = Vector128.Create(raLanes);
                Vector128<ushort> vb = Vector128.Create(rbLanes);
                Vector128<ushort> result = Vector128.Equals(va, vb);

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyu(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply Unsigned
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> mask = Vector128.Create(0xFFFFU);
                Vector128<uint> va = Vector128.Create(raLanes) & mask;
                Vector128<uint> vb = Vector128.Create(rbLanes) & mask;
                Vector128<uint> result = va * vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Mpyhhu(SpuThread spu, SpuInstruction instruction)
        {
            // Multiply High High Unsigned
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<uint> rbLanes = state->GPR[rb].UInts;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                Vector128<uint> va = Vector128.Create(raLanes) >> 16;
                Vector128<uint> vb = Vector128.Create(rbLanes) >> 16;
                Vector128<uint> result = va * vb;

                result.CopyTo(rtLanes);
            }
        }

        private static void Ceqb(SpuThread spu, SpuInstruction instruction)
        {
            // Compare Equal Byte
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rt = instruction.RtLo;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);
                Vector128<byte> result = Vector128.Equals(va, vb);

                result.CopyTo(rtLanes);
            }
        }
    }
}
