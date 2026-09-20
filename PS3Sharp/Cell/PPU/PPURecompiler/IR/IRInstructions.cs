using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static class IRInstructions {
        public enum Op {
            GPRLoad,
            GPRStore,

            FPRLoad,
            FPRStore,

            CRLoad,
            CRStore,

            CTRLoad,
            CTRStore,

            LRLoad,
            LRStore,

            XERLoad,
            XERStore,

            VRSaveLoad,
            VRSaveStore,

            PCStore,

            Constant,

            Add,
            Sub,

            IMulLD, // Signed 64x64 - takes low 64 of result
            IMulHD, // Signed 64x64 - takes high 64 of result
            IMulLW, // Signed 32x32 - takes low 32 of result
            IMulHW, // Signed 32x32 - takes high 32 of result
            MulHD,  // Unsigned 64x64 - takes high 64 of result
            MulHW,  // Unsigned 32x32 - takes high 32 of result

            IDiv,
            Div,
            CheckedIDiv,
            CheckedDiv,

            And,
            Or,
            Xor,
            Nor,
            Not,

            ShiftLeft,
            ShiftRight,
            ShiftRightArithmetic,
            RotateLeft,
            RotateRight,

            SetEq,
            SetNotEq,
            SetLessThanUnsigned,
            SetLessThanEqUnsigned,
            SetGreaterThanUnsigned,
            SetGreaterThanEqUnsigned,
            SetLessThanSigned,
            SetLessThanEqSigned,
            SetGreaterThanSigned,
            SetGreaterThanEqSigned,

            SignExtend,
            ZeroExtend,
            Truncate,

            Select,

            Load8,
            Load16,
            Load16Rev,
            Load32,
            Load32Rev,
            Load32Rx,
            Load64,
            Load64Rx,
            Load128,

            Store8,
            Store16,
            Store16Rev,
            Store32,
            Store32Rev,
            Store32CX,
            Store64,
            Store64CX,
            Store128,

            CNTLZ,  

            Compare,

            //Internal control flow inside IR, these are not for PPC branches
            Branch,         
            BranchConditional,

            Syscall,
            Phi,
            InterpreterFallback,
            Ret,
        }

        public struct IRInstruction {
            public Op Op;

            public ulong Immediate1;
            public ulong Immediate2;
            public ulong Immediate3;

            public Value Operand1;
            public Value Operand2;
            public Value Operand3;
            public Value Destination;

            public static IRInstruction Create(Op op, Value destination, Value a = default, Value b = default, Value c = default, ulong constant1 = 0, ulong constant2 = 0, ulong constant3 = 0) {
                return new IRInstruction {
                    Op = op,
                    Destination = destination,
                    Operand1 = a,
                    Operand2 = b,
                    Operand3 = c,
                    Immediate1 = constant1,
                    Immediate2 = constant2,
                    Immediate3 = constant3,
                };
            }

            public static IRInstruction Create(Op op, Value destination, ulong constant1 = 0, ulong constant2 = 0, ulong constant3 = 0) {
                return new IRInstruction {
                    Op = op,
                    Destination = destination,
                    Immediate1 = constant1,
                    Immediate2 = constant2,
                    Immediate3 = constant3,
                };
            }
        }

        public readonly struct Value : IEquatable<Value> {
            public enum ValueType {
                None,
                I8,
                I16,
                I32,
                I64,
                I128,
                F32,
                F64,
                Vector128,
            }

            public readonly int Id = -1;
            public readonly ValueType Type;

            public bool IsValid => Id > 0;

            public static Value Invalid => new Value(-1, ValueType.None);

            public Value(int id, ValueType type) {
                Id = id;
                Type = type;
            }

            public static int GetValueSize(ValueType type) {
                switch (type) {
                    case ValueType.None: 
                        return 0;

                    case ValueType.I8:
                        return 1;

                    case ValueType.I16:
                        return 2;
                            
                    case ValueType.I32: 
                    case ValueType.F32: 
                        return 4;

                    case ValueType.I64: 
                    case ValueType.F64: 
                        return 8;

                    case ValueType.I128: 
                    case ValueType.Vector128: 
                        return 16;

                    default:
                        return -1;
                }
            }

            public static int GetBitCount(ValueType type) {
                switch (type) {
                    case ValueType.None: 
                        return 0;

                    case ValueType.I8:
                        return 8;

                    case ValueType.I16:
                        return 16;

                    case ValueType.I32: 
                    case ValueType.F32: 
                        return 32;

                    case ValueType.I64: 
                    case ValueType.F64: 
                        return 64;

                    case ValueType.I128: 
                    case ValueType.Vector128: 
                        return 128;

                    default:
                        return -1;
                }
            }
            
            public bool Equals(Value other) => Id == other.Id && Type == other.Type;
            public override bool Equals(object? obj) => obj is Value other && Equals(other);
            public override int GetHashCode() => Id;
            public override string ToString() => $"v{Id}";


            public static bool operator ==(Value left, Value right) {
                return left.Equals(right);
            }

            public static bool operator !=(Value left, Value right) {
                return !(left == right);
            }
        }

        public static Value GPRLoad(IRContext ctx, ulong gprNumber) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.GPRLoad, value, gprNumber);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void GPRStore(IRContext ctx, Value src, ulong gprNumber) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.GPRStore, src, gprNumber);
            ctx.AddInstruction(instruction);
        }

        public static Value FPRLoad(IRContext ctx, ulong fprNumber) {
            Value value = ctx.CreateValue(Value.ValueType.F64);
            IRInstruction instruction = IRInstruction.Create(Op.FPRLoad, value, fprNumber);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void FPRStore(IRContext ctx, Value src, ulong fprNumber) {
            EnsureSameType(src.Type, Value.ValueType.F64);
            IRInstruction instruction = IRInstruction.Create(Op.FPRStore, src, fprNumber);
            ctx.AddInstruction(instruction);
        }


        public static Value CRLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CRLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void CRStore(IRContext ctx, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CRStore, src, 0);
            ctx.AddInstruction(instruction);
        }

        public static Value CTRLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CTRLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void CTRStore(IRContext ctx, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.CTRStore, src, 0);
            ctx.AddInstruction(instruction);
        }

        public static Value LRLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.LRLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void LRStore(IRContext ctx, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.LRStore, src, 0);
            ctx.AddInstruction(instruction);
        }

        public static Value XERLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.XERLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void XERStore(IRContext ctx, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.XERStore, src, 0);
            ctx.AddInstruction(instruction);
        }

        public static Value VRSaveLoad(IRContext ctx) {
            Value value = ctx.CreateValue(Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.VRSaveLoad, value, 0);
            ctx.AddInstruction(instruction);
            return value;
        }

        public static void VRSaveStore(IRContext ctx, Value src) {
            EnsureSameType(src.Type, Value.ValueType.I64);
            IRInstruction instruction = IRInstruction.Create(Op.VRSaveStore, src, 0);
            ctx.AddInstruction(instruction);
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

        public static Value CheckedIDiv(IRContext ctx, Value a, Value b) {
            // CheckedIDiv is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.CheckedIDiv, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value CheckedDiv(IRContext ctx, Value a, Value b) {
            // CheckedDiv is an IR operation whose semantics guarantee 
            // that the invalid PPC division cases produce zero.

            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.CheckedDiv, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value IDiv(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.IDiv, result, a, b);
            ctx.AddInstruction(instruction);
            return result;
        }

        public static Value Div(IRContext ctx, Value a, Value b) {
            EnsureSameType(a, b);
            Value result = ctx.CreateValue(a.Type);
            IRInstruction instruction = IRInstruction.Create(Op.Div, result, a, b);
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
    }
}
