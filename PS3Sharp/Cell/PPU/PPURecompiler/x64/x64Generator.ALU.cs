using Iced.Intel;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;
using static PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64 {
    public static unsafe partial class x64Generator {
        private static void Add(Assembler asm, AssemblerRegister64 destReg, AssemblerRegister64 srcReg, ulong imm) {
            asm.mov(destReg, imm);
            asm.add(destReg, srcReg);
        }

        private static void Add(Assembler asm, AssemblerRegister64 destReg, AssemblerRegister64 src1Reg, AssemblerRegister64 src2Reg) {
            asm.mov(destReg, src1Reg);
            asm.add(destReg, src2Reg);
        }

        private static void CommutativeOp(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 reg2;

            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                reg2 = _registerMap[op2].GetRegister();

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                reg2 = _registerMap[op1].GetRegister();

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                reg2 = _registerMap[op2].GetRegister();
            }

            switch (instruction.Op) {
                case IR.IRInstructions.Op.Add:
                    asm.add(dest, reg2);
                    break;

                case IR.IRInstructions.Op.And:
                    asm.and(dest, reg2);
                    break;

                case IR.IRInstructions.Op.Or:
                    asm.or(dest, reg2);
                    break;

                case IR.IRInstructions.Op.Xor:
                    asm.xor(dest, reg2);
                    break;

                default:
                    throw new NotImplementedException($"Unknown CommutativeOp = {instruction.Op}");
            }
        }

        private static void Sub(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 reg2;


            if (op1Const) {
                // A - B
                asm.mov(dest, _knownConstants[op1]);
                asm.sub(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                // - B + A
                asm.mov(dest, -(long)_knownConstants[op2]);
                asm.add(dest, _registerMap[op1].GetRegister());

            } else {
                // A - B
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.sub(dest, _registerMap[op2].GetRegister());
            }
        }

        private static void Not(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            bool op1Const = _knownConstants.ContainsKey(op1);

            AssemblerRegister64 dest = destination.GetRegister();

            if (op1Const) {
                asm.mov(dest, ~ _knownConstants[op1]);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.not(dest);
            }
        }

        private static void ShiftLeft(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // that would be weird, possible bug
            if (op1Const && !op2Const) {
                throw new Exception($"Shifting a constant by a variable");
            }

            asm.mov(dest, _registerMap[op1].GetRegister());

            if (op2Const) {
                asm.shl(dest, (byte)_knownConstants[op2]);
            } else {
                asm.shl(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));
            }
        }

        private static void ShiftRight(Assembler asm, IRInstruction instruction, bool arithmetic) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // that would be weird, possible bug
            if (op1Const && !op2Const) {
                throw new Exception($"Shifting a constant by a variable");
            }

            asm.mov(dest, _registerMap[op1].GetRegister());

            if (op2Const) {
                if (arithmetic) {
                    asm.sar(dest, (byte)_knownConstants[op2]);
                } else {
                    asm.shr(dest, (byte)_knownConstants[op2]);
                }

            } else {
                if (arithmetic) {
                    asm.sar(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));

                } else {
                    asm.shr(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));
                }
            }
        }

        private static void RotateLeft(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // that would be weird, possible bug
            if (op1Const && !op2Const) {
                throw new Exception($"Shifting a constant by a variable");
            }

            asm.mov(dest, _registerMap[op1].GetRegister());

            if (op2Const) {
                asm.rol(dest, (byte)_knownConstants[op2]);
            } else {
                asm.rol(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));
            }
        }

        private static void RotateRight(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // that would be weird, possible bug
            if (op1Const && !op2Const) {
                throw new Exception($"Shifting a constant by a variable");
            }

            asm.mov(dest, _registerMap[op1].GetRegister());

            if (op2Const) {
                asm.ror(dest, (byte)_knownConstants[op2]);
            } else {
                asm.ror(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));
            }
        }


        private static void SetEq(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                asm.cmp(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                asm.cmp(_registerMap[op1].GetRegister(), dest);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.cmp(dest, _registerMap[op2].GetRegister());
            }

            asm.sete(Reg64ToReg8(dest));
            asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetNotEq(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                asm.cmp(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                asm.cmp(_registerMap[op1].GetRegister(), dest);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.cmp(dest, _registerMap[op2].GetRegister());
            }

            asm.setne(Reg64ToReg8(dest));
            asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetLessThan(Assembler asm, IRInstruction instruction, bool signed) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                asm.cmp(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                asm.cmp(_registerMap[op1].GetRegister(), dest);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.cmp(dest, _registerMap[op2].GetRegister());
            }

            if (signed) {
                asm.setl(Reg64ToReg8(dest));
            } else {
                asm.setb(Reg64ToReg8(dest));
            }

            asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetGreaterThan(Assembler asm, IRInstruction instruction, bool signed) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                asm.cmp(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                asm.cmp(_registerMap[op1].GetRegister(), dest);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.cmp(dest, _registerMap[op2].GetRegister());
            }

            if (signed) {
                asm.setg(Reg64ToReg8(dest));
            } else {
                asm.seta(Reg64ToReg8(dest));
            }

            asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetLessThanEq(Assembler asm, IRInstruction instruction, bool signed) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                asm.cmp(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                asm.cmp(_registerMap[op1].GetRegister(), dest);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.cmp(dest, _registerMap[op2].GetRegister());
            }

            if (signed) {
                asm.setle(Reg64ToReg8(dest));
            } else {
                asm.setbe(Reg64ToReg8(dest));
            }

            asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetGreaterThanEq(Assembler asm, IRInstruction instruction, bool signed) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                asm.cmp(dest, _registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                asm.cmp(_registerMap[op1].GetRegister(), dest);

            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
                asm.cmp(dest, _registerMap[op2].GetRegister());
            }

            if (signed) {
                asm.setge(Reg64ToReg8(dest));
            } else {
                asm.setae(Reg64ToReg8(dest));
            }

            asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void Select(Assembler asm, IRInstruction instruction) {
            Label done = asm.CreateLabel();

            Location destination = _registerMap[instruction.Destination];
            Location condition = _registerMap[instruction.Operand1];

            Value op2 = instruction.Operand2;
            Value op3 = instruction.Operand3;

            bool op2Const = _knownConstants.ContainsKey(op2);
            bool op3Const = _knownConstants.ContainsKey(op3);

            AssemblerRegister64 cond = condition.GetRegister();
            AssemblerRegister64 dest = destination.GetRegister();

            // test the condition
            asm.test(cond, cond);

            // assume non zero
            if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
            } else {
                Location location2 = _registerMap[instruction.Operand2];
                asm.mov(dest, location2.GetRegister());
            }

            // branch
            asm.jnz(done);

            // if zero fallthough, overwrite

            if (op3Const) {
                asm.mov(dest, _knownConstants[op3]);
            } else {
                Location location3 = _registerMap[instruction.Operand3];
                asm.mov(dest, location3.GetRegister());
            }


            asm.Label(ref done);
        }

        public static void ZeroExtend(Assembler asm, IRInstruction instruction) {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = _registerMap[instruction.Destination].GetRegister();
            AssemblerRegister64 operandReg = _registerMap[instruction.Operand1].GetRegister();

            // using a constant would be weird, a possible IR bug
            if (_knownConstants.ContainsKey(instruction.Operand1)) {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            switch (destination.Type) {
                case Value.ValueType.I64:
                    ZeroExtendTo64(asm, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I32:
                    ZeroExtendTo32(asm, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I16:
                    ZeroExtendTo16(asm, destinationReg, operand.Type, operandReg);
                    break;

                default: throw new Exception($"Unsupported zero extend to {destination.Type}");
            }
        }

        public static void ZeroExtendTo64(Assembler asm, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand) {
            switch (operandType) {
                case Value.ValueType.I8:
                    asm.movzx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    asm.movzx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    asm.mov(Reg64ToReg32(dest), Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        public static void ZeroExtendTo32(Assembler asm, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand) {
            switch (operandType) {
                case Value.ValueType.I8:
                    asm.movzx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    asm.movzx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        public static void ZeroExtendTo16(Assembler asm, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand) {
            switch (operandType) {
                case Value.ValueType.I8:
                    asm.movzx(Reg64ToReg16(dest), Reg64ToReg8(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I16");
            }
        }

        public static void SignExtend(Assembler asm, IRInstruction instruction) {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = _registerMap[instruction.Destination].GetRegister();
            AssemblerRegister64 operandReg = _registerMap[instruction.Operand1].GetRegister();

            // using a constant would be weird, a possible IR bug
            if (_knownConstants.ContainsKey(instruction.Operand1)) {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            switch (destination.Type) {
                case Value.ValueType.I64:
                    SignExtendTo64(asm, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I32:
                    SignExtendTo32(asm, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I16:
                    SignExtendTo16(asm, destinationReg, operand.Type, operandReg);
                    break;

                default: throw new Exception($"Unsupported zero extend to {destination.Type}");
            }
        }

        public static void SignExtendTo64(Assembler asm, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand) {
            switch (operandType) {
                case Value.ValueType.I8:
                    asm.movsx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    asm.movsx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    asm.movsxd(dest, Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        public static void SignExtendTo32(Assembler asm, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand) {
            switch (operandType) {
                case Value.ValueType.I8:
                    asm.movsx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    asm.movsx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        public static void SignExtendTo16(Assembler asm, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand) {
            switch (operandType) {
                case Value.ValueType.I8:
                    asm.movsx(Reg64ToReg16(dest), Reg64ToReg8(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I16");
            }
        }

        public static void Truncate(Assembler asm, IRInstruction instruction) {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = _registerMap[instruction.Destination].GetRegister();
            AssemblerRegister64 operandReg = _registerMap[instruction.Operand1].GetRegister();

            // using a constant would be weird, a possible IR bug
            if (_knownConstants.ContainsKey(instruction.Operand1)) {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            uint mask;

            switch (destination.Type) {
                case Value.ValueType.I64: return;

                case Value.ValueType.I32: mask = uint.MaxValue; break;
                case Value.ValueType.I16: mask = ushort.MaxValue; break;
                case Value.ValueType.I8: mask = byte.MaxValue; break;

                default: throw new Exception($"Unsupported truncate to {destination.Type}");
            }

            asm.mov(destinationReg, operandReg);
            asm.and(destinationReg, (int)mask);
        }
    }
}
