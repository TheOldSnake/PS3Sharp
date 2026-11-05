using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
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

        private static void Shl(SpuThread spu, SpuInstruction instruction)
        {
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

        private static void Shli(SpuThread spu, SpuInstruction instruction)
        {
            // Shift Left Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int shift = instruction.I7 & 0x3F;

            unsafe
            {
                SpuState* state = spu.State;

                // Special cases if shift == 0 or shift > 31
                if (shift == 0)
                {
                    state->GPR[rt].Value128 = state->GPR[ra].Value128;
                }
                else if (shift > 31)
                {
                    state->GPR[rt].Value128 = UInt128.Zero;
                }
                else
                {
                    Span<int> raLanes = state->GPR[ra].Ints;
                    Span<int> rtLanes = state->GPR[rt].Ints;

                    Vector128<int> va = Vector128.Create(raLanes);
                    Vector128<int> result = Vector128.ShiftLeft(va, shift);

                    result.CopyTo(rtLanes);
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

                // Add and detect (unsigned) 32-bit carry
                Vector128<uint> va = Vector128.Create(raLanes);
                Vector128<uint> vb = Vector128.Create(rbLanes);
                Vector128<uint> mask = Vector128.Create(1u);

                // Carry if (a + b) < a
                Vector128<uint> added = va + vb;
                Vector128<uint> cmp = Vector128.LessThan(added, va);

                // Convert 0xFFFFFFFF / 0 into 1 / 0
                Vector128<uint> result = cmp & mask;

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

        private static void Bi(SpuThread spu, SpuInstruction instruction)
        {
            // Branch Indirect
            int ra = instruction.Ra;
            int e = instruction.E;
            int d = instruction.D;

            unsafe
            {
                SpuState* state = spu.State;
                uint currentPC = state->PC;
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
                uint currentPC = state->PC;
                uint target = state->GPR[ra].UInts[PreferredSlot] & 0x3FFFC;

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = currentPC;

                state->PC = target;
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

                state->GPR[rt].Value128 = value;
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

                state->GPR[rt].Value128 = value;
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
                Vector128<short> result = Vector128.ShiftRightArithmetic(Vector128.ShiftLeft(va, shift), shift);

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
