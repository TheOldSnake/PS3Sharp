using static PS3Sharp.Cell.Ppu.PpuRotationMasks;

namespace PS3Sharp.Cell.Ppu.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        // Add instructions

        internal void ADDI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = ra == 0 ? _irContext.Zero : _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.Si64);

            Value result = Add(a, b);
            _irContext.SetGPR(rt, result);
        }

        internal void ADDIS(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;
            long si = (int)(instruction.Si << 16); // sign extended

            Value a = ra == 0 ? _irContext.Zero : _irContext.GetGPR(ra);
            Value b = Constant((ulong)si);
            Value result = Add(a, b);
            _irContext.SetGPR(rt, result);
        }

        internal void ADDIC(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.Si64);
            Value result = CkdAdd(a, b, out Value carry);
            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ADDE(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ADD(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = Add(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ADDC(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CkdAdd(a, b, out Value carry);

            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ADDZE(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value ca = _irContext.GetCA();
            Value result = CkdAdd(a, ca, out Value carry);

            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ADDME(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        // Subtract instructions

        internal void SUBF(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = Sub(b, a); // b - a

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SUBFC(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SUBFIC(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.Si64);
            Value notA = Not(a);

            // Add ~a + b + 1, and handle intermediate carrys
            Value sum1 = CkdAdd(notA, b, out Value carry1);
            Value finalSum = CkdAdd(sum1, _irContext.One, out Value carry2);
            Value finalCarry = Or(carry1, carry2);

            _irContext.SetGPR(rt, finalSum);
            _irContext.SetCA(finalCarry);
        }

        internal void SUBFE(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SUBFZE(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value ca = _irContext.GetCA();
            Value notA = Not(a);
            Value result = CkdAdd(notA, ca, out Value carry);

            _irContext.SetGPR(rt, result);
            _irContext.SetCA(carry);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SUBFME(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(finalSum, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        // General logic instructions
        internal void ANDI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant((ulong)instruction.Ui);
            Value result = And(s, imm);

            _irContext.SetGPR(ra, result);
            _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);  // Always updates CR0
        }

        internal void ANDIS(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant(((ulong)instruction.Ui) << 16);
            Value result = And(s, imm);

            _irContext.SetGPR(ra, result);
            _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0); // Always updates CR0
        }

        internal void ORI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant((ulong)instruction.Ui);
            Value result = Or(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        internal void ORIS(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant(((ulong)instruction.Ui) << 16);
            Value result = Or(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        internal void XORI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant((ulong)instruction.Ui);
            Value result = Xor(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        internal void XORIS(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value imm = Constant(((ulong)instruction.Ui) << 16);
            Value result = Xor(s, imm);

            _irContext.SetGPR(ra, result);
            // Does not affect CR0
        }

        internal void AND(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = And(s, b);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void OR(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Or(s, b);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void XOR(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Xor(s, b);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void NAND(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Not(And(b, s));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void NOR(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Not(Or(b, s));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void EQV(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Not(Xor(b, s));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ANDC(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = And(s, Not(b));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void ORC(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value s = _irContext.GetGPR(rs);
            Value b = _irContext.GetGPR(rb);
            Value result = Or(s, Not(b));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void NEG(PpuInstruction instruction)
        {
            // RT = ¬(RA) + 1
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value notA = Not(a);
            Value result = Add(notA, _irContext.One);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        // Condition register bit logic opcodes
        internal (Value ba, Value bb) GetBitsAB(PpuInstruction instruction)
        {
            //Helper for condition register bit opcodes
            int bitA = (int)(31 - instruction.Ba);
            int bitB = (int)(31 - instruction.Bb);

            Value cr = _irContext.GetCR();
            Value a = ReadBit(cr, bitA);
            Value b = ReadBit(cr, bitB);
            return (a, b);
        }

        internal void CROR(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Or(bitA, bitB);
            _irContext.WriteCRBit(bt, result);
        }

        internal void CRORC(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Or(bitA, Not(bitB));
            _irContext.WriteCRBit(bt, result);
        }

        internal void CRAND(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = And(bitA, bitB);
            _irContext.WriteCRBit(bt, result);
        }

        internal void CRANDC(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = And(bitA, Not(bitB));
            _irContext.WriteCRBit(bt, result);
        }

        internal void CRXOR(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Xor(bitA, bitB);
            _irContext.WriteCRBit(bt, result);
        }

        internal void CRNAND(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Not(And(bitA, bitB));
            _irContext.WriteCRBit(bt, result);
        }

        internal void CRNOR(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Not(Or(bitA, bitB));
            _irContext.WriteCRBit(bt, result);
        }

        internal void CREQV(PpuInstruction instruction)
        {
            uint bt = instruction.Bt;
            (Value bitA, Value bitB) = GetBitsAB(instruction);
            Value result = Not(Xor(bitA, bitB));
            _irContext.WriteCRBit(bt, result);
        }

        // Multiply instructions
        internal void MULLI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = Constant((ulong)instruction.Si64);
            Value result = IMulLD(a, b);

            _irContext.SetGPR(rt, result);
            // No special registers affected
        }

        internal void MULLW(PpuInstruction instruction)
        {
            // Multiply word, write 64-bit result (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulLW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void MULHW(PpuInstruction instruction)
        {
            // Multiply word, write high 32-bit result (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulHW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void MULHWU(PpuInstruction instruction)
        {
            // Multiply word, write high 32-bit (unsigned)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = MulHW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void MULLD(PpuInstruction instruction)
        {
            // Multiply double word (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulLD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void MULHD(PpuInstruction instruction)
        {
            // Multiply double word, write high 64-bit (signed)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = IMulHD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void MULHDU(PpuInstruction instruction)
        {
            // Multiply double word, write high 64-bit (unsigned)
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = MulHD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        // Divide instructions
        internal void DIVD(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedIDivD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void DIVDU(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedDivD(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void DIVW(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedIDivW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void DIVWU(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rt = instruction.Rt;

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);
            Value result = CheckedDivW(a, b);

            _irContext.SetGPR(rt, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal (Value shiftAmount, Value isGreaterThanMaxShift) GetShiftAmount(Value shiftRegister, Value.ValueType destinationType)
        {
            if (destinationType != Value.ValueType.I64 && destinationType != Value.ValueType.I32)
            {
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

        internal Value GetShiftedOutCA(Value value, Value shiftAmount, Value isGreaterThanMax)
        {
            // Get the sign from the original value before extending
            Value sign = GetSign(value);
            Value value64;

            if (value.Type != Value.ValueType.I64)
            {
                // Shifted-out bits are examined as a raw bit pattern,
                // so widen without changing the low bits.
                value64 = ZeroExtend(value, Value.ValueType.I64);

            }
            else
            {
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

        internal Value GetSign(Value value)
        {
            // Make sure we're comparing same size
            Value zero = Constant(0, value.Type);
            Value isNegative = SetLessThanSigned(value, zero);

            // The result however, is 64-bit
            return Select(isNegative, _irContext.One, _irContext.Zero);
        }

        // Shift instructions
        internal void SLD(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg.Type);
            Value shiftedValue64 = ShiftLeft(srcReg, shiftAmount);
            Value result = Select(isGreaterThanMax, _irContext.Zero, shiftedValue64);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SRD(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg.Type);
            Value shiftedValue64 = ShiftRight(srcReg, shiftAmount);
            Value result = Select(isGreaterThanMax, _irContext.Zero, shiftedValue64);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SLW(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SRW(PpuInstruction instruction)
        {
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

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SRAD(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;

            Value shiftReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            (Value shiftAmount, Value isGreaterThanMax) = GetShiftAmount(shiftReg, srcReg.Type);
            Value shiftedValue = ShiftRightArithmetic(srcReg, shiftAmount);
            Value sign = GetSign(srcReg);

            // For arithmetic shift if shift > 63, fill based on sign.
            Value fill = Select(sign, _irContext.MinusOne, _irContext.Zero);
            Value result = Select(isGreaterThanMax, fill, shiftedValue);
            Value ca = GetShiftedOutCA(srcReg, shiftAmount, isGreaterThanMax);

            _irContext.SetGPR(ra, result);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SRADI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            // SH is 6 bits
            Value shiftAmount = Constant((ulong)instruction.Sh);

            Value srcReg = _irContext.GetGPR(rs);
            Value result = ShiftRightArithmetic(srcReg, shiftAmount);

            Value ca;

            if (instruction.Sh > 0)
            {
                Value mask = Constant((ulong)((1L << instruction.Sh) - 1));

                Value shiftedBits = And(srcReg, mask);
                Value hasOnes = SetNotEqual(shiftedBits, _irContext.Zero);
                Value sign = GetSign(srcReg);

                ca = And(sign, hasOnes);

            }
            else
            {
                ca = _irContext.Zero;
            }

            _irContext.SetGPR(ra, result);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SRAW(PpuInstruction instruction)
        {
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

            Value fill = Select(sign, _irContext.MinusOne, _irContext.Zero);
            Value result = Select(isGreaterThanMax, fill, shiftedValue64);
            Value ca = GetShiftedOutCA(srcReg32, shiftAmount, isGreaterThanMax);

            _irContext.SetGPR(ra, result);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void SRAWI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            // SH is 5 bits
            int sh = instruction.Sh & 0x1F;
            Value shiftAmount = Constant((ulong)sh, Value.ValueType.I32);

            Value srcReg = _irContext.GetGPR(rs);
            Value srcReg32 = Truncate(srcReg, Value.ValueType.I32);

            Value result32 = ShiftRightArithmetic(srcReg32, shiftAmount);
            Value result64 = SignExtend(result32, Value.ValueType.I64);

            Value ca;

            if (instruction.Sh > 0)
            {
                Value sign = GetSign(srcReg32); // pass the 32-bit value
                Value mask = Constant((ulong)((1L << sh) - 1));

                Value shiftedBits = And(srcReg, mask);
                Value hasOnes = SetNotEqual(shiftedBits, _irContext.Zero);

                ca = And(sign, hasOnes);

            }
            else
            {
                ca = _irContext.Zero;
            }

            _irContext.SetGPR(ra, result64);
            _irContext.SetCA(ca);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result64, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLDICL(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.Sh;
            ulong mask = GetRotationMask(instruction.Mb6, 63);

            Value srcReg = _irContext.GetGPR(rs);
            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLDICR(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.Sh;
            ulong mask = GetRotationMask(0, instruction.Me6);

            Value srcReg = _irContext.GetGPR(rs);
            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLDIC(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.Sh;
            ulong mask = GetRotationMask(instruction.Mb6, 63 - sh);

            Value srcReg = _irContext.GetGPR(rs);
            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLDIMI(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            int sh = instruction.Sh;
            ulong mask = GetRotationMask(instruction.Mb6, 63 - sh);

            Value srcReg = _irContext.GetGPR(rs);
            Value raReg = _irContext.GetGPR(ra);

            Value rotationAmount = Constant((ulong)sh);
            Value rotated = RotateLeft(srcReg, rotationAmount);

            // result = (rotated & mask) | (raReg & (~mask));

            Value maskedResult = And(rotated, Constant(mask));
            Value maskedRa = And(raReg, Constant(~mask));
            Value result = Or(maskedResult, maskedRa);

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLDCL(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.Mb6, 63);

            Value rbReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            Value rotationAmount = And(rbReg, Constant(0x3FL)); // 6-bits
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLDCR(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(0, instruction.Me6);

            Value rbReg = _irContext.GetGPR(rb);
            Value srcReg = _irContext.GetGPR(rs);

            Value rotationAmount = And(rbReg, Constant(0x3FL)); // 6-bits
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLWIMI(PpuInstruction instruction)
        {
            int n = instruction.Sh & 0x1F; //Rotate 32 takes only 5 bits of SH

            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.Mb5 + 32, instruction.Me5 + 32);

            Value srcReg = _irContext.GetGPR(rs);
            Value raReg = _irContext.GetGPR(ra);

            Value rotationAmount = Constant(n);
            Value rotated = RotateLeft(srcReg, rotationAmount);

            Value rotatedMasked = And(rotated, Constant(mask));
            Value raMasked = And(raReg, Constant(~mask));

            Value result = Or(rotatedMasked, raMasked);
            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLWINM(PpuInstruction instruction)
        {
            int n = instruction.Sh & 0x1F; //Rotate 32 takes only 5 bits of SH

            uint ra = instruction.Ra;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.Mb5 + 32, instruction.Me5 + 32);

            Value srcReg = _irContext.GetGPR(rs);

            Value rotationAmount = Constant(n);
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }

        internal void RLWNM(PpuInstruction instruction)
        {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            uint rs = instruction.Rs;
            ulong mask = GetRotationMask(instruction.Mb5 + 32, instruction.Me5 + 32);

            Value srcReg = _irContext.GetGPR(rs);
            Value rbReg = _irContext.GetGPR(rb);

            Value rotationAmount = And(rbReg, Constant(0x1F)); //Rotate 32 takes only 5 bits of SH
            Value rotated = RotateLeft(srcReg, rotationAmount);
            Value result = And(rotated, Constant(mask));

            _irContext.SetGPR(ra, result);

            if (instruction.Rc == 1)
            {
                _irContext.UpdateCR(result, _irContext.Zero, PpuThread.CrNumber.CR0);
            }
        }
    }
}
