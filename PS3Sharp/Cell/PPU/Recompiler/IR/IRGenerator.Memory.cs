using static PS3Sharp.Cell.PPU.Interpreter.PPUInterpreter;

namespace PS3Sharp.Cell.PPU.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal Value GetEa(PPUInstruction instruction, MemAccessForm form, bool checkRA)
        {

            Value baseAddress;
            if (checkRA)
            {
                baseAddress = instruction.Ra != 0 ? _irContext.GetGPR(instruction.Ra) : _irContext.Zero;
            }
            else
            {
                baseAddress = _irContext.GetGPR(instruction.Ra);
            }


            Value offs;
            Value ea;

            switch (form)
            {
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
        internal void STB(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
        }

        internal void STBX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
        }

        internal void STBU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STBUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        // Store Half Instructions
        internal void STH(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
        }

        internal void STHX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
        }

        internal void STHU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STHUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STHBRX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16Reversed(ea, Truncate(src, Value.ValueType.I16));
        }

        // Store Word Instructions
        internal void STW(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
        }

        internal void STWX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
        }

        internal void STWU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STWUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STWBRX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32Reversed(ea, Truncate(src, Value.ValueType.I32));
        }

        internal void STWCX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Value success = Store32Conditional(ea, Truncate(src, Value.ValueType.I32));
            Value cr0 = Select(success, _irContext.CompareResult_Eq, _irContext.Zero);
            _irContext.WriteCRField(PPUThread.CRNumber.CR0, cr0);
        }

        // Store Double Word Instructions
        internal void STD(PPUInstruction instruction)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(instruction, MemAccessForm.DSForm, checkRa);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);

            if (update)
            {
                _irContext.SetGPR(instruction.Ra, ea);
            }
        }

        internal void STDX(PPUInstruction instruction)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(instruction, MemAccessForm.XForm, checkRa);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);

            if (update)
            {
                _irContext.SetGPR(instruction.Ra, ea);
            }
        }

        internal void STDUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);
            _irContext.SetGPR(instruction.Ra, ea);     // update
        }

        internal void STDCX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Value success = Store64Conditional(ea, src);
            Value cr0 = Select(success, _irContext.CompareResult_Eq, _irContext.Zero);
            _irContext.WriteCRField(PPUThread.CRNumber.CR0, cr0);
        }

        // Load Byte Instructions
        internal void LBZ(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LBZX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LBZU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LBZUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        // Load Half Instructions
        internal void LHZ(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LHZX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LHZU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LHZUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LHBRX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16Reversed(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LHA(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LHAX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LHAU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LHAUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }


        // Load Word Instructions
        internal void LWZ(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWZX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWZU(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LWZUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LWBRX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32Reversed(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWARX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32AndReserve(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWA(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LWAX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LWAUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LMW(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            uint currentRegister = instruction.Rt;

            while (currentRegister <= 31)
            {
                Value value = Load32(ea);
                _irContext.SetGPR(currentRegister, ZeroExtend(value, Value.ValueType.I64));
                ea = Add(ea, Constant(4));
                currentRegister++;
            }
        }

        // Load Double Word Instructions
        internal void LD(PPUInstruction instruction)
        {
            switch (instruction.Raw & 0x3)
            {
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

        internal void LoadDoubleWord(PPUInstruction instruction)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            Value ea = GetEa(instruction, MemAccessForm.DSForm, checkRa);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);

            if (update)
            {
                _irContext.SetGPR(instruction.Ra, ea);
            }
        }

        internal void LDX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);
        }

        internal void LDUX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);
            _irContext.SetGPR(instruction.Ra, ea);     // update
        }

        internal void LDARX(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load64AndReserve(ea);
            _irContext.SetGPR(instruction.Rt, value);
        }
    }
}
