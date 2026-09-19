using static PS3Sharp.Cell.PPU.PPURotationMasks;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRGenerator {
        // Add instructions

        public static void ADDI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ra == 0 ? ctx.Zero : ctx.GetGPR(ra);
            Value b = Constant(ctx, (ulong)instruction.SI_64);

            Value result = Add(ctx, a, b);
            ctx.SetGPR(rt, result);
        }

        public static void ADDIS(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;
            long si = (int)(instruction.SI << 16); // sign extended

            Value a = ra == 0 ? ctx.Zero : ctx.GetGPR(ra);
            Value b = Constant(ctx, (ulong)si);
            Value result = Add(ctx, a, b);
            ctx.SetGPR(rt, result);
        }

        public static void ADDIC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = Constant(ctx, (ulong)instruction.SI_64);
            Value result = CkdAdd(ctx, a, b, out Value carry);
            ctx.SetGPR(rt, result);
            ctx.SetCA(carry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ADDE(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value ca = ctx.GetCA();

            // Add a + b + ca, and handle intermediate carrys
            Value sum1 = CkdAdd(ctx, a, b, out Value carry1);
            Value finalSum = CkdAdd(ctx, sum1, ca, out Value carry2);
            Value finalCarry = Or(ctx, carry1, carry2);

            ctx.SetGPR(rt, finalSum);
            ctx.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(finalSum, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ADD(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value result = Add(ctx, a, b);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ADDC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value result = CkdAdd(ctx, a, b, out Value carry);

            ctx.SetGPR(rt, result);
            ctx.SetCA(carry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ADDZE(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value ca = ctx.GetCA();
            Value result = CkdAdd(ctx, a, ca, out Value carry);

            ctx.SetGPR(rt, result);
            ctx.SetCA(carry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ADDME(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value ca = ctx.GetCA();

            // Add a + ca - 1, and handle intermediate carrys
            Value sum1 = CkdAdd(ctx, a, ca, out Value carry1);
            Value finalSum = CkdAdd(ctx, sum1, ctx.MinusOne, out Value carry2);
            Value finalCarry = Or(ctx, carry1, carry2);

            ctx.SetGPR(rt, finalSum);
            ctx.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(finalSum, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        // Subtract instructions

        public static void SUBF(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value result = Sub(ctx, b, a); // b - a

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SUBFC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value notA = Not(ctx, a);

            // Add ~a + b + 1, and handle intermediate carrys
            Value sum1 = CkdAdd(ctx, notA, b, out Value carry1);
            Value finalSum = CkdAdd(ctx, sum1, ctx.One, out Value carry2);
            Value finalCarry = Or(ctx, carry1, carry2);

            ctx.SetGPR(rt, finalSum);
            ctx.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(finalSum, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SUBFIC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = Constant(ctx, (ulong)instruction.SI_64);
            Value notA = Not(ctx, a);

            // Add ~a + b + 1, and handle intermediate carrys
            Value sum1 = CkdAdd(ctx, notA, b, out Value carry1);
            Value finalSum = CkdAdd(ctx, sum1, ctx.One, out Value carry2);
            Value finalCarry = Or(ctx, carry1, carry2);

            ctx.SetGPR(rt, finalSum);
            ctx.SetCA(finalCarry);
        }

        public static void SUBFE(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value ca = ctx.GetCA();
            Value notA = Not(ctx, a);

            Value sum1 = CkdAdd(ctx, notA, b, out Value carry1);
            Value finalSum = CkdAdd(ctx, sum1, ca, out Value carry2);
            Value finalCarry = Or(ctx, carry1, carry2);

            ctx.SetGPR(rt, finalSum);
            ctx.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(finalSum, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SUBFZE(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value ca = ctx.GetCA();
            Value notA = Not(ctx, a);
            Value result = CkdAdd(ctx, notA, ca, out Value carry);

            ctx.SetGPR(rt, result);
            ctx.SetCA(carry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SUBFME(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value ca = ctx.GetCA();
            Value notA = Not(ctx, a);

            Value sum1 = CkdAdd(ctx, notA, ca, out Value carry1);
            Value finalSum = CkdAdd(ctx, sum1, ctx.MinusOne, out Value carry2);
            Value finalCarry = Or(ctx, carry1, carry2);

            ctx.SetGPR(rt, finalSum);
            ctx.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(finalSum, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        // General logic instructions
        public static void ANDI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value imm = Constant(ctx, (ulong)instruction.UI);
            Value result = And(ctx, s, imm);

            ctx.SetGPR(ra, result);
            ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);  // Always updates CR0
        }

        public static void ANDIS(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value imm = Constant(ctx, ((ulong)instruction.UI) << 16);
            Value result = And(ctx, s, imm);

            ctx.SetGPR(ra, result);
            ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0); // Always updates CR0
        }

        public static void ORI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value imm = Constant(ctx, (ulong)instruction.UI);
            Value result = Or(ctx, s, imm);

            ctx.SetGPR(ra, result);
            // Does not affect CR0
        }

        public static void ORIS(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value imm = Constant(ctx, ((ulong)instruction.UI) << 16);
            Value result = Or(ctx, s, imm);

            ctx.SetGPR(ra, result);
            // Does not affect CR0
        }

        public static void XORI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value imm = Constant(ctx, (ulong)instruction.UI);
            Value result = Xor(ctx, s, imm);

            ctx.SetGPR(ra, result);
            // Does not affect CR0
        }

        public static void XORIS(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value imm = Constant(ctx, ((ulong)instruction.UI) << 16);
            Value result = Xor(ctx, s, imm);

            ctx.SetGPR(ra, result);
            // Does not affect CR0
        }

        public static void AND(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = And(ctx, s, b);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void OR(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = Or(ctx, s, b);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void XOR(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = Xor(ctx, s, b);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void NAND(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = Not(ctx, And(ctx, b, s));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void NOR(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = Not(ctx, Or(ctx, b, s));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void EQV(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = Not(ctx, Xor(ctx, b, s));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ANDC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = And(ctx, s, Not(ctx, b));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void ORC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = ctx.GetGPR(rs);
            Value b = ctx.GetGPR(rb);
            Value result = Or(ctx, s, Not(ctx, b));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void NEG(IRContext ctx, PPUInstruction instruction) {
            // RT = ¬(RA) + 1
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value notA = Not(ctx, a);
            Value result = Add(ctx, notA, ctx.One);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        // Condition register bit logic opcodes
        public static (Value ba, Value bb) GetBitsAB(IRContext ctx, PPUInstruction instruction) {
            //Helper for condition register bit opcodes
            int bitA = (int)(31 - instruction.Ba);
            int bitB = (int)(31 - instruction.Bb);

            Value cr = ctx.GetCR();
            Value a = ReadBit(ctx, cr, bitA);
            Value b = ReadBit(ctx, cr, bitB);
            return (a, b);
        }

        public static void CROR(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = Or(ctx, bitA, bitB);
            ctx.WriteCRBit(bt, result);
        }

        public static void CRORC(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = Or(ctx, bitA, Not(ctx, bitB));
            ctx.WriteCRBit(bt, result);
        }

        public static void CRAND(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = And(ctx, bitA, bitB);
            ctx.WriteCRBit(bt, result);
        }

        public static void CRANDC(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = And(ctx, bitA, Not(ctx, bitB));
            ctx.WriteCRBit(bt, result);
        }

        public static void CRXOR(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = Xor(ctx, bitA, bitB);
            ctx.WriteCRBit(bt, result);
        }

        public static void CRNAND(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = Not(ctx, And(ctx, bitA, bitB));
            ctx.WriteCRBit(bt, result);
        }

        public static void CRNOR(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = Not(ctx, Or(ctx, bitA, bitB));
            ctx.WriteCRBit(bt, result);
        }

        public static void CREQV(IRContext ctx, PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(ctx, instruction);
            Value result = Not(ctx, Xor(ctx, bitA, bitB));
            ctx.WriteCRBit(bt, result);
        }

        // Multiply instructions
        public static void MULLI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = Constant(ctx, (ulong)instruction.SI_64);
            Value result = IMul(ctx, a, b);

            ctx.SetGPR(rt, result);
            // No special registers affected
        }

        public static void MULLW(IRContext ctx, PPUInstruction instruction) {
            // Multiply word, write 64-bit result (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a32 = Truncate(ctx, a, Value.ValueType.I32);
            Value b32 = Truncate(ctx, b, Value.ValueType.I32);

            Value a64 = SignExtend(ctx, a32, Value.ValueType.I64);
            Value b64 = SignExtend(ctx, b32, Value.ValueType.I64);

            Value result = IMul(ctx, a64, b64);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void MULHW(IRContext ctx, PPUInstruction instruction) {
            // Multiply word, write high 32-bit result (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a32 = Truncate(ctx, a, Value.ValueType.I32);
            Value b32 = Truncate(ctx, b, Value.ValueType.I32);

            Value a64 = SignExtend(ctx, a32, Value.ValueType.I64);
            Value b64 = SignExtend(ctx, b32, Value.ValueType.I64);

            Value value = IMul(ctx, a64, b64);
            Value shiftAmount = Constant(ctx, 32);
            Value result = ShiftRight(ctx, value, shiftAmount);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void MULHWU(IRContext ctx, PPUInstruction instruction) {
            // Multiply word, write high 32-bit (unsigned)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a32 = Truncate(ctx, a, Value.ValueType.I32);
            Value b32 = Truncate(ctx, b, Value.ValueType.I32);

            Value a64 = ZeroExtend(ctx, a32, Value.ValueType.I64);
            Value b64 = ZeroExtend(ctx, b32, Value.ValueType.I64);

            Value value = Mul(ctx, a64, b64);
            Value shiftAmount = Constant(ctx, 32);
            Value result = ShiftRight(ctx, value, shiftAmount);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void MULLD(IRContext ctx, PPUInstruction instruction) {
            // Multiply double word (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value result = IMul(ctx, a, b);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void MULHD(IRContext ctx, PPUInstruction instruction) {
            // Multiply double word, write high 64-bit (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a128 = SignExtend(ctx, a, Value.ValueType.I128);
            Value b128 = SignExtend(ctx, b, Value.ValueType.I128);

            Value value = IMul(ctx, a128, b128);
            Value shiftAmount = Constant(ctx, 64);
            Value shiftedValue = ShiftRight(ctx, value, shiftAmount);
            Value result = Truncate(ctx, shiftedValue, Value.ValueType.I64);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void MULHDU(IRContext ctx, PPUInstruction instruction) {
            // Multiply double word, write high 64-bit (unsigned)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a128 = ZeroExtend(ctx, a, Value.ValueType.I128);
            Value b128 = ZeroExtend(ctx, b, Value.ValueType.I128);

            Value value = Mul(ctx, a128, b128);
            Value shiftAmount = Constant(ctx, 64);
            Value shiftedValue = ShiftRight(ctx, value, shiftAmount);
            Value result = Truncate(ctx, shiftedValue, Value.ValueType.I64);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        // Divide instructions
        public static void DIVD(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value result = CheckedIDiv(ctx, a, b);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void DIVDU(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);
            Value result = CheckedDiv(ctx, a, b);

            ctx.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void DIVW(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a32 = Truncate(ctx, a, Value.ValueType.I32);
            Value b32 = Truncate(ctx, b, Value.ValueType.I32);

            Value result32 = CheckedIDiv(ctx, a32, b32);
            Value result64 = ZeroExtend(ctx, result32, Value.ValueType.I64);

            ctx.SetGPR(rt, result64);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result64, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void DIVWU(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            Value a32 = Truncate(ctx, a, Value.ValueType.I32);
            Value b32 = Truncate(ctx, b, Value.ValueType.I32);

            Value result32 = CheckedDiv(ctx, a32, b32);
            Value result64 = ZeroExtend(ctx, result32, Value.ValueType.I64);

            ctx.SetGPR(rt, result64);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result64, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static (Value shiftAmount, Value isGreaterThanMaxShift) GetShiftAmount(IRContext ctx, Value shiftRegister, Value.ValueType destinationType) {
            if (destinationType != Value.ValueType.I64 && destinationType != Value.ValueType.I32) {
                throw new Exception($"IR unsupported shift type: {destinationType}");
            }

            Value maxShift;
            Value shiftMask;
            Value shiftAmount;
            Value isGreaterThanMaxShift;

            ulong bitCount = destinationType == Value.ValueType.I64 ? 63u : 31u;
            ulong mask = destinationType == Value.ValueType.I64 ? 0x7F : 0x3Fu;

            maxShift = Constant(ctx, bitCount);
            shiftMask = Constant(ctx, mask);

            shiftAmount = And(ctx, shiftRegister, shiftMask);
            isGreaterThanMaxShift = SetGreaterThanUnsigned(ctx, shiftAmount, maxShift);

            return (shiftAmount, isGreaterThanMaxShift);
        }

        public static Value GetShiftedOutCA(IRContext ctx, Value value, Value shiftAmount, Value isGreaterThanMax) {
            // Get the sign from the original value before extending
            Value sign = GetSign(ctx, value);
            Value value64;

            if (value.Type != Value.ValueType.I64) {
                // Shifted-out bits are examined as a raw bit pattern,
                // so widen without changing the low bits.
                value64 = ZeroExtend(ctx, value, Value.ValueType.I64);

            } else {
                value64 = value;
            }
 
            // We must not perform an invalid shift when shiftAmount > max.
            Value safeShift = Select(ctx, isGreaterThanMax, ctx.Zero, shiftAmount);

            // mask = (1 << safeShift) - 1
            Value one = Constant(ctx, 1);
            Value shiftedOne = ShiftLeft(ctx, one, safeShift);
            Value mask = Sub(ctx, shiftedOne, one);

            Value shiftedBits = And(ctx, value64, mask);
            Value hasOnes = SetNotEqual(ctx, shiftedBits, ctx.Zero);

            // For shift > max, every bit is shifted out, so CA = sign.
            Value normalCA = And(ctx, sign, hasOnes);
            Value largeShiftCA = sign;

            return Select(ctx, isGreaterThanMax, largeShiftCA, normalCA);
        }

        public static Value GetSign(IRContext ctx, Value value) {
            // Make sure we're comparing same size
            Value zero = Constant(ctx, 0, value.Type);
            Value isNegative = SetLessThanSigned(ctx, value, zero);

            // The result however, is 64-bit
            return Select(ctx, isNegative, ctx.One, ctx.Zero);
        }

        // Shift instructions
        public static void SLD(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = ctx.GetGPR(rb);
            Value srcReg = ctx.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(ctx, shiftReg, srcReg.Type);
            Value shiftedValue64 = ShiftLeft(ctx, srcReg, shiftAmount);
            Value result = Select(ctx, isGreaterThanMax, ctx.Zero, shiftedValue64);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SRD(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = ctx.GetGPR(rb);
            Value srcReg = ctx.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(ctx, shiftReg, srcReg.Type);
            Value shiftedValue64 = ShiftRight(ctx, srcReg, shiftAmount);
            Value result = Select(ctx, isGreaterThanMax, ctx.Zero, shiftedValue64);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SLW(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftAmountReg = ctx.GetGPR(rb);
            Value srcReg32 = Truncate(ctx, ctx.GetGPR(rs), Value.ValueType.I32);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(ctx, shiftAmountReg, srcReg32.Type);
            Value shiftAmount32 = Truncate(ctx, shiftAmount, Value.ValueType.I32);

            Value shiftedValue32 = ShiftLeft(ctx, srcReg32, shiftAmount32);
            Value shiftedValue64 = ZeroExtend(ctx, shiftedValue32, Value.ValueType.I64);     
            Value result = Select(ctx, isGreaterThanMax, ctx.Zero, shiftedValue64);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SRW(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftAmountReg = ctx.GetGPR(rb);
            Value srcReg32 = Truncate(ctx, ctx.GetGPR(rs), Value.ValueType.I32);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(ctx, shiftAmountReg, srcReg32.Type);
            Value shiftAmount32 = Truncate(ctx, shiftAmount, Value.ValueType.I32);

            Value shiftedValue32 = ShiftRight(ctx, srcReg32, shiftAmount32);
            Value shiftedValue64 = ZeroExtend(ctx, shiftedValue32, Value.ValueType.I64);     
            Value result = Select(ctx, isGreaterThanMax, ctx.Zero, shiftedValue64);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SRAD(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = ctx.GetGPR(rb);
            Value srcReg = ctx.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(ctx, shiftReg, srcReg.Type);
            Value shiftedValue = ShiftRightArithmetic(ctx, srcReg, shiftAmount);
            Value sign = GetSign(ctx, srcReg);

            // For arithmetic shift if shift > 63, fill based on sign.
            Value fill =  Select(ctx, sign, ctx.MinusOne, ctx.Zero);
            Value result = Select(ctx, isGreaterThanMax, fill, shiftedValue);
            Value ca = GetShiftedOutCA(ctx, srcReg, shiftAmount, isGreaterThanMax);

            ctx.SetGPR(ra, result);
            ctx.SetCA(ca);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SRADI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            // SH is 6 bits
            Value shiftAmount = Constant(ctx, (ulong)instruction.SH); 

            Value srcReg = ctx.GetGPR(rs);
            Value result = ShiftRightArithmetic(ctx, srcReg, shiftAmount);

            Value ca;

            if (instruction.SH > 0) {
                Value mask = Constant(ctx,(ulong)((1L << instruction.SH) - 1));

                Value shiftedBits = And(ctx, srcReg, mask);
                Value hasOnes = SetNotEqual(ctx, shiftedBits, ctx.Zero);
                Value sign = GetSign(ctx, srcReg);

                ca = And(ctx, sign, hasOnes);

            } else {
                ca = ctx.Zero;
            }

            ctx.SetGPR(ra, result);
            ctx.SetCA(ca);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SRAW(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = ctx.GetGPR(rb);
            Value srcReg64 = ctx.GetGPR(rs);
            Value srcReg32 = Truncate(ctx, srcReg64, Value.ValueType.I32);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(ctx, shiftReg, srcReg32.Type);
            Value shiftAmount32 = Truncate(ctx, shiftAmount, Value.ValueType.I32);

            Value shiftedValue32 = ShiftRightArithmetic(ctx, srcReg32, shiftAmount32);
            Value shiftedValue64 = SignExtend(ctx, shiftedValue32, Value.ValueType.I64);

            Value sign = GetSign(ctx, srcReg32);

            // For arithmetic shift if shift > 31, fill based on sign.
            // We use 64-bit operands here

            Value fill =  Select(ctx, sign, ctx.MinusOne, ctx.Zero);
            Value result = Select(ctx, isGreaterThanMax, fill, shiftedValue64);
            Value ca = GetShiftedOutCA(ctx, srcReg32, shiftAmount, isGreaterThanMax);

            ctx.SetGPR(ra, result);
            ctx.SetCA(ca);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void SRAWI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            // SH is 5 bits
            int sh = instruction.SH & 0x1F;
            Value shiftAmount = Constant(ctx, (ulong)sh, Value.ValueType.I32);

            Value srcReg = ctx.GetGPR(rs);
            Value srcReg32 = Truncate(ctx, srcReg, Value.ValueType.I32);

            Value result32 = ShiftRightArithmetic(ctx, srcReg32, shiftAmount);
            Value result64 = SignExtend(ctx, result32, Value.ValueType.I64);

            Value ca;

            if (instruction.SH > 0) {
                Value sign = GetSign(ctx, srcReg32); // pass the 32-bit value
                Value mask = Constant(ctx, (ulong)((1L << sh) - 1));

                Value shiftedBits = And(ctx, srcReg, mask);
                Value hasOnes = SetNotEqual(ctx, shiftedBits, ctx.Zero);

                ca = And(ctx, sign, hasOnes);

            } else {
                ca = ctx.Zero;
            }

            ctx.SetGPR(ra, result64);
            ctx.SetCA(ca);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result64, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLDICL(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(instruction.MB_6, 63);

            Value srcReg = ctx.GetGPR(rs);
            Value rotationAmount = Constant(ctx, (ulong)sh);
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLDICR(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(0, instruction.ME_6);

            Value srcReg = ctx.GetGPR(rs);
            Value rotationAmount = Constant(ctx, (ulong)sh);
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLDIC(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(instruction.MB_6, 63 - sh);

            Value srcReg = ctx.GetGPR(rs);
            Value rotationAmount = Constant(ctx, (ulong)sh);
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLDIMI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(instruction.MB_6, 63 - sh);

            Value srcReg = ctx.GetGPR(rs);
            Value raReg = ctx.GetGPR(ra);

            Value rotationAmount = Constant(ctx, (ulong)sh);
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);

            // result = (rotated & mask) | (raReg & (~mask));

            Value maskedResult = And(ctx, rotated, Constant(ctx, mask));
            Value maskedRa = And(ctx, raReg, Constant(ctx, ~mask));
            Value result = Or(ctx, maskedResult, maskedRa);

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLDCL(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_6, 63);

            Value rbReg = ctx.GetGPR(rb);
            Value srcReg = ctx.GetGPR(rs);

            Value rotationAmount = And(ctx, rbReg, Constant(ctx, 0x3FL)); // 6-bits
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLDCR(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(0, instruction.ME_6);

            Value rbReg = ctx.GetGPR(rb);
            Value srcReg = ctx.GetGPR(rs);

            Value rotationAmount = And(ctx, rbReg, Constant(ctx, 0x3FL)); // 6-bits
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLWIMI(IRContext ctx, PPUInstruction instruction) {
            int n = instruction.SH & 0x1F; //Rotate 32 takes only 5 bits of SH

            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            Value srcReg = ctx.GetGPR(rs);
            Value raReg = ctx.GetGPR(ra);

            Value rotationAmount = Constant(ctx, n);
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);

            Value rotatedMasked = And(ctx, rotated, Constant(ctx, mask));
            Value raMasked = And(ctx, raReg, Constant(ctx, ~mask));

            Value result = Or(ctx, rotatedMasked, raMasked);
            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLWINM(IRContext ctx, PPUInstruction instruction) {
            int n = instruction.SH & 0x1F; //Rotate 32 takes only 5 bits of SH

            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            Value srcReg = ctx.GetGPR(rs);

            Value rotationAmount = Constant(ctx, n);
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }

        public static void RLWNM(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            Value srcReg = ctx.GetGPR(rs);
            Value rbReg = ctx.GetGPR(rb);

            Value rotationAmount = And(ctx, rbReg, Constant(ctx, 0x1F)); //Rotate 32 takes only 5 bits of SH
            Value rotated = RotateLeft(ctx, srcReg, rotationAmount);
            Value result = And(ctx, rotated, Constant(ctx, mask));

            ctx.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(result, ctx.Zero, PPU.CRNumber.CR0);
            }
        }
    }
}
