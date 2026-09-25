using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRInstructions {
        // Helpers
        public static Value GPRLoad(IRContext ctx, ulong gprNumber) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.GPRLoad, value, gprNumber);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void GPRStore(IRContext ctx, Value src, ulong gprNumber, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.GPRStore, src, gprNumber);
            ctx.AddInstruction(instruction, index);
        }

        public static Value FPRLoad(IRContext ctx, ulong fprNumber) {
            Value value = ctx.CreateValue(Value.ValueType.F64);
            IRInstruction instruction = IRInstruction.Create(Op.FPRLoad, value, fprNumber);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void FPRStore(IRContext ctx, Value src, ulong fprNumber, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.F64);
            IRInstruction instruction = IRInstruction.Create(Op.FPRStore, src, fprNumber);
            ctx.AddInstruction(instruction, index);
        }


        public static Value CRLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CRLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void CRStore(IRContext ctx, Value src, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CRStore, src, 0);
            ctx.AddInstruction(instruction, index);
        }

        public static Value CTRLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CTRLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void CTRStore(IRContext ctx, Value src, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CTRStore, src, 0);
            ctx.AddInstruction(instruction, index);
        }

        public static Value LRLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.LRLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void LRStore(IRContext ctx, Value src, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.LRStore, src, 0);
            ctx.AddInstruction(instruction, index);
        }

        public static Value XERLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.XERLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void XERStore(IRContext ctx, Value src, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.XERStore, src, 0);
            ctx.AddInstruction(instruction, index);
        }

        public static Value VRSaveLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.VRSaveLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void VRSaveStore(IRContext ctx, Value src, int index = -1) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.VRSaveStore, src, 0);
            ctx.AddInstruction(instruction, index);
        }

        public static void PCStore(IRContext ctx, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.PCStore, src, 0);
            ctx.AddInstruction(instruction);
        }

        public static Value Constant(IRContext ctx, ulong constant, Value.ValueType type = Value.ValueType.I64) {
            Value value = ctx.CreateValue(type);
            IRInstruction instruction = IRInstruction.Create(Op.Constant, value, constant);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Constant(IRContext ctx, long constant, Value.ValueType type = Value.ValueType.I64) {
            Value value = ctx.CreateValue(type);
            IRInstruction instruction = IRInstruction.Create(Op.Constant, value, (ulong)constant);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load8(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I8);
            IRInstruction instruction = IRInstruction.Create(Op.Load8, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load16(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(Op.Load16, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load16Reversed(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(Op.Load16Rev, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load32(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(Op.Load32, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load32Reversed(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(Op.Load32Rev, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load32AndReserve(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(Op.Load32Rx, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load64(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.Load64, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load64AndReserve(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.Load64Rx, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static Value Load128(IRContext ctx, Value virtualAddress) {
            Value value = ctx.CreateValue(Value.ValueType.I128);
            IRInstruction instruction = IRInstruction.Create(Op.Load128, value, virtualAddress);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void Store8(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I8);
            IRInstruction instruction = IRInstruction.Create(Op.Store8, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static void Store16(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(Op.Store16, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static void Store16Reversed(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I16);
            IRInstruction instruction = IRInstruction.Create(Op.Store16Rev, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static void Store32(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(Op.Store32, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static void Store32Reversed(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I32);
            IRInstruction instruction = IRInstruction.Create(Op.Store32Rev, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static Value Store32Conditional(IRContext ctx, Value virtualAddress, Value src) {
            // This instruction returns success/failure
            EnsureSameType(src.Type, Value.ValueType.I32);
            Value success = ctx.CreateValue(Value.ValueType.I64); // success flag is 64-bit
            IRInstruction instruction = IRInstruction.Create(Op.Store32CX, success, virtualAddress, src);
            ctx.AddInstruction(instruction);
            return success;
        }

        public static void Store64(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.Store64, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static Value Store64Conditional(IRContext ctx, Value virtualAddress, Value src) {
            // This instruction returns success/failure
            EnsureSameType(src.Type, Value.ValueType.I64);
            Value success = ctx.CreateValue(Value.ValueType.I64); // success flag is 64-bit
            IRInstruction instruction = IRInstruction.Create(Op.Store64CX, success, virtualAddress, src);
            ctx.AddInstruction(instruction);
            return success;
        }

        public static void Store128(IRContext ctx, Value virtualAddress, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I128);
            IRInstruction instruction = IRInstruction.Create(Op.Store128, virtualAddress, src);
            ctx.AddInstruction(instruction);
        }

        public static Value And(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.And, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Or(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Or, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Xor(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Xor, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Nor(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Nor, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Not(IRContext ctx, Value a) {
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Not, result, a);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value ShiftLeft(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.ShiftLeft, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value ShiftRight(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.ShiftRight, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value ShiftRightArithmetic(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.ShiftRightArithmetic, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value RotateLeft(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.RotateLeft, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value RotateRight(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.RotateRight, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Add(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Add, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Sub(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Sub, result, a, b); // a - b
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value IMulLD(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.IMulLD, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value IMulHD(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.IMulHD, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value IMulLW(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.IMulLW, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value IMulHW(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.IMulHW, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value MulHD(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.MulHD, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value MulHW(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.MulHW, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value CheckedIDivD(IRContext ctx, Value a, Value b) {
            // CheckedIDivD is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.CheckedIDivD, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value CheckedDivD(IRContext ctx, Value a, Value b) {
            // CheckedDivD is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.CheckedDivD, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value CheckedIDivW(IRContext ctx, Value a, Value b) {
            // CheckedIDivW is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.CheckedIDivW, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value CheckedDivW(IRContext ctx, Value a, Value b) {
            // CheckedDivW is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.CheckedDivW, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Select(IRContext ctx, Value condition, Value trueValue, Value falseValue) {
            EnsureSameType(trueValue, falseValue);
            Value result = ctx.CreateValue(trueValue.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Select, result, condition, trueValue, falseValue);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetEqual(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetEq, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetNotEqual(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetNotEq, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetLessThanUnsigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetLessThanUnsigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetLessThanEqUnsigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetLessThanEqUnsigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetGreaterThanUnsigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetGreaterThanUnsigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetGreaterThanEqUnsigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetGreaterThanEqUnsigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetLessThanSigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetLessThanSigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetLessThanEqSigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetLessThanEqSigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetGreaterThanSigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetGreaterThanSigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SetGreaterThanEqSigned(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.SetGreaterThanEqSigned, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value SignExtend(IRContext ctx, Value a, Value.ValueType type) {
            int sourceSize = Value.GetValueSize(a.Type);
            int destinationSize = Value.GetValueSize(type);

            if (destinationSize > sourceSize) {
                Value result = ctx.CreateValue(type);
                IRInstruction instruction = IRInstruction.Create(Op.SignExtend, result, a);
                ctx.AddInstruction(instruction);
                return result;

            } else {
                throw new Exception($"[IR] Invalid sign extension from {a.Type} to {type}");
            }
        }

        public static Value ZeroExtend(IRContext ctx, Value a, Value.ValueType type) {
            int sourceSize = Value.GetValueSize(a.Type);
            int destinationSize = Value.GetValueSize(type);

            if (destinationSize > sourceSize) {
                Value result = ctx.CreateValue(type);
                IRInstruction instruction = IRInstruction.Create(Op.ZeroExtend, result, a);
                ctx.AddInstruction(instruction);
                return result;

            } else {
                throw new Exception($"[IR] Invalid zero extension from {a.Type} to {type}");
            }
        }

        public static Value Truncate(IRContext ctx, Value a, Value.ValueType type) {
            int sourceSize = Value.GetValueSize(a.Type);
            int destinationSize = Value.GetValueSize(type);

            if (destinationSize < sourceSize) {
                Value result = ctx.CreateValue(type);
                IRInstruction instruction = IRInstruction.Create(Op.Truncate, result, a);
                ctx.AddInstruction(instruction);
                return result;

            } else {
                throw new Exception($"[IR] Invalid truncation from {a.Type} to {type}");
            }
        }

        public static Value CountLeadingZeroes(IRContext ctx, Value a) {
            Value result = ctx.CreateValue(Value.ValueType.I64);    // count is 64-bit
            IRInstruction instruction = IRInstruction.Create(Op.CNTLZ, result, a);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static void IRSyscall(IRContext ctx, PPUInstruction ppcInstruction, uint address) {
            IRInstruction instruction = IRInstruction.Create(Op.Syscall, default, ppcInstruction.Raw, address);
            ctx.AddInstruction(instruction);
        }

        public static void InterpreterFallback(IRContext ctx, PPUInstruction ppcInstruction) {
            IRInstruction instruction = IRInstruction.Create(Op.InterpreterFallback, default, ppcInstruction.Raw);
            ctx.AddInstruction(instruction);

            // temporary
            throw new Exception($"Interpreter Fallback: Op = {ppcInstruction.Op} Raw = 0x{ppcInstruction.Raw:X8} ");
        }

        public static void Return(IRContext ctx) {
            IRInstruction instruction = IRInstruction.Create(Op.Ret, default, 0);
            ctx.AddInstruction(instruction);
        }

        public static bool IsRegularStore(IRInstruction instruction) {
            bool isStore =
                instruction.Op == IRInstructions.Op.Store8 ||
                instruction.Op == IRInstructions.Op.Store16 ||
                instruction.Op == IRInstructions.Op.Store16Rev ||
                instruction.Op == IRInstructions.Op.Store32 ||
                instruction.Op == IRInstructions.Op.Store32Rev ||
                instruction.Op == IRInstructions.Op.Store32CX ||
                instruction.Op == IRInstructions.Op.Store64 ||
                instruction.Op == IRInstructions.Op.Store64CX ||
                instruction.Op == IRInstructions.Op.Store128;

            return isStore;
        }

        public static bool IsSpecialRegisterStore(IRInstruction instruction) {
            bool isStore =
                instruction.Op == IRInstructions.Op.GPRStore ||
                instruction.Op == IRInstructions.Op.FPRStore ||
                instruction.Op == IRInstructions.Op.CRStore ||
                instruction.Op == IRInstructions.Op.CTRStore ||
                instruction.Op == IRInstructions.Op.XERStore ||
                instruction.Op == IRInstructions.Op.LRStore ||
                instruction.Op == IRInstructions.Op.VRSaveStore ||
                instruction.Op == IRInstructions.Op.PCStore;


            return isStore;
        }

        public static bool IsSpecialRegisterLoad(IRInstruction instruction) {
            bool isStore =
                instruction.Op == IRInstructions.Op.GPRLoad ||
                instruction.Op == IRInstructions.Op.FPRLoad ||
                instruction.Op == IRInstructions.Op.CRLoad ||
                instruction.Op == IRInstructions.Op.CTRLoad ||
                instruction.Op == IRInstructions.Op.XERLoad ||
                instruction.Op == IRInstructions.Op.LRLoad ||
                instruction.Op == IRInstructions.Op.VRSaveLoad;


            return isStore;
        }


        public static bool IsMemoryOp(IRInstruction instruction) {
            bool isMemOp =
                instruction.Op == IR.IRInstructions.Op.Load8 ||
                instruction.Op == IR.IRInstructions.Op.Load16 ||
                instruction.Op == IR.IRInstructions.Op.Load16Rev ||
                instruction.Op == IR.IRInstructions.Op.Load32 ||
                instruction.Op == IR.IRInstructions.Op.Load32Rev ||
                instruction.Op == IR.IRInstructions.Op.Load32Rx ||
                instruction.Op == IR.IRInstructions.Op.Load64 ||
                instruction.Op == IR.IRInstructions.Op.Load64Rx ||
                instruction.Op == IR.IRInstructions.Op.Load128 ||
                instruction.Op == IR.IRInstructions.Op.Store8 ||
                instruction.Op == IR.IRInstructions.Op.Store16 ||
                instruction.Op == IR.IRInstructions.Op.Store16Rev ||
                instruction.Op == IR.IRInstructions.Op.Store32 ||
                instruction.Op == IR.IRInstructions.Op.Store32Rev ||
                instruction.Op == IR.IRInstructions.Op.Store32CX ||
                instruction.Op == IR.IRInstructions.Op.Store64 ||
                instruction.Op == IR.IRInstructions.Op.Store64CX ||
                instruction.Op == IR.IRInstructions.Op.Store128;

            return isMemOp;
        }

        public static bool HasCalls(List<IRInstruction> instructions) {
            for (int i = 0; i < instructions.Count; i++) {
                if (RequiresCall(instructions[i])) {
                    return true;
                }
            }

            return false;
        }

        public static bool HasMemoryOp(List<IRInstruction> instructions) {
            for (int i = 0; i < instructions.Count; i++) {
                if (IsMemoryOp(instructions[i])) {
                    return true;
                }
            }

            return false;
        }

        public static bool RequiresCall(IRInstruction instruction) {
            bool requresCall =
                instruction.Op == IR.IRInstructions.Op.Syscall ||
                instruction.Op == IR.IRInstructions.Op.InterpreterFallback;

            return requresCall;
        }

        public static int GetNumberOfValidOperands(IRInstruction instruction) {
            int operands = 0;

            if (instruction.Operand1.IsValid) {
                operands++;
            }

            if (instruction.Operand2.IsValid) {
                operands++;
            }

            if (instruction.Operand3.IsValid) {
                operands++;
            }

            return operands;
        }

    }
}
