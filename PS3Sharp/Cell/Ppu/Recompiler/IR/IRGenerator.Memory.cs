using static PS3Sharp.Cell.Ppu.Interpreter.PpuInterpreter;

namespace PS3Sharp.Cell.Ppu.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal Value GetEa(PpuInstruction instruction, MemAccessForm form, bool checkRA)
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
                    offs = Constant((long)(short)(instruction.Ds << 2));   // sign extended
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
        internal void STB(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
        }

        internal void STBX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
        }

        internal void STBU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STBUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store8(ea, Truncate(src, Value.ValueType.I8));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        // Store Half Instructions
        internal void STH(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
        }

        internal void STHX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
        }

        internal void STHU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STHUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16(ea, Truncate(src, Value.ValueType.I16));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STHBRX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store16Reversed(ea, Truncate(src, Value.ValueType.I16));
        }

        // Store Word Instructions
        internal void STW(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
        }

        internal void STWX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
        }

        internal void STWU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STWUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32(ea, Truncate(src, Value.ValueType.I32));
            _irContext.SetGPR(instruction.Ra, ea); // update
        }

        internal void STWBRX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store32Reversed(ea, Truncate(src, Value.ValueType.I32));
        }

        internal void STWCX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Value success = Store32Conditional(ea, Truncate(src, Value.ValueType.I32));
            Value cr0 = Select(success, _irContext.CompareResult_Eq, _irContext.Zero);
            _irContext.WriteCRField(PpuThread.CrNumber.CR0, cr0);
        }

        // Store Double Word Instructions
        internal void STD(PpuInstruction instruction)
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

        internal void STDX(PpuInstruction instruction)
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

        internal void STDUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value src = _irContext.GetGPR(instruction.Rs);
            Store64(ea, src);
            _irContext.SetGPR(instruction.Ra, ea);     // update
        }

        internal void STDCX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value src = _irContext.GetGPR(instruction.Rs);
            Value success = Store64Conditional(ea, src);
            Value cr0 = Select(success, _irContext.CompareResult_Eq, _irContext.Zero);
            _irContext.WriteCRField(PpuThread.CrNumber.CR0, cr0);
        }

        // Load Byte Instructions
        internal void LBZ(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LBZX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LBZU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LBZUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load8(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        // Load Half Instructions
        internal void LHZ(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LHZX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LHZU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LHZUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LHBRX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16Reversed(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LHA(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LHAX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LHAU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LHAUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load16(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }


        // Load Word Instructions
        internal void LWZ(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWZX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWZU(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LWZUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LWBRX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32Reversed(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWARX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32AndReserve(ea);
            _irContext.SetGPR(instruction.Rt, ZeroExtend(value, Value.ValueType.I64));
        }

        internal void LWA(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LWAX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
        }

        internal void LWAUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load32(ea);
            _irContext.SetGPR(instruction.Rt, SignExtend(value, Value.ValueType.I64));    // Sign extends
            _irContext.SetGPR(instruction.Ra, ea);    // update
        }

        internal void LMW(PpuInstruction instruction)
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
        internal void LD(PpuInstruction instruction)
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

        internal void LoadDoubleWord(PpuInstruction instruction)
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

        internal void LDX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);
        }

        internal void LDUX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, false);
            Value value = Load64(ea);
            _irContext.SetGPR(instruction.Rt, value);
            _irContext.SetGPR(instruction.Ra, ea);     // update
        }

        internal void LDARX(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.XForm, true);
            Value value = Load64AndReserve(ea);
            _irContext.SetGPR(instruction.Rt, value);
        }
    }
}
