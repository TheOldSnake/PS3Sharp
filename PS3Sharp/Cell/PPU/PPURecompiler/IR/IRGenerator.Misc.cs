using static PS3Sharp.Cell.PPU.PPU;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRGenerator {

        public static void Syscall(IRContext ctx, PPUInstruction instruction) {
            //IRSyscall(ctx, instruction, ctx.CurrentPC);
            ctx.ExitPC = Constant(ctx, ctx.CurrentPC);
        }

        public static void MFSPR(IRContext ctx, PPUInstruction instruction) {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            uint rt = instruction.Rt;

            Value result;
            switch (spr) {
                case 0b0000000001:
                    result = ctx.GetXER();
                    break;  

                case 0b0000001000:
                    result = ctx.GetLR();
                    break;  

                case 0b0000001001:
                    result = ctx.GetCTR();
                    break;  

                case 0b0100000000:
                    result = ctx.GetVRSave();
                    break;

                default:
                    throw new Exception($"[IR] Unknown SPR = {spr:B10}");
            }

            ctx.SetGPR(rt, result);
        }

        public static void MTSPR(IRContext ctx, PPUInstruction instruction) {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            uint rs = instruction.Rs;

            Value src = ctx.GetGPR(rs);
            switch (spr) {
                case 0b0000000001:
                    ctx.SetXER(src);
                    break;  

                case 0b0000001000:
                    ctx.SetLR(src);
                    break;  

                case 0b0000001001:
                    ctx.SetCTR(src); 
                    break;  

                case 0b0100000000:
                    ctx.SetVRSave(src);
                    break;

                default:
                    throw new Exception($"[IR] Unknown SPR = {spr:B10}");
            }
        }

        public static void MFCR(IRContext ctx, PPUInstruction instruction) {
            uint rt = instruction.Rt;
            Value cr = ctx.GetCR();
            ctx.SetGPR(rt, cr);
            //TODO: mfocrf
        }

        public static void MTCRF(IRContext ctx, PPUInstruction instruction) {
            uint rs = instruction.Rs;
            bool isOneField = ((instruction.Raw >> 20) & 1) == 1;
            uint fxm = instruction.Fxm;
            Value src = ctx.GetGPR(rs);

            if (isOneField && uint.PopCount(fxm) > 1) {
                throw new Exception($"[IR] mtocrf with fxm containing more than one field");
            }

            for (int i = 0; i < 8; i++) {
                if (((fxm >> i) & 1) == 1) {
                    PPU.CRNumber cr = (PPU.CRNumber)(7 - i);

                    // uint value = (rs >> (i * 4)) & 0xF;
                    Value srcShifted = ShiftRight(ctx, src, Constant(ctx, i * 4));
                    Value result = And(ctx, srcShifted, Constant(ctx, 0xF));
                    ctx.WriteCRField(cr, result);
                }
            }
        }

        public static void MCRF(IRContext ctx, PPUInstruction instruction) {
            // CR_BF = CR_BFA
            Value bfa = ctx.GetCRField((CRNumber)instruction.BFA);
            ctx.WriteCRField((CRNumber)instruction.BF, bfa);
        }

        public static void MFTB(IRContext ctx, PPUInstruction instruction) {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            throw new Exception("[IR] TODO: MFTB");
        }

        // Helper for conditional branches
        public static Value TestBranchCondition(IRContext ctx, uint bo, uint bi) {
            // Based on PPC manual page 34

            uint bit1 = (bo >> 1) & 1; // CTR condition: if set, compares with 0, if unset compares with != 0
            uint bit2 = (bo >> 2) & 1; // If set don't test CTR (and don't decrement)
            uint bit3 = (bo >> 3) & 1; // CR condition: if set, compares with 1, if unset compares with 0 (the opposite of bit 1), basically compare this bit with cr bi bit
            uint bit4 = (bo >> 4) & 1; // if set don't test CR

            // If both bits 2 and 4 are set, don't test anything and the result is always true
            if ((bit2 & bit4) == 1) { return ctx.One; }


            // CTR condition
            Value ctr_condition = ctx.One;

            if (bit2 == 0) {
                // ppu.State->CTR--;
                Value newCtr = Sub(ctx, ctx.GetCTR(), ctx.One);
                ctx.SetCTR(newCtr);

                // ctr_condition = bit1 == 1 ? (ppu.State->CTR == 0) : (ppu.State->CTR != 0);
                ctr_condition = bit1 == 1 ? SetEqual(ctx, newCtr, ctx.Zero) : SetNotEqual(ctx, newCtr, ctx.Zero);
            }


            // CR Condition
            Value cr_condition = ctx.One;

            if (bit4 == 0) {
                // uint cr_bi_bit = ppu.State->CR.ReadCRBit(bi);
                Value cr_bi_bit = ctx.GetCRBit(bi);

                // cr_condition = cr_bi_bit == bit3;
                cr_condition = SetEqual(ctx, cr_bi_bit, Constant(ctx, bit3));
            }

            return And(ctx, ctr_condition, cr_condition);
        }

        private static void Branch(IRContext ctx, PPUInstruction instruction) {
            // Branch L form 

            bool link = (instruction.Raw & 1) == 1;
            bool absolute_address = ((instruction.Raw >> 1) & 1) == 1;

            int li = (int)((instruction.Raw >> 2) & 0xFFFFFF) << 2; //26-bits
            long li_signExtend = (long)(int)((li << 6) >> 6);       //singed 32-bits
            long currentPC = ctx.CurrentPC - 4;                     //Address of this opcode
            long target = li_signExtend;

            // Compute relative address if !absolute_address
            if (!absolute_address) {
                target += currentPC;
            }

            // Link if needed
            if (link) {
                ctx.SetLR(Constant(ctx, ctx.CurrentPC));
            }

            // unconditional
            ctx.ExitPC = Constant(ctx, (uint)target);
        }

        public static void BC(IRContext ctx, PPUInstruction instruction) {
            bool lk = (instruction.Raw & 1) == 1;
            bool aa = ((instruction.Raw >> 1) & 1) == 1;
            int bd = (int)(short)(instruction.BD << 2);
            long bd_signExtend = (long)(int)((bd << 6) >> 6);       //singed 32-bits
            long currentPC = ctx.CurrentPC - 4;                     //Address of this opcode
            long target = bd_signExtend;

            if (!aa) {
                target += currentPC;
            }

            if (lk) {
                ctx.SetLR(Constant(ctx, ctx.CurrentPC));
            }

            Value result = TestBranchCondition(ctx, instruction.Bo, instruction.Bi);
            ctx.ExitPC = Select(ctx, result, Constant(ctx, target), Constant(ctx, ctx.CurrentPC));
        }

        public static void BCLR(IRContext ctx, PPUInstruction instruction) {
            bool lk = (instruction.Raw & 1) == 1;
            Value target = ctx.GetLR();

            if (lk) {
                ctx.SetLR(Constant(ctx, ctx.CurrentPC));
            }

            Value result = TestBranchCondition(ctx, instruction.Bo, instruction.Bi);
            ctx.ExitPC = Select(ctx, result, target, Constant(ctx, ctx.CurrentPC));
        }

        public static void BCCTR(IRContext ctx, PPUInstruction instruction) {
            bool lk = (instruction.Raw & 1) == 1;
            Value target = ctx.GetCTR();

            if (lk) {
                ctx.SetLR(Constant(ctx, ctx.CurrentPC));
            }

            Value result = TestBranchCondition(ctx, instruction.Bo, instruction.Bi);
            ctx.ExitPC = Select(ctx, result, target, Constant(ctx, ctx.CurrentPC));
        }

        // Compare instructions

        public static void CMPI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value si64 = Constant(ctx, instruction.SI_64);
            Value a;

            a = ctx.GetGPR(ra);

            if (!l) {
                // Sign extend low 32-bit
                a = Truncate(ctx, a, Value.ValueType.I32);
                a = SignExtend(ctx, a, Value.ValueType.I64);
            }

            ctx.UpdateCR(a, si64, (CRNumber)bf);
        }

        public static void CMPLI(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value ui = Constant(ctx, instruction.UI);
            Value a;

            a = ctx.GetGPR(ra);

            if (!l) {
                // Zero extend low 32-bit
                a = Truncate(ctx, a, Value.ValueType.I32);
                a = ZeroExtend(ctx, a, Value.ValueType.I64);
            }

            ctx.UpdateCR(a, ui, (CRNumber)bf, signed: false); // unsigned compare
        }

        public static void CMPL(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            if (!l) {
                // Zero extend low 32-bit
                a = Truncate(ctx, a, Value.ValueType.I32);
                a = ZeroExtend(ctx, a, Value.ValueType.I64);

                b = Truncate(ctx, b, Value.ValueType.I32);
                b = ZeroExtend(ctx, b, Value.ValueType.I64);
            }

            ctx.UpdateCR(a, b, (CRNumber)bf, signed: false); // unsigned compare
        }

        public static void CMP(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rb = instruction.Rb;
            bool l = ((instruction.Raw >> 21) & 0x1) == 1; // long
            uint bf = ((instruction.Raw >> 23) & 0x7);

            Value a = ctx.GetGPR(ra);
            Value b = ctx.GetGPR(rb);

            if (!l) {
                // Sign extend low 32-bit
                a = Truncate(ctx, a, Value.ValueType.I32);
                a = SignExtend(ctx, a, Value.ValueType.I64);

                b = Truncate(ctx, b, Value.ValueType.I32);
                b = SignExtend(ctx, b, Value.ValueType.I64);
            }

            ctx.UpdateCR(a, b, (CRNumber)bf);
        }

        // Sign Extension instructions
        public static void EXTSB(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = ctx.GetGPR(rs);
            src = Truncate(ctx, src, Value.ValueType.I8);
            src = SignExtend(ctx, src, Value.ValueType.I64);

            ctx.SetGPR(ra, src);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(src, ctx.Zero, CRNumber.CR0);
            }
        }

        public static void EXTSH(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = ctx.GetGPR(rs);
            src = Truncate(ctx, src, Value.ValueType.I16);
            src = SignExtend(ctx, src, Value.ValueType.I64);

            ctx.SetGPR(ra, src);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(src, ctx.Zero, CRNumber.CR0);
            }
        }

        public static void EXTSW(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = ctx.GetGPR(rs);
            src = Truncate(ctx, src, Value.ValueType.I32);
            src = SignExtend(ctx, src, Value.ValueType.I64);

            ctx.SetGPR(ra, src);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(src, ctx.Zero, CRNumber.CR0);
            }
        }

        public static void CNTLZD(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = ctx.GetGPR(rs);
            Value count = CountLeadingZeroes(ctx, src);
            ctx.SetGPR(ra, count);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(count, ctx.Zero, CRNumber.CR0, signed: false);  // unsigned compare
            }
        }

        public static void CNTLZW(IRContext ctx, PPUInstruction instruction) {
            uint ra = instruction.Ra;
            uint rs = instruction.Rs;

            Value src = ctx.GetGPR(rs);
            Value src32 = Truncate(ctx, src, Value.ValueType.I32);
            Value count = CountLeadingZeroes(ctx, src32);
            ctx.SetGPR(ra, count);

            if (instruction.Rc == 1) {
                ctx.UpdateCR(count, ctx.Zero, CRNumber.CR0, signed: false);  // unsigned compare
            }
        }
    }
}
