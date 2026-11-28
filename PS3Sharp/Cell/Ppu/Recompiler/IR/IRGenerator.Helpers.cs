namespace PS3Sharp.Cell.PPU.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        // Helpers

        internal Value CkdAdd(Value a, Value b, out Value carry)
        {
            // Add with carry helper
            Value result = Add(a, b);
            carry = SetLessThanUnsigned(result, a); // carry = result < a;
            return result;
        }

        internal Value WriteBit(Value destination, int bitNumber, Value flag)
        {
            // Clear bit                                                               
            Value mask = Constant(~(1u << bitNumber));
            destination = And(destination, mask);

            // Set bit to flag
            Value shiftCount = Constant((ulong)bitNumber);
            Value shiftedFlag = ShiftLeft(flag, shiftCount);
            return Or(destination, shiftedFlag);
        }

        internal Value ReadBit(Value source, int bitNumber)
        {
            Value shiftCount = Constant((ulong)bitNumber);
            Value shiftedSource = ShiftRight(source, shiftCount);
            Value mask = Constant(1);
            return And(shiftedSource, mask);
        }

        internal Value ReadBit(Value source, Value bitNumber)
        {
            Value shiftedSource = ShiftRight(source, bitNumber);
            return And(shiftedSource, _irContext.One);
        }

        internal Value GPRLoad(ulong gprNumber)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.GPRLoad, value, gprNumber);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void GPRStore(Value src, ulong gprNumber, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.GPRStore, src, gprNumber);
            _irContext.AddInstruction(instruction, index);
        }

        internal Value FPRLoad(ulong fprNumber)
        {
            Value value = _irContext.CreateValue(Value.ValueType.F64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.FPRLoad, value, fprNumber);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void FPRStore(Value src, ulong fprNumber, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.F64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.FPRStore, src, fprNumber);
            _irContext.AddInstruction(instruction, index);
        }


        internal Value CRLoad()
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CRLoad, value, 0);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void CRStore(Value src, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CRStore, src, 0);
            _irContext.AddInstruction(instruction, index);
        }

        internal Value CTRLoad()
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CTRLoad, value, 0);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void CTRStore(Value src, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CTRStore, src, 0);
            _irContext.AddInstruction(instruction, index);
        }

        internal Value LRLoad()
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.LRLoad, value, 0);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void LRStore(Value src, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.LRStore, src, 0);
            _irContext.AddInstruction(instruction, index);
        }

        internal Value XERLoad()
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.XERLoad, value, 0);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void XERStore(Value src, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.XERStore, src, 0);
            _irContext.AddInstruction(instruction, index);
        }

        internal Value VRSaveLoad()
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.VRSaveLoad, value, 0);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void VRSaveStore(Value src, int index = -1)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.VRSaveStore, src, 0);
            _irContext.AddInstruction(instruction, index);
        }

        internal void PCStore(Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.PCStore, src, 0);
            _irContext.AddInstruction(instruction);
        }

        internal Value Constant(ulong constant, Value.ValueType type = Value.ValueType.I64)
        {
            Value value = _irContext.CreateValue(type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Constant, value, constant);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Constant(long constant, Value.ValueType type = Value.ValueType.I64)
        {
            Value value = _irContext.CreateValue(type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Constant, value, (ulong)constant);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load8(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I8);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load8, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load16(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load16, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load16Reversed(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load16Rev, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load32(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load32, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load32Reversed(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load32Rev, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load32AndReserve(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load32Rx, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load64(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load64, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load64AndReserve(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load64Rx, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal Value Load128(Value virtualAddress)
        {
            Value value = _irContext.CreateValue(Value.ValueType.I128);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Load128, value, virtualAddress);
            _irContext.AddInstruction(instruction);
            return value;
        }

        internal void Store8(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I8);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store8, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal void Store16(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store16, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal void Store16Reversed(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store16Rev, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal void Store32(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store32, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal void Store32Reversed(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store32Rev, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal Value Store32Conditional(Value virtualAddress, Value src)
        {
            // This instruction returns success/failure
            EnsureSameType(src.Type, Value.ValueType.I32);
            Value success = _irContext.CreateValue(Value.ValueType.I64); // success flag is 64-bit
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store32CX, success, virtualAddress, src);
            _irContext.AddInstruction(instruction);
            return success;
        }

        internal void Store64(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store64, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal Value Store64Conditional(Value virtualAddress, Value src)
        {
            // This instruction returns success/failure
            EnsureSameType(src.Type, Value.ValueType.I64);
            Value success = _irContext.CreateValue(Value.ValueType.I64); // success flag is 64-bit
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store64CX, success, virtualAddress, src);
            _irContext.AddInstruction(instruction);
            return success;
        }

        internal void Store128(Value virtualAddress, Value src)
        {
            EnsureSameType(src.Type, Value.ValueType.I128);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Store128, virtualAddress, src);
            _irContext.AddInstruction(instruction);
        }

        internal Value And(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.And, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Or(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Or, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Xor(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Xor, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Nor(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Nor, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Not(Value a)
        {
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Not, result, a);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value ShiftLeft(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.ShiftLeft, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value ShiftRight(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.ShiftRight, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value ShiftRightArithmetic(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.ShiftRightArithmetic, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value RotateLeft(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.RotateLeft, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value RotateRight(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.RotateRight, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Add(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Add, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Sub(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Sub, result, a, b); // a - b
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value IMulLD(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.IMulLD, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value IMulHD(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.IMulHD, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value IMulLW(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.IMulLW, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value IMulHW(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.IMulHW, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value MulHD(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.MulHD, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value MulHW(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.MulHW, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value CheckedIDivD(Value a, Value b)
        {
            // CheckedIDivD is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CheckedIDivD, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value CheckedDivD(Value a, Value b)
        {
            // CheckedDivD is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CheckedDivD, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value CheckedIDivW(Value a, Value b)
        {
            // CheckedIDivW is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CheckedIDivW, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value CheckedDivW(Value a, Value b)
        {
            // CheckedDivW is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CheckedDivW, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value Select(Value condition, Value trueValue, Value falseValue)
        {
            EnsureSameType(trueValue, falseValue);
            Value result = _irContext.CreateValue(trueValue.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Select, result, condition, trueValue, falseValue);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetEqual(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetEq, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetNotEqual(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetNotEq, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetLessThanUnsigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetLessThanUnsigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetLessThanEqUnsigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetLessThanEqUnsigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetGreaterThanUnsigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetGreaterThanUnsigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetGreaterThanEqUnsigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetGreaterThanEqUnsigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetLessThanSigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetLessThanSigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetLessThanEqSigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetLessThanEqSigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetGreaterThanSigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetGreaterThanSigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SetGreaterThanEqSigned(Value a, Value b)
        {
            EnsureSameType(a, b);
            Value result = _irContext.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SetGreaterThanEqSigned, result, a, b);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal Value SignExtend(Value a, Value.ValueType type)
        {
            int sourceSize = Value.GetValueSize(a.Type);
            int destinationSize = Value.GetValueSize(type);

            if (destinationSize > sourceSize)
            {
                Value result = _irContext.CreateValue(type);
                IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.SignExtend, result, a);
                _irContext.AddInstruction(instruction);
                return result;

            }
            else
            {
                throw new Exception($"[IR] Invalid sign extension from {a.Type} to {type}");
            }
        }

        internal Value ZeroExtend(Value a, Value.ValueType type)
        {
            int sourceSize = Value.GetValueSize(a.Type);
            int destinationSize = Value.GetValueSize(type);

            if (destinationSize > sourceSize)
            {
                Value result = _irContext.CreateValue(type);
                IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.ZeroExtend, result, a);
                _irContext.AddInstruction(instruction);
                return result;

            }
            else
            {
                throw new Exception($"[IR] Invalid zero extension from {a.Type} to {type}");
            }
        }

        internal Value Truncate(Value a, Value.ValueType type)
        {
            int sourceSize = Value.GetValueSize(a.Type);
            int destinationSize = Value.GetValueSize(type);

            if (destinationSize < sourceSize)
            {
                Value result = _irContext.CreateValue(type);
                IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Truncate, result, a);
                _irContext.AddInstruction(instruction);
                return result;

            }
            else
            {
                throw new Exception($"[IR] Invalid truncation from {a.Type} to {type}");
            }
        }

        internal Value CountLeadingZeroes(Value a)
        {
            Value result = _irContext.CreateValue(Value.ValueType.I64);    // count is 64-bit
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.CNTLZ, result, a);
            _irContext.AddInstruction(instruction);
            return result;
        }

        internal void IRSyscall(PPUInstruction ppcInstruction, uint address)
        {
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Syscall, default, ppcInstruction.Raw, address);
            _irContext.AddInstruction(instruction);
        }

        internal void InterpreterFallback(PPUInstruction ppcInstruction)
        {
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.InterpreterFallback, default, ppcInstruction.Raw);
            _irContext.AddInstruction(instruction);

            // temporary
            throw new Exception($"Interpreter Fallback: Op = {ppcInstruction.Op} Raw = 0x{ppcInstruction.Raw:X8} ");
        }

        internal void Return()
        {
            IRInstruction instruction = IRInstruction.Create(IRInstruction.Op.Ret, default, 0);
            _irContext.AddInstruction(instruction);
        }

        // Static helpers
        internal static bool IsBranch(PPUInstruction instruction)
        {
            Op op = instruction.Op;

            if (op == Op.G13)
            {
                G13 g13 = instruction.G13;
                return g13 == G13.BCCTR || g13 == G13.BCLR;

            }
            else
            {
                return op == Op.Branch || op == Op.BC;
            }
        }

        internal static bool IsSyscall(PPUInstruction instruction)
        {
            Op op = instruction.Op;
            return op == Op.SC;
        }

        internal static void EnsureSameType(Value a, Value b)
        {
            if (a.Type != b.Type)
            {
                throw new InvalidOperationException($"[IR] Type check failed: {a.Type} - {b.Type}");
            }
        }

        internal static void EnsureSameType(Value.ValueType typeA, Value.ValueType typeB)
        {
            if (typeA != typeB)
            {
                throw new InvalidOperationException($"[IR] Type check failed: {typeA} - {typeB}");
            }
        }

        internal static bool IsRegularStore(IRInstruction instruction)
        {
            bool isStore =
                instruction.Opcode == IRInstruction.Op.Store8 ||
                instruction.Opcode == IRInstruction.Op.Store16 ||
                instruction.Opcode == IRInstruction.Op.Store16Rev ||
                instruction.Opcode == IRInstruction.Op.Store32 ||
                instruction.Opcode == IRInstruction.Op.Store32Rev ||
                instruction.Opcode == IRInstruction.Op.Store32CX ||
                instruction.Opcode == IRInstruction.Op.Store64 ||
                instruction.Opcode == IRInstruction.Op.Store64CX ||
                instruction.Opcode == IRInstruction.Op.Store128;

            return isStore;
        }

        internal static bool IsSpecialRegisterStore(IRInstruction instruction)
        {
            bool isStore =
                instruction.Opcode == IRInstruction.Op.GPRStore ||
                instruction.Opcode == IRInstruction.Op.FPRStore ||
                instruction.Opcode == IRInstruction.Op.CRStore ||
                instruction.Opcode == IRInstruction.Op.CTRStore ||
                instruction.Opcode == IRInstruction.Op.XERStore ||
                instruction.Opcode == IRInstruction.Op.LRStore ||
                instruction.Opcode == IRInstruction.Op.VRSaveStore ||
                instruction.Opcode == IRInstruction.Op.PCStore;


            return isStore;
        }

        internal static bool IsSpecialRegisterLoad(IRInstruction instruction)
        {
            bool isStore =
                instruction.Opcode == IRInstruction.Op.GPRLoad ||
                instruction.Opcode == IRInstruction.Op.FPRLoad ||
                instruction.Opcode == IRInstruction.Op.CRLoad ||
                instruction.Opcode == IRInstruction.Op.CTRLoad ||
                instruction.Opcode == IRInstruction.Op.XERLoad ||
                instruction.Opcode == IRInstruction.Op.LRLoad ||
                instruction.Opcode == IRInstruction.Op.VRSaveLoad;


            return isStore;
        }


        internal static bool IsMemoryOp(IRInstruction instruction)
        {
            bool isMemOp =
                instruction.Opcode == IRInstruction.Op.Load8 ||
                instruction.Opcode == IRInstruction.Op.Load16 ||
                instruction.Opcode == IRInstruction.Op.Load16Rev ||
                instruction.Opcode == IRInstruction.Op.Load32 ||
                instruction.Opcode == IRInstruction.Op.Load32Rev ||
                instruction.Opcode == IRInstruction.Op.Load32Rx ||
                instruction.Opcode == IRInstruction.Op.Load64 ||
                instruction.Opcode == IRInstruction.Op.Load64Rx ||
                instruction.Opcode == IRInstruction.Op.Load128 ||
                instruction.Opcode == IRInstruction.Op.Store8 ||
                instruction.Opcode == IRInstruction.Op.Store16 ||
                instruction.Opcode == IRInstruction.Op.Store16Rev ||
                instruction.Opcode == IRInstruction.Op.Store32 ||
                instruction.Opcode == IRInstruction.Op.Store32Rev ||
                instruction.Opcode == IRInstruction.Op.Store32CX ||
                instruction.Opcode == IRInstruction.Op.Store64 ||
                instruction.Opcode == IRInstruction.Op.Store64CX ||
                instruction.Opcode == IRInstruction.Op.Store128;

            return isMemOp;
        }

        internal static bool HasCalls(List<IRInstruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (RequiresCall(instructions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasMemoryOp(List<IRInstruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (IsMemoryOp(instructions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool RequiresCall(IRInstruction instruction)
        {
            bool requresCall =
                instruction.Opcode == IRInstruction.Op.Syscall ||
                instruction.Opcode == IRInstruction.Op.InterpreterFallback;

            return requresCall;
        }

        internal static int GetNumberOfValidOperands(IRInstruction instruction)
        {
            int operands = 0;

            if (instruction.Operand1.IsValid)
            {
                operands++;
            }

            if (instruction.Operand2.IsValid)
            {
                operands++;
            }

            if (instruction.Operand3.IsValid)
            {
                operands++;
            }

            return operands;
        }

    }
}
