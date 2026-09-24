using Iced.Intel;
using System.Runtime.Intrinsics.Arm;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;
using static PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator;
using static Iced.Intel.AssemblerRegisters;

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

        private static void IMulLD(Assembler asm, IRInstruction instruction) {
            // Signed 64 × 64 → Low 64-bit

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
                AssemblerRegister64 op2Reg = _registerMap[op2].GetRegister();
                asm.imul(dest, op2Reg);

            } else if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
                AssemblerRegister64 op1Reg = _registerMap[op1].GetRegister();
                asm.imul(dest, op1Reg);

            } else {
                AssemblerRegister64 op1Reg = _registerMap[op1].GetRegister();
                AssemblerRegister64 op2Reg = _registerMap[op2].GetRegister();
                asm.mov(dest, op1Reg);
                asm.imul(dest, op2Reg);
            }
        }

        private static void IMulHD(Assembler asm, IRInstruction instruction) {
            // Signed 64 × 64 → High 64-bit

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // Use imul(reg) form

            if (op1Const) {
                asm.mov(rax, _knownConstants[op1]);
                asm.imul(_registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(rax, _knownConstants[op2]);
                asm.imul(_registerMap[op1].GetRegister());

            } else {
                asm.mov(rax, _registerMap[op1].GetRegister());
                asm.imul(_registerMap[op2].GetRegister());
            }

            // rdx contains high 64-bits
            asm.mov(dest, rdx);
        }

        private static void MulHD(Assembler asm, IRInstruction instruction) {
            // Unsigned 64 × 64 → High 64-bit

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            // Use mul(reg) form

            if (op1Const) {
                asm.mov(rax, _knownConstants[op1]);
                asm.mul(_registerMap[op2].GetRegister());

            } else if (op2Const) {
                asm.mov(rax, _knownConstants[op2]);
                asm.mul(_registerMap[op1].GetRegister());

            } else {
                asm.mov(rax, _registerMap[op1].GetRegister());
                asm.mul(_registerMap[op2].GetRegister());
            }

            // rdx contains high 64-bits
            asm.mov(dest, rdx);
        }

        private static void IMulLW(Assembler asm, IRInstruction instruction) {
            // Signed 32 × 32 → Full 64-bit

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(asm);

            if (op1Const) {
                long signExtended = (long)(int)_knownConstants[op1];
                asm.mov(dest, signExtended);

            } else {
                AssemblerRegister64 op1Reg = _registerMap[op1].GetRegister();
                asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (op2Const) {
                long signExtended = (long)(int)_knownConstants[op2];
                asm.mov(temp, signExtended);

            } else {
                AssemblerRegister64 op2Reg = _registerMap[op2].GetRegister();
                asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            // No shift needed
            asm.imul(dest, temp);

            ReturnBorrowedRegister(asm, temp);
        }

        private static void IMulHW(Assembler asm, IRInstruction instruction) {
            // Signed 32 × 32 → High 32-bit, Sign-Extended

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(asm);

            if (op1Const) {
                long signExtended = (long)(int)_knownConstants[op1];
                asm.mov(dest, signExtended);

            } else {
                AssemblerRegister64 op1Reg = _registerMap[op1].GetRegister();
                asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (op2Const) {
                long signExtended = (long)(int)_knownConstants[op2];
                asm.mov(temp, signExtended);

            } else {
                AssemblerRegister64 op2Reg = _registerMap[op2].GetRegister();
                asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            asm.imul(dest, temp);
            asm.sar(dest, 32);   // arithmetic shift

            ReturnBorrowedRegister(asm, temp);
        }

        private static void MulHW(Assembler asm, IRInstruction instruction) {
            // Unsigned 32 × 32 → High 32-bit, Zero-Extended

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(asm);

            if (op1Const) {
                uint zeroExtended = (uint)_knownConstants[op1];
                asm.mov(dest, zeroExtended);

            } else {
                AssemblerRegister64 op1Reg = _registerMap[op1].GetRegister();
                asm.mov(Reg64ToReg32(dest), Reg64ToReg32(op1Reg)); // automatic zero extend
            }

            if (op2Const) {
                long signExtended = (long)(int)_knownConstants[op2];
                asm.mov(temp, signExtended);

            } else {
                AssemblerRegister64 op2Reg = _registerMap[op2].GetRegister();
                asm.mov(Reg64ToReg32(temp), Reg64ToReg32(op2Reg)); // automatic zero extend
            }

            asm.imul(dest, temp);
            asm.shr(dest, 32);   // logical shift

            ReturnBorrowedRegister(asm, temp);
        }

        private static void CheckedDivD(Assembler asm, IRInstruction instruction) {
            // Unsigned 64/64, result -> 0 If denominator is zero

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            Label zero = asm.CreateLabel();
            Label done = asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && _knownConstants[op2] == 0) {
                asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Zero the numerator top 64-bit
            asm.xor(rdx, rdx);

            // Mov numerator to rax
            if (op1Const) {
                asm.mov(rax, _knownConstants[op1]);
            } else {
                asm.mov(rax, _registerMap[op1].GetRegister());
            }

            // if op2 is constant no runtime check is needed
            if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);        // use dest to hold denominator
                asm.div(dest);                              // divide
                asm.mov(dest, rax);                         // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister64 denominator = _registerMap[op2].GetRegister();

            // denominator == 0 ?
            asm.test(denominator, denominator);
            asm.jz(zero);

            asm.div(denominator);       // divide
            asm.mov(dest, rax);         // mov result and zero extend
            asm.jmp(done);

            asm.Label(ref zero);
            asm.xor(dest, dest);

            asm.Label(ref done);
        }

        private static void CheckedIDivD(Assembler asm, IRInstruction instruction) {
            // Signed 64/64, result -> 0 If denominator is zero || (numerator is 0x8000_0000_0000_0000 && denominator is -1)

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            // TODO: Find a better way to do this

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();
            Label div = asm.CreateLabel();
            Label zero = asm.CreateLabel();
            Label done = asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && _knownConstants[op2] == 0) {
                asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Move numerator to rax
            if (op1Const) {
                asm.mov(rax, _knownConstants[op1]);
            } else {
                asm.mov(rax, _registerMap[op1].GetRegister());
            }

            // Sign extend rax into rdx:rax
            asm.cqo();

            // Move denominator to dest
            if (op2Const) {
                asm.mov(dest, _knownConstants[op2]);
            } else {
                asm.mov(dest, _registerMap[op2].GetRegister());
            }

            // Borrow a register to hold 0x8000_0000_0000_0000
            AssemblerRegister64 temp = BorrowRegister(asm);
            AssemblerRegister64 denominator = dest;

            // Emit runtime check 
            // denominator == 0 ?
            asm.test(denominator, denominator); 
            asm.jz(zero);

            // numerator == 0x8000_0000_0000_0000 ?
            asm.mov(temp, 0x8000_0000_0000_0000);
            asm.cmp(rax, temp);
            asm.jnz(div);

            // denominator == -1 ?
            asm.cmp(denominator, -1);
            asm.jnz(div);
            asm.jmp(zero);

            asm.Label(ref div);
            asm.idiv(denominator);  // divide
            asm.mov(dest, rax);     // mov result
            asm.jmp(done);

            asm.Label(ref zero);
            asm.xor(dest, dest);

            asm.Label(ref done);
            ReturnBorrowedRegister(asm, temp);
        }

        private static void CheckedDivW(Assembler asm, IRInstruction instruction) {
            // Unsigned 32/32, result -> 0 If denominator is zero

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest_64 = destination.GetRegister();
            AssemblerRegister32 dest_32 = Reg64ToReg32(dest_64);

            Label zero = asm.CreateLabel();
            Label done = asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && _knownConstants[op2] == 0) {
                asm.xor(dest_64, dest_64);
                return;
            }

            // Actual division required 

            // Zero the top 64-bit of numerator
            asm.xor(rdx, rdx);

            // Move numerator to eax
            if (op1Const) {
                asm.mov(eax, (uint)_knownConstants[op1]);
            } else {
                asm.mov(eax, Reg64ToReg32(_registerMap[op1].GetRegister()));
            }

            // if op2 is constant no runtime check is needed
            if (op2Const) {
                asm.mov(dest_32, (uint)_knownConstants[op2]);   // use dest to hold denominator
                asm.div(dest_32);                               // divide
                asm.mov(dest_32, eax);                          // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister32 denominator = Reg64ToReg32(_registerMap[op2].GetRegister());

            // denominator == 0 ?
            asm.test(denominator, denominator);
            asm.jz(zero);

            asm.div(denominator);       // divide
            asm.mov(dest_32, eax);      // mov result and zero extend
            asm.jmp(done);

            asm.Label(ref zero);
            asm.xor(dest_64, dest_64);

            asm.Label(ref done);
        }

        private static void CheckedIDivW(Assembler asm, IRInstruction instruction) {
            // Signed 32/32, result -> 0 If denominator is zero || (numerator is 0x8000_0000 && denominator is -1)

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest_64 = destination.GetRegister();
            AssemblerRegister32 dest_32 = Reg64ToReg32(dest_64);

            // Special case that we can check at compile time
            if (op2Const && ((uint)_knownConstants[op2]) == 0) {
                asm.xor(dest_64, dest_64);
                return;
            }

            Label div = asm.CreateLabel();
            Label zero = asm.CreateLabel();
            Label done = asm.CreateLabel();

            // Move numerator to eax
            if (op1Const) {
                asm.mov(eax, (uint)_knownConstants[op1]);
            } else {
                asm.mov(eax, Reg64ToReg32(_registerMap[op1].GetRegister()));
            }

            // Sign extend eax into edx:eax
            asm.cdq();

            // use dest to hold denominator 
            if (op2Const) {
                asm.mov(dest_32, (uint)_knownConstants[op2]);
            } else {
                asm.mov(dest_32, Reg64ToReg32(_registerMap[op2].GetRegister()));
            }

            AssemblerRegister32 denominator = dest_32;

            // Emit runtime check 
            // denominator == 0 ?
            asm.test(denominator, denominator);
            asm.jz(zero);

            // numerator == 0x8000_0000 ?
            asm.cmp(eax, 0x8000_0000);
            asm.jnz(div);

            // denominator == -1 ?
            asm.cmp(denominator, -1);
            asm.jnz(div);

            asm.jmp(zero);

            asm.Label(ref div);

            asm.idiv(denominator); // divide
            asm.mov(dest_32, eax); // mov result and zero extend
            asm.jmp(done);

            asm.Label(ref zero);
            asm.xor(dest_64, dest_64);

            asm.Label(ref done);
        }

        private static void ShiftLeft(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
            }

            if (op2Const) {
                asm.shl(dest, (byte)_knownConstants[op2]);
            } else {
                asm.shlx(dest, dest, _registerMap[op2].GetRegister()); // BMI2 extension
            }
        }

        private static void ShiftRight(Assembler asm, IRInstruction instruction, bool arithmetic) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
            }

            if (op2Const) {
                if (arithmetic) {
                    asm.sar(dest, (byte)_knownConstants[op2]);
                } else {
                    asm.shr(dest, (byte)_knownConstants[op2]);
                }

            } else {
                if (arithmetic) {
                    asm.sarx(dest, dest, _registerMap[op2].GetRegister());  // BMI2 extension

                } else {
                    asm.shrx(dest, dest, _registerMap[op2].GetRegister());  // BMI2 extension
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

            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
            }

            if (op2Const) {
                asm.rol(dest, (byte)_knownConstants[op2]);
            } else {
                // we need to use CL
                asm.rol(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));
                throw new Exception($"todo: rol");
            }
        }

        private static void RotateRight(Assembler asm, IRInstruction instruction) {
            Location destination = _registerMap[instruction.Destination];
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = _knownConstants.ContainsKey(op1);
            bool op2Const = _knownConstants.ContainsKey(op2);

            AssemblerRegister64 dest = destination.GetRegister();

            if (op1Const) {
                asm.mov(dest, _knownConstants[op1]);
            } else {
                asm.mov(dest, _registerMap[op1].GetRegister());
            }

            if (op2Const) {
                asm.ror(dest, (byte)_knownConstants[op2]);
            } else {
                // we need to use CL
                asm.ror(dest, Reg64ToReg8(_registerMap[op2].GetRegister()));
                throw new Exception($"todo: ror");
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

            uint mask;

            switch (destination.Type) {
                case Value.ValueType.I64: return;

                case Value.ValueType.I32: mask = uint.MaxValue; break;
                case Value.ValueType.I16: mask = ushort.MaxValue; break;
                case Value.ValueType.I8: mask = byte.MaxValue; break;

                default: throw new Exception($"Unsupported truncate to {destination.Type}");
            }

            if (_knownConstants.TryGetValue(instruction.Operand1, out ulong value)) {
                asm.mov(destinationReg, value & mask);

            } else {
                AssemblerRegister64 operandReg = _registerMap[instruction.Operand1].GetRegister();
                asm.mov(destinationReg, operandReg);
                asm.and(destinationReg, (int)mask);
            }
        }
    }
}
