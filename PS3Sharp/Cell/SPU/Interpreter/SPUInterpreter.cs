using PS3Sharp.Common;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal class SpuInterpreter
    {
        internal const int PreferredSlot = 3;

        internal static void Step(SpuThread spu)
        {
            SpuInstruction instruction = spu.ReadInstruction();
            HandleInstruction(spu, instruction);
        }

        internal static void HandleInstruction(SpuThread spu, SpuInstruction instruction)
        {
            GroupRR17(spu, instruction);
        }

        internal static void GroupRR17(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RR17Op)
            {
                case RR17.Sync:
                    // Sync: Synchronize
                    break;

                case RR17.DSync:
                    // DSync: Synchronize Data
                    break;

                case RR17.Rdch:
                    Rdch(spu, instruction);
                    break;

                case RR17.Sf:
                    Sf(spu, instruction);
                    break;

                case RR17.Or:
                    Or(spu, instruction);
                    break;

                case RR17.Rotm:
                    Rotm(spu, instruction);
                    break;

                case RR17.Shl:
                    Shl(spu, instruction);
                    break;

                case RR17.Roti:
                    Roti(spu, instruction);
                    break;

                case RR17.Rotmi:
                    Rotmi(spu, instruction);
                    break;

                case RR17.Shli:
                    Shli(spu, instruction);
                    break;

                case RR17.A:
                    A(spu, instruction);
                    break;

                case RR17.And:
                    And(spu, instruction);
                    break;

                case RR17.Cg:
                    Cg(spu, instruction);
                    break;

                case RR17.Wrch:
                    Wrch(spu, instruction);
                    break;

                case RR17.Bisl:
                    Bisl(spu, instruction);
                    break;

                case RR17.Gb:
                    Gb(spu, instruction);
                    break;

                case RR17.Gbb:
                    Gbb(spu, instruction);
                    break;

                case RR17.Fsm:
                    Fsm(spu, instruction);
                    break;

                case RR17.Fsmb:
                    Fsmb(spu, instruction);
                    break;

                case RR17.Cbx:
                    Cbx(spu, instruction);
                    break;

                case RR17.Cbd:
                    Cbd(spu, instruction);
                    break;

                case RR17.Cwd:
                    Cwd(spu, instruction);
                    break;

                case RR17.Cdd:
                    Cdd(spu, instruction);
                    break;

                case RR17.Rotqbyi:
                    Rotqbyi(spu, instruction);
                    break;

                case RR17.Rotqmbyi:
                    Rotqmbyi(spu, instruction);
                    break;

                case RR17.Shlqbyi:
                    Shlqbyi(spu, instruction);
                    break;

                case RR17.Clz:
                    Clz(spu, instruction);
                    break;

                case RR17.Xsbh:
                    Xsbh(spu, instruction);
                    break;

                case RR17.Andc:
                    Andc(spu, instruction);
                    break;

                case RR17.Clgtb:
                    Clgtb(spu, instruction);
                    break;

                case RR17.Addx:
                    Addx(spu, instruction);
                    break;

                case RR17.Ceq:
                    Ceq(spu, instruction);
                    break;

                case RR17.Ceqb:
                    Ceqb(spu, instruction);
                    break;

                default:
                    GroupRI16(spu, instruction);
                    break;
            }
        }

        internal static void GroupRI16(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RI16Op)
            {
                case RI16.Stqa:
                    Stqa(spu, instruction);
                    break;

                case RI16.Brnz:
                    Brnz(spu, instruction);
                    break;

                case RI16.Stqr:
                    Stqr(spu, instruction);
                    break;

                case RI16.Fsmbi:
                    Fsmbi(spu, instruction);
                    break;

                case RI16.Brsl:
                    Brsl(spu, instruction);
                    break;

                case RI16.Lqa:
                    Lqa(spu, instruction);
                    break;

                case RI16.Lqr:
                    Lqr(spu, instruction);
                    break;

                case RI16.Il:
                    Il(spu, instruction);
                    break;

                case RI16.Ilhu:
                    Ilhu(spu, instruction);
                    break;

                case RI16.Ilh:
                    Ilh(spu, instruction);
                    break;

                default:
                    GroupRI10(spu, instruction);
                    break;
            }
        }

        internal static void GroupRI10(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RI10Op)
            {
                case RI10.Ori:
                    Ori(spu, instruction);
                    break;

                case RI10.Sfi:
                    Sfi(spu, instruction);
                    break;

                case RI10.Andi:
                    Andi(spu, instruction);
                    break;

                case RI10.Andhi:
                    Andhi(spu, instruction);
                    break;

                case RI10.Andbi:
                    Andbi(spu, instruction);
                    break;

                case RI10.Ai:
                    Ai(spu, instruction);
                    break;

                case RI10.Stqd:
                    Stqd(spu, instruction);
                    break;

                case RI10.Cgti:
                    Cgti(spu, instruction);
                    break;

                case RI10.Clgti:
                    Clgti(spu, instruction);
                    break;

                case RI10.Clgtbi:
                    Clgtbi(spu, instruction);
                    break;

                case RI10.Ceqi:
                    Ceqi(spu, instruction);
                    break;

                case RI10.Ceqbi:
                    Ceqbi(spu, instruction);
                    break;

                default:
                    GroupRI18(spu, instruction);
                    break;
            }
        }

        internal static void GroupRI18(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RI18Op)
            {
                case RI18.Hbrr:
                    // HBRR: Hint for Branch Relative
                    break;

                case RI18.Ila: 
                    Ila(spu, instruction);
                    break;

                default:
                    GroupRRROp(spu, instruction);
                    break;
            }
        }

        internal static void GroupRRROp(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RRROp)
            {
                case RRR.Selb:
                    Selb(spu, instruction);
                    break;

                case RRR.ShufB:
                    ShufB(spu, instruction);
                    break;

                default:
                    unsafe
                    {
                        string op = GetInstructionOpcodes(instruction);
                        uint address = spu.State->PC - 4;
                        throw new NotImplementedException($"[SPUInterpreter][{spu.ThreadName}] Unhandled instruction at 0x{address:X} - {op}");
                    }
            }
        }

        private static void Ila(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Load Address
            int rt = instruction.RtLo;
            int i18 = instruction.I18;

            unsafe
            {
                SPUState* state = spu.State;
                Span<int> rtLanes = state->GPR[rt].Ints;
                Vector128<int> vi18 = Vector128.Create(i18);
                vi18.CopyTo(rtLanes);
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                Span<short> rtLanes = state->GPR[rt].Shorts;

                Vector128<short> vi = Vector128.Create((short)i16);
                vi.CopyTo(rtLanes);
            }
        }

        private static void Stqa(SpuThread spu, SpuInstruction instruction)
        {
            // Store Quadword (a-form)
            int rt = instruction.RtLo;

            // Offset is 18-bits
            int i16 = instruction.I16 << 2;

            unsafe
            {
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;

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
                SPUState* state = spu.State;
                uint address = (state->PC + (uint)seI16) & 0x3FFF0;
                BigEndian<UInt128> value = state->GPR[rt].Value128;
                spu.WriteStorage(address, value);
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                // Rotate to the right to do the big endian left rotation
                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> shuffle = Vector128.LoadUnsafe(ref SPUTables.RightRotationTableRef, (nuint)position);  // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
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
                SPUState* state = spu.State;

                if (shiftCount < 16)
                {
                    Span<byte> raLanes = state->GPR[ra].Bytes;
                    Span<byte> rtLanes = state->GPR[rt].Bytes;

                    // Shift to the left to do the big endian right shift
                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(ref SPUTables.LeftShiftTableRef, (nuint)position); // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
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
                SPUState* state = spu.State;

                if (shiftCount < 16)
                {
                    Span<byte> raLanes = state->GPR[ra].Bytes;
                    Span<byte> rtLanes = state->GPR[rt].Bytes;

                    // Shift to the right to do the big endian left shift
                    // Note: the shift table depends on x86 PSHUFB semanticss

                    Vector128<byte> va = Vector128.Create(raLanes);
                    Vector128<byte> shuffle = Vector128.LoadUnsafe(ref SPUTables.RightShiftTableRef, (nuint)position); // Note: In .NET 11 we can replace LoadUnsafe() with Create() from Span.Slice() without bounds check
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);
                Vector128<byte> result = Vector128.Equals(va, vb);

                result.CopyTo(rtLanes);
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
                SPUState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + state->GPR[rb].Ints[PreferredSlot]) & 0xF;
                int bytePosition = 15 - t; // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].Bytes[bytePosition] = 0x03;
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
                SPUState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + seI7) & 0xF;
                int bytePosition = 15 - t; // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].Bytes[bytePosition] = 0x03;
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
                SPUState* state = spu.State;
                int t = (state->GPR[ra].Ints[PreferredSlot] + seI7) & 0xC;
                int wPosition = 3 - (t >> 2); // Big endian indexing

                state->GPR[rt].Value128 = new(0x1011121314151617, 0x18191A1B1C1D1E1F);
                state->GPR[rt].UInts[wPosition] = 0x00010203;
            }
        }

        private static void Selb(SpuThread spu, SpuInstruction instruction)
        {
            // Select Bits
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rc = instruction.Rc;
            int rt = instruction.RtHi;

            unsafe
            {
                SPUState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rcLanes = state->GPR[rc].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);
                Vector128<byte> vc = Vector128.Create(rcLanes);

                // The required operations is:
                // If the bit in register RC is ‘0’, then select the bit from register RA; otherwise, select the bit from register RB.

                Vector128<byte> result = Vector128.ConditionalSelect(vc, vb, va);

                result.CopyTo(rtLanes);
            }
        }

        private static void ShufB(SpuThread spu, SpuInstruction instruction)
        {
            // Shuffle Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rc = instruction.Rc;
            int rt = instruction.RtHi;

            unsafe
            {
                SPUState* state = spu.State;

                // Concatenate RA and RB
                Span<byte> ab = stackalloc byte[32];
                state->GPR[rb].Bytes.CopyTo(ab);
                state->GPR[ra].Bytes.CopyTo(ab.Slice(16));

                // Shuffle RT based on rc
                Span<byte> rcLanes = state->GPR[rc].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                for (int i = 0; i < rcLanes.Length; i++)
                {
                    byte selector = rcLanes[i];
                    byte result;

                    // Special cases
                    if ((selector >> 6) == 0x2)
                    {
                        result = 0x00;
                    }
                    else if ((selector >> 5) == 0x6)
                    {
                        result = 0xFF;
                    }
                    else if ((selector >> 5) == 0x7)
                    {
                        result = 0x80;
                    }
                    else
                    {
                        // Select from AB
                        int idx = 31 - (rcLanes[i] & 0x1F); // Big endian indexing
                        result = ab[idx];
                    }

                    rtLanes[i] = result;
                }
            }
        }

        private static void Fsmbi(SpuThread spu, SpuInstruction instruction)
        {
            // Form Select Mask for Bytes Immediate
            int rt = instruction.RtLo;
            int i16 = instruction.I16;

            // Look up the value in the table
            ReadOnlySpan<UInt128> lut = SPUTables.FsmbTable;

            unsafe
            {
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                uint currentPC = state->PC - 4;
                uint target = (currentPC + (uint)seI16) & 0x3FFFC;

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = currentPC;
                state->PC = target;
            }
        }

        private static void Ori(SpuThread spu, SpuInstruction instruction)
        {
            // Or Word Immediate
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int seI10 = instruction.SeI10;

            unsafe
            {
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vi = Vector128.Create(imm);
                Vector128<byte> result = Vector128.Equals(va, vi);

                result.CopyTo(rtLanes);
            }
        }

        private static void Rdch(SpuThread spu, SpuInstruction instruction)
        {
            // Read Channel
            int rt = instruction.RtLo;
            int ca = instruction.Ca;

            unsafe
            {
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<int> rbLanes = state->GPR[rb].Ints;
                Span<int> rtLanes = state->GPR[rt].Ints;

                Vector128<int> va = Vector128.Create(raLanes);
                Vector128<int> vb = Vector128.Create(rbLanes);
                Vector128<int> result = va | vb;

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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;

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
                SPUState* state = spu.State;

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
                SPUState* state = spu.State;

                // Special cases if shift == 0 or shift > 31
                if (shift == 0)
                {
                    state->GPR[rt].Value128 = state->GPR[ra].Value128;
                }
                else if(shift > 31)
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
                uint value = state->GPR[rt].UInts[PreferredSlot];
                spu.WriteChannel(ca, value);
            }
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
                SPUState* state = spu.State;
                uint currentPC = state->PC;
                uint target = state->GPR[ra].UInts[PreferredSlot] & 0x3FFFC;

                state->GPR[rt].Value128 = UInt128.Zero;
                state->GPR[rt].UInts[PreferredSlot] = currentPC;
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
                SPUState* state = spu.State;
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
                SPUState* state = spu.State;
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
            ReadOnlySpan<UInt128> lut = SPUTables.FsmTable;

            unsafe
            {
                SPUState* state = spu.State;
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
            ReadOnlySpan<UInt128> lut = SPUTables.FsmbTable;

            unsafe
            {
                SPUState* state = spu.State;
                int index = state->GPR[ra].Ints[PreferredSlot] & 0xFFFF;
                state->GPR[rt].Value128 = lut[index];
            }
        }

        private static string GetInstructionOpcodes(SpuInstruction instruction)
        {
            int raw = instruction.Raw;
            int rr17 = (int)instruction.RR17Op;
            int ri16 = (int)instruction.RI16Op;
            int ri10 = (int)instruction.RI10Op;
            int ri18 = (int)instruction.RI18Op;
            int rrr = (int)instruction.RRROp;

            string info = $"Instruction Raw = 0x{raw:X8}\n";
            info += $"RR17Op = 0x{rr17:X8} - ";
            info += $"RI16Op = 0x{ri16:X8} - ";
            info += $"RI10Op = 0x{ri10:X8} - ";
            info += $"RI18Op = 0x{ri18:X8} - ";
            info += $"RRR = 0x{rrr:X8}";

            return info;
        }
    }
}
