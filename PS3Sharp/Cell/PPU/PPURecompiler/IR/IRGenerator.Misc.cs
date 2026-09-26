using static PS3Sharp.Cell.PPU.PPU;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public partial class IRGenerator {

        public void Syscall(PPUInstruction instruction) {
            //IRSyscall(instruction, _irContext.CurrentPC);
            _irContext.ExitPC = Constant(_irContext.CurrentPC);
        }

        public void MFSPR(PPUInstruction instruction) {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            uint rt = instruction.Rt;

            Value result;
            switch (spr) {
                case 0b0000000001:
                    result = _irContext.GetXER();
                    break;  

                case 0b0000001000:
                    result = _irContext.GetLR();
                    break;  

                case 0b0000001001:
                    result = _irContext.GetCTR();
                    break;  

                case 0b0100000000:
                    result = _irContext.GetVRSave();
                    break;

                default:
                    throw new Exception($"[IR] Unknown SPR = {spr:B10}");
            }

            _irContext.SetGPR(rt, result);
        }

        public void MTSPR(PPUInstruction instruction) {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            uint rs = instruction.Rs;

            Value src = _irContext.GetGPR(rs);
            switch (spr) {
                case 0b0000000001:
                    _irContext.SetXER(src);
                    break;  

                case 0b0000001000:
                    _irContext.SetLR(src);
                    break;  

                case 0b0000001001:
                    _irContext.SetCTR(src); 
                    break;  

                case 0b0100000000:
                    _irContext.SetVRSave(src);
                    break;

                default:
                    throw new Exception($"[IR] Unknown SPR = {spr:B10}");
            }
        }

        public void MFCR(PPUInstruction instruction) {
            uint rt = instruction.Rt;
            Value cr = _irContext.GetCR();
            _irContext.SetGPR(rt, cr);
            //TODO: mfocrf
        }

        public void MTCRF(PPUInstruction instruction) {
            uint rs = instruction.Rs;
            bool isOneField = ((instruction.Raw >> 20) & 1) == 1;
            uint fxm = instruction.Fxm;
            Value src = _irContext.GetGPR(rs);

            if (isOneField && uint.PopCount(fxm) > 1) {
                throw new Exception($"[IR] mtocrf with fxm containing more than one field");
            }

            for (int i = 0; i < 8; i++) {
                if (((fxm >> i) & 1) == 1) {
                    PPU.CRNumber cr = (PPU.CRNumber)(7 - i);

                    // uint value = (rs >> (i * 4)) & 0xF;
                    Value srcShifted = ShiftRight(src, Constant(i * 4));
                    Value result = And(srcShifted, Constant(0xF));
                    _irContext.WriteCRField(cr, result);
                }
            }
        }

        public void MCRF(PPUInstruction instruction) {
            // CR_BF = CR_BFA
            Value bfa = _irContext.GetCRField((CRNumber)instruction.BFA);
            _irContext.WriteCRField((CRNumber)instruction.BF, bfa);
        }

        public void MFTB(PPUInstruction instruction) {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            throw new Exception("[IR] TODO: MFTB");
        }

        // Helper for conditional branches
        public Value TestBranchCondition(uint bo, uint bi) {
            // Based on PPC manual page 34

            uint bit1 = (bo >> 1) & 1; // CTR condition: if set, compares with 0, if unset compares with != 0
            uint bit2 = (bo >> 2) & 1; // If set don't test CTR (and don't decrement)
            uint bit3 = (bo >> 3) & 1; // CR condition: if set, compares with 1, if unset compares with 0 (the opposite of bit 1), basically compare this bit with cr bi bit
            uint bit4 = (bo >> 4) & 1; // if set don't test CR

            // If both bits 2 and 4 are set, don't test anything and the result is always true
            if ((bit2 & bit4) == 1) { return _irContext.One; }


            // CTR condition
            Value ctr_condition = _irContext.One;

            if (bit2 == 0) {
                // ppu.State->CTR--;
                Value newCtr = Sub(_irContext.GetCTR(), _irContext.One);
                _irContext.SetCTR(newCtr);

                // ctr_condition = bit1 == 1 ? (ppu.State->CTR == 0) : (ppu.State->CTR != 0);
                ctr_condition = bit1 == 1 ? SetEqual(newCtr, _irContext.Zero) : SetNotEqual(newCtr, _irContext.Zero);
            }


            // CR Condition
            Value cr_condition = _irContext.One;

            if (bit4 == 0) {
                // uint cr_bi_bit = ppu.State->CR.ReadCRBit(bi);
                Value cr_bi_bit = _irContext.GetCRBit(bi);

                // cr_condition = cr_bi_bit == bit3;
                cr_condition = SetEqual(cr_bi_bit, Constant(bit3));
            }

            return And(ctr_condition, cr_condition);
        }

        private void Branch(PPUInstruction instruction) {
            // Branch L form 

            bool link = (instruction.Raw & 1) == 1;
            bool absolute_address = ((instruction.Raw >> 1) & 1) == 1;

            int li = (int)((instruction.Raw >> 2) & 0xFFFFFF) << 2; //26-bits
            long li_signExtend = (long)(int)((li << 6) >> 6);       //singed 32-bits
            long currentPC = _irContext.CurrentPC - 4;                     //Address of this opcode
            long target = li_signExtend;

            // Compute relative address if !absolute_address
            if (!absolute_address) {
                target += currentPC;
            }

            // Link if needed
            if (link) {
                _irContext.SetLR(Constant(_irContext.CurrentPC));
            }

            // unconditional
            _irContext.ExitPC = Constant((uint)target);
        }

        public void BC(PPUInstruction instruction) {
            bool lk = (instruction.Raw & 1) == 1;
            bool aa = ((instruction.Raw >> 1) & 1) == 1;
            int bd = (int)(short)(instruction.BD << 2);
            long bd_signExtend = (long)(int)((bd << 6) >> 6);       //singed 32-bits
            long currentPC = _irContext.CurrentPC - 4;                     //Address of this opcode
            long target = bd_signExtend;

            if (!aa) {
                target += currentPC;
            }

            if (lk) {
                _irContext.SetLR(Constant(_irContext.CurrentPC));
            }

            Value result = TestBranchCondition(instruction.Bo, instruction.Bi);
            _irContext.ExitPC = Select(result, Constant(target), Constant(_irContext.CurrentPC));
        }

        public void BCLR(PPUInstruction instruction) {
            bool lk = (instruction.Raw & 1) == 1;
            Value target = _irContext.GetLR();

            if (lk) {
                _irContext.SetLR(Constant(_irContext.CurrentPC));
            }

            Value result = TestBranchCondition(instruction.Bo, instruction.Bi);
            _irContext.ExitPC = Select(result, target, Constant(_irContext.CurrentPC));
        }

        public void BCCTR(PPUInstruction instruction) {
            bool lk = (instruction.Raw & 1) == 1;
            Value target = _irContext.GetCTR();

            if (lk) {
                _irContext.SetLR(Constant(_irContext.CurrentPC));
            }

            Value result = TestBranchCondition(instruction.Bo, instruction.Bi);
            _irContext.ExitPC = Select(result, target, Constant(_irContext.CurrentPC));
        }

        // Compare instructions

        public void CMPI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value si64 = Constant(instruction.SI_64);
            Value a;

            a = _irContext.GetGPR(ra);

            if (!l) {
                // Sign extend low 32-bit
                a = Truncate(a, Value.ValueType.I32);
                a = SignExtend(a, Value.ValueType.I64);
            }

            _irContext.UpdateCR(a, si64, (CRNumber)bf);
        }

        public void CMPLI(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value ui = Constant(instruction.UI);
            Value a;

            a = _irContext.GetGPR(ra);

            if (!l) {
                // Zero extend low 32-bit
                a = Truncate(a, Value.ValueType.I32);
                a = ZeroExtend(a, Value.ValueType.I64);
            }

            _irContext.UpdateCR(a, ui, (CRNumber)bf, signed: false); // unsigned compare
        }

        public void CMPL(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);

            if (!l) {
                // Zero extend low 32-bit
                a = Truncate(a, Value.ValueType.I32);
                a = ZeroExtend(a, Value.ValueType.I64);

                b = Truncate(b, Value.ValueType.I32);
                b = ZeroExtend(b, Value.ValueType.I64);
            }

            _irContext.UpdateCR(a, b, (CRNumber)bf, signed: false); // unsigned compare
        }

        public void CMP(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value a = _irContext.GetGPR(ra);
            Value b = _irContext.GetGPR(rb);

            if (!l) {
                // Sign extend low 32-bit
                a = Truncate(a, Value.ValueType.I32);
                a = SignExtend(a, Value.ValueType.I64);

                b = Truncate(b, Value.ValueType.I32);
                b = SignExtend(b, Value.ValueType.I64);
            }

            _irContext.UpdateCR(a, b, (CRNumber)bf);
        }

        // Sign Extension instructions
        public void EXTSB(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = _irContext.GetGPR(rs);
            src = Truncate(src, Value.ValueType.I8);
            src = SignExtend(src, Value.ValueType.I64);

            _irContext.SetGPR(ra, src);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(src, _irContext.Zero, CRNumber.CR0);
            }
        }

        public void EXTSH(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = _irContext.GetGPR(rs);
            src = Truncate(src, Value.ValueType.I16);
            src = SignExtend(src, Value.ValueType.I64);

            _irContext.SetGPR(ra, src);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(src, _irContext.Zero, CRNumber.CR0);
            }
        }

        public void EXTSW(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = _irContext.GetGPR(rs);
            src = Truncate(src, Value.ValueType.I32);
            src = SignExtend(src, Value.ValueType.I64);

            _irContext.SetGPR(ra, src);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(src, _irContext.Zero, CRNumber.CR0);
            }
        }

        public void CNTLZD(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = _irContext.GetGPR(rs);
            Value count = CountLeadingZeroes(src);
            _irContext.SetGPR(ra, count);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(count, _irContext.Zero, CRNumber.CR0, signed: false);  // unsigned compare
            }
        }

        public void CNTLZW(PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = _irContext.GetGPR(rs);
            Value src32 = Truncate(src, Value.ValueType.I32);
            Value count = CountLeadingZeroes(src32);
            _irContext.SetGPR(ra, count);

            if (instruction.Rc == 1) {
                _irContext.UpdateCR(count, _irContext.Zero, CRNumber.CR0, signed: false);  // unsigned compare
            }
        }
    }
}
