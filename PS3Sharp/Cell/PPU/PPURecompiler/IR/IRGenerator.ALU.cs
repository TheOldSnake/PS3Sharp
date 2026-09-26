using static PS3Sharp.Cell.PPU.PPURotationMasks;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public partial class IRGenerator {
        // Add instructions

        public void ADDI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ra == 0 ? _irContext.Zero : _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.SI_64);

            Value result = Add(a, b);
            _irContext.SetGPR(rt, result);
        }

        public void ADDIS(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;
            long si = (int)(instruction.SI << 16); // sign extended

            Value a = ra == 0 ? _irContext.Zero : _irContext.GetGPR(ra);
            Value b = Constant((ulong)si);
            Value result = Add(a, b);
            _irContext.SetGPR(rt, result);
        }

        public void ADDIC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.SI_64);
            Value result = CkdAdd(a, b, out Value carry);
            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ADDE(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value ca = _irContext.GetCA();

            // Add a + b + ca, and handle intermediate carrys
            Value sum1 = CkdAdd(a, b, out Value carry1);
            Value finalSum = CkdAdd(sum1, ca, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ADD(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = Add(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ADDC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CkdAdd(a, b, out Value carry);

            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ADDZE(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value ca = _irContext.GetCA();
            Value result = CkdAdd(a, ca, out Value carry);

            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ADDME(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value ca = _irContext.GetCA();

            // Add a + ca - 1, and handle intermediate carrys
            Value sum1 = CkdAdd(a, ca, out Value carry1);
            Value finalSum = CkdAdd(sum1, _irContext.MinusOne, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        // Subtract instructions

        public void SUBF(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = Sub(b, a); // b - a

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SUBFC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value notA = Not(a);

            // Add ~a + b + 1, and handle intermediate carrys
            Value sum1 = CkdAdd(notA, b, out Value carry1);
            Value finalSum = CkdAdd(sum1, _irContext.One, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SUBFIC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.SI_64);
            Value notA = Not(a);

            // Add ~a + b + 1, and handle intermediate carrys
            Value sum1 = CkdAdd(notA, b, out Value carry1);
            Value finalSum = CkdAdd(sum1, _irContext.One, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);
        }

        public void SUBFE(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value ca = _irContext.GetCA();
            Value notA = Not(a);

            Value sum1 = CkdAdd(notA, b, out Value carry1);
            Value finalSum = CkdAdd(sum1, ca, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SUBFZE(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value ca = _irContext.GetCA();
            Value notA = Not(a);
            Value result = CkdAdd(notA, ca, out Value carry);

            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SUBFME(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value ca = _irContext.GetCA();
            Value notA = Not(a);

            Value sum1 = CkdAdd(notA, ca, out Value carry1);
            Value finalSum = CkdAdd(sum1, _irContext.MinusOne, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        // General logic instructions
        public void ANDI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant((ulong)instruction.UI);
            Value result = And(s, imm);

            _irContext.SetGPR(ra, result);
            _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);  // Always updates CR0
        }

        public void ANDIS(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant(((ulong)instruction.UI) << 16);
            Value result = And(s, imm);

            _irContext.SetGPR(ra, result);
            _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0); // Always updates CR0
        }

        public void ORI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant((ulong)instruction.UI);
            Value result = Or(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        public void ORIS(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant(((ulong)instruction.UI) << 16);
            Value result = Or(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        public void XORI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant((ulong)instruction.UI);
            Value result = Xor(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        public void XORIS(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant(((ulong)instruction.UI) << 16);
            Value result = Xor(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        public void AND(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = And(s, b);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void OR(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Or(s, b);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void XOR(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Xor(s, b);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void NAND(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Not(And(b, s));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void NOR(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Not(Or(b, s));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void EQV(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Not(Xor(b, s));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ANDC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = And(s, Not(b));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void ORC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Or(s, Not(b));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void NEG(PPUInstruction instruction) {
            // RT = ¬(RA) + 1
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value notA = Not(a);
            Value result = Add(notA, _irContext.One);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        // Condition register bit logic opcodes
        public (Value ba, Value bb) GetBitsAB(PPUInstruction instruction) {
            //Helper for condition register bit opcodes
            int bitA = (int)(31 - instruction.Ba);
            int bitB = (int)(31 - instruction.Bb);

            Value cr = _irContext.GetCR();
            Value a = ReadBit(cr, bitA);
            Value b = ReadBit(cr, bitB);
            return (a, b);
        }

        public void CROR(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Or(bitA, bitB);
            _irContext.WriteCRBit(bt, result);
        }

        public void CRORC(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Or(bitA, Not(bitB));
            _irContext.WriteCRBit(bt, result);
        }

        public void CRAND(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = And(bitA, bitB);
            _irContext.WriteCRBit(bt, result);
        }

        public void CRANDC(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = And(bitA, Not(bitB));
            _irContext.WriteCRBit(bt, result);
        }

        public void CRXOR(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Xor(bitA, bitB);
            _irContext.WriteCRBit(bt, result);
        }

        public void CRNAND(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Not(And(bitA, bitB));
            _irContext.WriteCRBit(bt, result);
        }

        public void CRNOR(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Not(Or(bitA, bitB));
            _irContext.WriteCRBit(bt, result);
        }

        public void CREQV(PPUInstruction instruction) {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Not(Xor(bitA, bitB));
            _irContext.WriteCRBit(bt, result);
        }

        // Multiply instructions
        public void MULLI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.SI_64);
            Value result = IMulLD(a, b);

            _irContext.SetGPR(rt, result);
            // No special registers affected
        }

        public void MULLW(PPUInstruction instruction) {
            // Multiply word, write 64-bit result (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulLW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void MULHW(PPUInstruction instruction) {
            // Multiply word, write high 32-bit result (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulHW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void MULHWU(PPUInstruction instruction) {
            // Multiply word, write high 32-bit (unsigned)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = MulHW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void MULLD(PPUInstruction instruction) {
            // Multiply double word (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulLD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void MULHD(PPUInstruction instruction) {
            // Multiply double word, write high 64-bit (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulHD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void MULHDU(PPUInstruction instruction) {
            // Multiply double word, write high 64-bit (unsigned)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = MulHD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        // Divide instructions
        public void DIVD(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedIDivD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void DIVDU(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedDivD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void DIVW(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedIDivW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void DIVWU(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedDivW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public (Value shiftAmount, Value isGreaterThanMaxShift) GetShiftAmount(Value shiftRegister, Value.ValueType destinationType) {
            if (destinationType != Value.ValueType.I64 && destinationType != Value.ValueType.I32) {
                throw new Exception($"IR unsupported shift type: {destinationType}");
            }

            Value maxShift;
            Value shiftMask;
            Value shiftAmount;
            Value isGreaterThanMaxShift;

            ulong bitCount = destinationType == Value.ValueType.I64 ? 63u : 31u;
            ulong mask = destinationType == Value.ValueType.I64 ? 0x7F : 0x3Fu;

            maxShift = Constant(bitCount);
            shiftMask = Constant(mask);

            shiftAmount = And(shiftRegister, shiftMask);
            isGreaterThanMaxShift = SetGreaterThanUnsigned(shiftAmount, maxShift);

            return (shiftAmount, isGreaterThanMaxShift);
        }

        public Value GetShiftedOutCA(Value value, Value shiftAmount, Value isGreaterThanMax) {
            // Get the sign from the original value before extending
            Value sign = GetSign(value);
            Value value64;

            if (value.Type != Value.ValueType.I64) {
                // Shifted-out bits are examined as a raw bit pattern,
                // so widen without changing the low bits.
                value64 = ZeroExtend(value, Value.ValueType.I64);

            } else {
                value64 = value;
            }
 
            // We must not perform an invalid shift when shiftAmount > max.
            Value safeShift = Select(isGreaterThanMax, _irContext.Zero, shiftAmount);

            // mask = (1 << safeShift) - 1
            Value one = Constant(1);
            Value shiftedOne = ShiftLeft(one, safeShift);
            Value mask = Sub(shiftedOne, one);

            Value shiftedBits = And(value64, mask);
            Value hasOnes = SetNotEqual(shiftedBits, _irContext.Zero);

            // For shift > max, every bit is shifted out, so CA = sign.
            Value normalCA = And(sign, hasOnes);
            Value largeShiftCA = sign;

            return Select(isGreaterThanMax, largeShiftCA, normalCA);
        }

        public Value GetSign(Value value) {
            // Make sure we're comparing same size
            Value zero = Constant(0, value.Type);
            Value isNegative = SetLessThanSigned(value, zero);

            // The result however, is 64-bit
            return Select(isNegative, _irContext.One, _irContext.Zero);
        }

        // Shift instructions
        public void SLD(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg.Type);
            Value shiftedValue64 = ShiftLeft(srcReg, shiftAmount);
            Value result = Select(isGreaterThanMax, _irContext.Zero, shiftedValue64);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SRD(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg.Type);
            Value shiftedValue64 = ShiftRight(srcReg, shiftAmount);
            Value result = Select(isGreaterThanMax, _irContext.Zero, shiftedValue64);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SLW(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftAmountReg = _irContext.GetGPR(rb);
            Value srcReg32 = Truncate(_irContext.GetGPR(rs), Value.ValueType.I32);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftAmountReg, srcReg32.Type);
            Value shiftAmount32 = Truncate(shiftAmount, Value.ValueType.I32);

            Value shiftedValue32 = ShiftLeft(srcReg32, shiftAmount32);
            Value shiftedValue64 = ZeroExtend(shiftedValue32, Value.ValueType.I64);     
            Value result = Select(isGreaterThanMax, _irContext.Zero, shiftedValue64);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SRW(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftAmountReg = _irContext.GetGPR(rb);
            Value srcReg32 = Truncate(_irContext.GetGPR(rs), Value.ValueType.I32);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftAmountReg, srcReg32.Type);
            Value shiftAmount32 = Truncate(shiftAmount, Value.ValueType.I32);

            Value shiftedValue32 = ShiftRight(srcReg32, shiftAmount32);
            Value shiftedValue64 = ZeroExtend(shiftedValue32, Value.ValueType.I64);     
            Value result = Select(isGreaterThanMax, _irContext.Zero, shiftedValue64);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SRAD(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg.Type);
            Value shiftedValue = ShiftRightArithmetic(srcReg, shiftAmount);
            Value sign = GetSign(srcReg);

            // For arithmetic shift if shift > 63, fill based on sign.
            Value fill =  Select(sign, _irContext.MinusOne, _irContext.Zero);
            Value result = Select(isGreaterThanMax, fill, shiftedValue);
            Value ca = GetShiftedOutCA(srcReg, shiftAmount, isGreaterThanMax);

            _irContext.SetGPR(ra, result);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SRADI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            // SH is 6 bits
            Value shiftAmount = Constant((ulong)instruction.SH); 

            Value srcReg = _irContext.GetGPR(rs);
            Value result = ShiftRightArithmetic(srcReg, shiftAmount);

            Value ca;

            if (instruction.SH > 0) {
                Value mask = Constant((ulong)((1L << instruction.SH) - 1));

                Value shiftedBits = And(srcReg, mask);
                Value hasOnes = SetNotEqual(shiftedBits, _irContext.Zero);
                Value sign = GetSign(srcReg);

                ca = And(sign, hasOnes);

            } else {
                ca = _irContext.Zero;
            }

            _irContext.SetGPR(ra, result);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SRAW(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg64 = _irContext.GetGPR(rs);
            Value srcReg32 = Truncate(srcReg64, Value.ValueType.I32);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg32.Type);
            Value shiftAmount32 = Truncate(shiftAmount, Value.ValueType.I32);

            Value shiftedValue32 = ShiftRightArithmetic(srcReg32, shiftAmount32);
            Value shiftedValue64 = SignExtend(shiftedValue32, Value.ValueType.I64);

            Value sign = GetSign(srcReg32);

            // For arithmetic shift if shift > 31, fill based on sign.
            // We use 64-bit operands here

            Value fill =  Select(sign, _irContext.MinusOne, _irContext.Zero);
            Value result = Select(isGreaterThanMax, fill, shiftedValue64);
            Value ca = GetShiftedOutCA(srcReg32, shiftAmount, isGreaterThanMax);

            _irContext.SetGPR(ra, result);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void SRAWI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            // SH is 5 bits
            int sh = instruction.SH & 0x1F;
            Value shiftAmount = Constant((ulong)sh, Value.ValueType.I32);

            Value srcReg = _irContext.GetGPR(rs);
            Value srcReg32 = Truncate(srcReg, Value.ValueType.I32);

            Value result32 = ShiftRightArithmetic(srcReg32, shiftAmount);
            Value result64 = SignExtend(result32, Value.ValueType.I64);

            Value ca;

            if (instruction.SH > 0) {
                Value sign = GetSign(srcReg32); // pass the 32-bit value
                Value mask = Constant((ulong)((1L << sh) - 1));

                Value shiftedBits = And(srcReg, mask);
                Value hasOnes = SetNotEqual(shiftedBits, _irContext.Zero);

                ca = And(sign, hasOnes);

            } else {
                ca = _irContext.Zero;
            }

            _irContext.SetGPR(ra, result64);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result64, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLDICL(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(instruction.MB_6, 63);

            Value srcReg = _irContext.GetGPR(rs);
            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLDICR(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(0, instruction.ME_6);

            Value srcReg = _irContext.GetGPR(rs);
            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLDIC(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(instruction.MB_6, 63 - sh);

            Value srcReg = _irContext.GetGPR(rs);
            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLDIMI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.SH;
            ulong mask = GetRotationMask(instruction.MB_6, 63 - sh);

            Value srcReg = _irContext.GetGPR(rs);
            Value raReg = _irContext.GetGPR(ra);

            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);

            // result = (rotated & mask) | (raReg & (~mask));

            Value maskedResult = And(rotated, Constant(mask));
            Value maskedRa = And(raReg, Constant(~mask));
            Value result = Or(maskedResult, maskedRa);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLDCL(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_6, 63);

            Value rbReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            Value rotationAmount = And(rbReg, Constant(0x3FL)); // 6-bits
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLDCR(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(0, instruction.ME_6);

            Value rbReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            Value rotationAmount = And(rbReg, Constant(0x3FL)); // 6-bits
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLWIMI(PPUInstruction instruction) {
            int n = instruction.SH & 0x1F; //Rotate 32 takes only 5 bits of SH

            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            Value srcReg = _irContext.GetGPR(rs);
            Value raReg = _irContext.GetGPR(ra);

            Value rotationAmount = Constant(n);
            Value rotated = RotateLeft(srcReg, rotationAmount);

            Value rotatedMasked = And(rotated, Constant(mask));
            Value raMasked = And(raReg, Constant(~mask));

            Value result = Or(rotatedMasked, raMasked);
            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLWINM(PPUInstruction instruction) {
            int n = instruction.SH & 0x1F; //Rotate 32 takes only 5 bits of SH

            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            Value srcReg = _irContext.GetGPR(rs);

            Value rotationAmount = Constant(n);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }

        public void RLWNM(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            Value srcReg = _irContext.GetGPR(rs);
            Value rbReg = _irContext.GetGPR(rb);

            Value rotationAmount = And(rbReg, Constant(0x1F)); //Rotate 32 takes only 5 bits of SH
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(result, _irContext.Zero, PPU.CRNumber.CR0);
            }
        }
    }
}
