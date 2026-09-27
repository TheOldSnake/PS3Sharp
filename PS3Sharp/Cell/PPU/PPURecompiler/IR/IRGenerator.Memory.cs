using static PS3Sharp.Cell.PPU.PPUInterpreter.PPUInterpreter;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public sealed partial class IRGenerator {
        public Value GetEa(PPUInstruction instruction, MemAccessForm form, bool checkRA) {

            Value baseAddress;
            if (checkRA) {
                baseAddress = instruction.Ra != 0 ? _irContext.GetGPR(instruction.Ra) : _irContext.Zero;
            } else {
                baseAddress = _irContext.GetGPR(instruction.Ra);
            }


            Value offs;
            Value ea;

            switch (form) {
                case MemAccessForm.DForm:
                    offs = Constant((long)(short)instruction.D);           // sign extended
                    break;

                case MemAccessForm.DSForm:
                    offs = Constant((long)(short)(instruction.DS << 2));   // sign extended
                    break;

                case MemAccessForm.XForm:
                    offs = _irContext.GetGPR(instruction.Rb);
                    break;

                // Should be unreachable
                default:
                    throw new Exception($"Unknown Form {form}");
            }

            ea = Add(baseAddress, offs);
            return ea;
        }

        // Store Byte Instructions
        public void STB(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
        }

        public void STBX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
        }

        public void STBU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        public void STBUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        // Store Half Instructions
        public void STH(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
        }

        public void STHX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
        }

        public void STHU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        public void STHUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        public void STHBRX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16Reversed(ea, Truncate(src, Value.ValueType.I16));
        }

        // Store Word Instructions
        public void STW(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
        }

        public void STWX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
        }

        public void STWU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        public void STWUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        public void STWBRX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32Reversed(ea, Truncate(src, Value.ValueType.I32));
        }

        public void STWCX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Value success = Store32Conditional(ea, Truncate(src, Value.ValueType.I32));
            Value cr0 = Select(success, _irContext.CompareResult_Eq, _irContext.Zero);
            _irContext.WriteCRField(PPU.CRNumber.CR0, cr0);
        }

        // Store Double Word Instructions
        public void STD(PPUInstruction instruction) {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(instruction, MemAccessForm.DSForm, checkRa);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);

            if (update) {
                _irContext.SetGPR(instruction.Ra, ea);
            }
        }

        public void STDX(PPUInstruction instruction) {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(instruction, MemAccessForm.XForm, checkRa);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);

            if (update) {
                _irContext.SetGPR(instruction.Ra, ea);
            }
        }

        public void STDUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);
            _irContext.SetGPR(instruction.Ra, ea);     // update
        }

        public void STDCX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Value success = Store64Conditional(ea, src);
            Value cr0 = Select(success, _irContext.CompareResult_Eq, _irContext.Zero);
            _irContext.WriteCRField(PPU.CRNumber.CR0, cr0);
        }

        // Load Byte Instructions
        public void LBZ(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LBZX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LBZU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LBZUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        // Load Half Instructions
        public void LHZ(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LHZX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LHZU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LHZUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LHBRX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16Reversed(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LHA(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        public void LHAX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        public void LHAU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LHAUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }


        // Load Word Instructions
        public void LWZ(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LWZX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LWZU(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LWZUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LWBRX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32Reversed(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LWARX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32AndReserve(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        public void LWA(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        public void LWAX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        public void LWAUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        public void LMW(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            uint currentRegister = instruction.Rt;

            while (currentRegister <= 31) {
                Value value = Load32(ea);
                _irContext.SetGPR(currentRegister, ZeroExtend(value, Value.ValueType.I64));
                ea = Add(ea, Constant(4));
                currentRegister++;
            }
        }

        // Load Double Word Instructions
        public void LD(PPUInstruction instruction) {
            switch (instruction.Raw & 0x3) {
                case 0:
                case 1:
                    LoadDoubleWord(instruction);
                    break;

                //LoadWordAlgebraic
                case 2:
                    LWA(instruction);
                    break;
            }
        }

        public void LoadDoubleWord(PPUInstruction instruction) {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(instruction, MemAccessForm.DSForm, checkRa);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);

            if (update) {
                _irContext.SetGPR(instruction.Ra, ea);
            }
        }

        public void LDX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);
        }

        public void LDUX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);
            _irContext.SetGPR(instruction.Ra, ea);     // update
        }

        public void LDARX(PPUInstruction instruction) {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load64AndReserve(ea);
            _irContext.SetGPR(instruction.Rt, value);
        }
    }
}
