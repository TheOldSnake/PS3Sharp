using static PS3Sharp.Cell.PPU.PPUInterpreter.PPUInterpreter;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRGenerator {
        public static Value GetEa(IRContext ctx, PPUInstruction instruction, MemAccessForm form, bool checkRA) {

            Value baseAddress;
            if (checkRA) {
                baseAddress = instruction.Ra != 0 ? ctx.GetGPR(instruction.Ra) : ctx.Zero;
            } else {
                baseAddress = ctx.GetGPR(instruction.Ra);
            }


            Value offs;
            Value ea;

            switch (form) {
                case MemAccessForm.DForm:
                    offs = Constant(ctx, (long)(short)instruction.D);           // sign extended
                    break;

                case MemAccessForm.DSForm:
                    offs = Constant(ctx, (long)(short)(instruction.DS << 2));   // sign extended
                    break;

                case MemAccessForm.XForm:
                    offs = ctx.GetGPR(instruction.Rb);
                    break;

                // Should be unreachable
                default:
                    throw new Exception($"Unknown Form {form}");
            }

            ea = Add(ctx, baseAddress, offs);
            return ea;
        }

        // Store Byte Instructions
        public static void STB(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store8(ctx, ea, Truncate(ctx, src, Value.ValueType.I8));
        }

        public static void STBX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store8(ctx, ea, Truncate(ctx, src, Value.ValueType.I8));
        }

        public static void STBU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store8(ctx, ea, Truncate(ctx, src, Value.ValueType.I8));
            ctx.SetGPR(instruction.Ra, ea); // update
        }

        public static void STBUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store8(ctx, ea, Truncate(ctx, src, Value.ValueType.I8));
            ctx.SetGPR(instruction.Ra, ea); // update
        }

        // Store Half Instructions
        public static void STH(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store16(ctx, ea, Truncate(ctx, src, Value.ValueType.I16));
        }

        public static void STHX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store16(ctx, ea, Truncate(ctx, src, Value.ValueType.I16));
        }

        public static void STHU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store16(ctx, ea, Truncate(ctx, src, Value.ValueType.I16));
            ctx.SetGPR(instruction.Ra, ea); // update
        }

        public static void STHUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store16(ctx, ea, Truncate(ctx, src, Value.ValueType.I16));
            ctx.SetGPR(instruction.Ra, ea); // update
        }

        public static void STHBRX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store16Reversed(ctx, ea, Truncate(ctx, src, Value.ValueType.I16));
        }

        // Store Word Instructions
        public static void STW(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store32(ctx, ea, Truncate(ctx, src, Value.ValueType.I32));
        }

        public static void STWX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store32(ctx, ea, Truncate(ctx, src, Value.ValueType.I32));
        }

        public static void STWU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store32(ctx, ea, Truncate(ctx, src, Value.ValueType.I32));
            ctx.SetGPR(instruction.Ra, ea); // update
        }

        public static void STWUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store32(ctx, ea, Truncate(ctx, src, Value.ValueType.I32));
            ctx.SetGPR(instruction.Ra, ea); // update
        }

        public static void STWBRX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Store32Reversed(ctx, ea, Truncate(ctx, src, Value.ValueType.I32));
        }

        public static void STWCX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Value success = Store32Conditional(ctx, ea, Truncate(ctx, src, Value.ValueType.I32));
            Value cr0 = Select(ctx, success, ctx.CompareResult_Eq, ctx.Zero);
            ctx.WriteCRField(PPU.CRNumber.CR0, cr0);
        }

        // Store Double Word Instructions
        public static void STD(IRContext ctx, PPUInstruction instruction) {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(ctx, instruction, MemAccessForm.DSForm, checkRa);
            Value src = ctx.GetGPR(instruction.Rs);
            Store64(ctx, ea, src);

            if (update) {
                ctx.SetGPR(instruction.Ra, ea);
            }
        }

        public static void STDX(IRContext ctx, PPUInstruction instruction) {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, checkRa);
            Value src = ctx.GetGPR(instruction.Rs);
            Store64(ctx, ea, src);

            if (update) {
                ctx.SetGPR(instruction.Ra, ea);
            }
        }

        public static void STDUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value src = ctx.GetGPR(instruction.Rs);
            Store64(ctx, ea, src);
            ctx.SetGPR(instruction.Ra, ea);     // update
        }

        public static void STDCX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value src = ctx.GetGPR(instruction.Rs);
            Value success = Store64Conditional(ctx, ea, src);
            Value cr0 = Select(ctx, success, ctx.CompareResult_Eq, ctx.Zero);
            ctx.WriteCRField(PPU.CRNumber.CR0, cr0);
        }

        // Load Byte Instructions
        public static void LBZ(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value value = Load8(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LBZX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load8(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LBZU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value value = Load8(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LBZUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value value = Load8(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        // Load Half Instructions
        public static void LHZ(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LHZX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LHZU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LHZUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LHBRX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load16Reversed(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LHA(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
        }

        public static void LHAX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
        }

        public static void LHAU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LHAUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value value = Load16(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
            ctx.SetGPR(instruction.Ra, ea);    // update
        }


        // Load Word Instructions
        public static void LWZ(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LWZX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LWZU(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, false);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LWZUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LWBRX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load32Reversed(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LWARX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load32AndReserve(ctx, ea);
            ctx.SetGPR(instruction.Rt, ZeroExtend(ctx, value, Value.ValueType.I64));
        }

        public static void LWA(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
        }

        public static void LWAX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
        }

        public static void LWAUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value value = Load32(ctx, ea);
            ctx.SetGPR(instruction.Rt, SignExtend(ctx, value, Value.ValueType.I64));    // Sign extends
            ctx.SetGPR(instruction.Ra, ea);    // update
        }

        public static void LMW(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.DForm, true);
            uint currentRegister = instruction.Rt;

            while (currentRegister <= 31) {
                Value value = Load32(ctx, ea);
                ctx.SetGPR(currentRegister, ZeroExtend(ctx, value, Value.ValueType.I64));
                ea = Add(ctx, ea, Constant(ctx, 4));
                currentRegister++;
            }
        }

        // Load Double Word Instructions
        public static void LD(IRContext ctx, PPUInstruction instruction) {
            switch (instruction.Raw & 0x3) {
                case 0:
                case 1:
                    LoadDoubleWord(ctx, instruction);
                    break;

                //LoadWordAlgebraic
                case 2:
                    LWA(ctx, instruction);
                    break;
            }
        }

        public static void LoadDoubleWord(IRContext ctx, PPUInstruction instruction) {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(ctx, instruction, MemAccessForm.DSForm, checkRa);
            Value value = Load64(ctx, ea);
            ctx.SetGPR(instruction.Rt, value);

            if (update) {
                ctx.SetGPR(instruction.Ra, ea);
            }
        }

        public static void LDX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load64(ctx, ea);
            ctx.SetGPR(instruction.Rt, value);
        }

        public static void LDUX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, false);
            Value value = Load64(ctx, ea);
            ctx.SetGPR(instruction.Rt, value);
            ctx.SetGPR(instruction.Ra, ea);     // update
        }

        public static void LDARX(IRContext ctx, PPUInstruction instruction) {
            Value ea = GetEa(ctx, instruction, MemAccessForm.XForm, true);
            Value value = Load64AndReserve(ctx, ea);
            ctx.SetGPR(instruction.Rt, value);
        }
    }
}
