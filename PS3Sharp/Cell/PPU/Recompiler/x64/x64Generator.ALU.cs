using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal sealed partial class X64Generator
    {
        private void Add(AssemblerRegister64 destReg, AssemblerRegister64 srcReg, ulong imm)
        {
            Asm.mov(destReg, imm);
            Asm.add(destReg, srcReg);
        }

        private void Add(AssemblerRegister64 destReg, AssemblerRegister64 src1Reg, AssemblerRegister64 src2Reg)
        {
            Asm.mov(destReg, src1Reg);
            Asm.add(destReg, src2Reg);
        }

        private void CommutativeOp(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 reg2;

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                reg2 = GetLocation(op2).GetRegister();
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                reg2 = GetLocation(op1).GetRegister();
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                reg2 = GetLocation(op2).GetRegister();
            }

            switch (instruction.Opcode)
            {
                case IRInstruction.Op.Add:
                    Asm.add(dest, reg2);
                    break;

                case IRInstruction.Op.And:
                    Asm.and(dest, reg2);
                    break;

                case IRInstruction.Op.Or:
                    Asm.or(dest, reg2);
                    break;

                case IRInstruction.Op.Xor:
                    Asm.xor(dest, reg2);
                    break;

                default:
                    throw new NotImplementedException($"Unknown CommutativeOp = {instruction.Opcode}");
            }
        }

        private void Sub(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                // A - B
                Asm.mov(dest, op1Value);
                Asm.sub(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                // - B + A
                Asm.mov(dest, -(long)op2Value);
                Asm.add(dest, GetLocation(op1).GetRegister());
            }
            else
            {
                // A - B
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.sub(dest, GetLocation(op2).GetRegister());
            }
        }

        private void Not(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, ~op1Value);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.not(dest);
            }
        }

        private void IMulLD(IRInstruction instruction)
        {
            // Signed 64 × 64 → Low 64-bit

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                Asm.imul(dest, op2Reg);
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                Asm.imul(dest, op1Reg);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                Asm.mov(dest, op1Reg);
                Asm.imul(dest, op2Reg);
            }
        }

        private void IMulHD(IRInstruction instruction)
        {
            // Signed 64 × 64 → High 64-bit

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // Use imul(reg) form

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(rax, op1Value);
                Asm.imul(GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(rax, op2Value);
                Asm.imul(GetLocation(op1).GetRegister());
            }
            else
            {
                Asm.mov(rax, GetLocation(op1).GetRegister());
                Asm.imul(GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            Asm.mov(dest, rdx);
        }

        private void MulHD(IRInstruction instruction)
        {
            // Unsigned 64 × 64 → High 64-bit

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // Use mul(reg) form

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(rax, op1Value);
                Asm.mul(GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(rax, op2Value);
                Asm.mul(GetLocation(op1).GetRegister());
            }
            else
            {
                Asm.mov(rax, GetLocation(op1).GetRegister());
                Asm.mul(GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            Asm.mov(dest, rdx);
        }

        private void IMulLW(IRInstruction instruction)
        {
            // Signed 32 × 32 → Full 64-bit

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(Asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                Asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                Asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                Asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            // No shift needed
            Asm.imul(dest, temp);

            ReturnBorrowedRegister(temp);
        }

        private void IMulHW(IRInstruction instruction)
        {
            // Signed 32 × 32 → High 32-bit, Sign-Extended

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(Asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                Asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                Asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                Asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            Asm.imul(dest, temp);
            Asm.sar(dest, 32);   // arithmetic shift

            ReturnBorrowedRegister(temp);
        }

        private void MulHW(IRInstruction instruction)
        {
            // Unsigned 32 × 32 → High 32-bit, Zero-Extended

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(Asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                uint zeroExtended = (uint)op1Value;
                Asm.mov(dest, zeroExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                Asm.mov(Reg64ToReg32(dest), Reg64ToReg32(op1Reg)); // automatic zero extend
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                Asm.mov(Reg64ToReg32(temp), Reg64ToReg32(op2Reg)); // automatic zero extend
            }

            Asm.imul(dest, temp);
            Asm.shr(dest, 32);   // logical shift

            ReturnBorrowedRegister(temp);
        }

        private void CheckedDivD(IRInstruction instruction)
        {
            // Unsigned 64/64, result -> 0 If denominator is zero

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            Label zero = Asm.CreateLabel();
            Label done = Asm.CreateLabel();

            bool op1Const = TryGetConst(op1, out ulong op1Value);
            bool op2Const = TryGetConst(op2, out ulong op2Value);

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                Asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Zero the numerator top 64-bit
            Asm.xor(rdx, rdx);

            // Mov numerator to rax
            if (op1Const)
            {
                Asm.mov(rax, op1Value);
            }
            else
            {
                Asm.mov(rax, GetLocation(op1).GetRegister());
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                Asm.mov(dest, op2Value);                    // use dest to hold denominator
                Asm.div(dest);                              // divide
                Asm.mov(dest, rax);                         // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister64 denominator = GetLocation(op2).GetRegister();

            // denominator == 0 ?
            Asm.test(denominator, denominator);
            Asm.jz(zero);

            Asm.div(denominator);       // divide
            Asm.mov(dest, rax);         // mov result and zero extend
            Asm.jmp(done);

            Asm.Label(ref zero);
            Asm.xor(dest, dest);

            Asm.Label(ref done);
        }

        private void CheckedIDivD(IRInstruction instruction)
        {
            // Signed 64/64, result -> 0 If denominator is zero || (numerator is 0x8000_0000_0000_0000 && denominator is -1)

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            // TODO: Find a better way to do this

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = TryGetConst(op1, out ulong op1Value);
            bool op2Const = TryGetConst(op2, out ulong op2Value);

            AssemblerRegister64 dest = destination.GetRegister();
            Label div = Asm.CreateLabel();
            Label zero = Asm.CreateLabel();
            Label done = Asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                Asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Move numerator to rax
            if (op1Const)
            {
                Asm.mov(rax, op1Value);
            }
            else
            {
                Asm.mov(rax, GetLocation(op1).GetRegister());
            }

            // Move denominator to dest
            if (op2Const)
            {
                Asm.mov(dest, op2Value);
            }
            else
            {
                Asm.mov(dest, GetLocation(op2).GetRegister());
            }

            AssemblerRegister64 denominator = dest;

            // Emit runtime check 
            // denominator == 0 ?
            Asm.test(denominator, denominator);
            Asm.jz(zero);

            // use rdx to hold 0x8000_0000_0000_0000 temporarily for the check
            // numerator == 0x8000_0000_0000_0000 ?
            Asm.mov(rdx, 0x8000_0000_0000_0000);
            Asm.cmp(rax, rdx);
            Asm.jnz(div);

            // denominator == -1 ?
            Asm.cmp(denominator, -1);
            Asm.jnz(div);
            Asm.jmp(zero);

            Asm.Label(ref div);

            Asm.cqo();              // Sign extend rax into rdx:rax for the division
            Asm.idiv(denominator);  // divide
            Asm.mov(dest, rax);     // mov result
            Asm.jmp(done);

            Asm.Label(ref zero);
            Asm.xor(dest, dest);

            Asm.Label(ref done);
        }

        private void CheckedDivW(IRInstruction instruction)
        {
            // Unsigned 32/32, result -> 0 If denominator is zero

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = TryGetConst(op1, out ulong op1Value);
            bool op2Const = TryGetConst(op2, out ulong op2Value);

            AssemblerRegister64 dest_64 = destination.GetRegister();
            AssemblerRegister32 dest_32 = Reg64ToReg32(dest_64);

            Label zero = Asm.CreateLabel();
            Label done = Asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                Asm.xor(dest_64, dest_64);
                return;
            }

            // Actual division required 

            // Zero the top 64-bit of numerator
            Asm.xor(rdx, rdx);

            // Move numerator to eax
            if (op1Const)
            {
                Asm.mov(eax, (uint)op1Value);
            }
            else
            {
                Asm.mov(eax, Reg64ToReg32(GetLocation(op1).GetRegister()));
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                Asm.mov(dest_32, (uint)op2Value);               // use dest to hold denominator
                Asm.div(dest_32);                               // divide
                Asm.mov(dest_32, eax);                          // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister32 denominator = Reg64ToReg32(GetLocation(op2).GetRegister());

            // denominator == 0 ?
            Asm.test(denominator, denominator);
            Asm.jz(zero);

            Asm.div(denominator);       // divide
            Asm.mov(dest_32, eax);      // mov result and zero extend
            Asm.jmp(done);

            Asm.Label(ref zero);
            Asm.xor(dest_64, dest_64);

            Asm.Label(ref done);
        }

        private void CheckedIDivW(IRInstruction instruction)
        {
            // Signed 32/32, result -> 0 If denominator is zero || (numerator is 0x8000_0000 && denominator is -1)

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = TryGetConst(op1, out ulong op1Value);
            bool op2Const = TryGetConst(op2, out ulong op2Value);

            AssemblerRegister64 dest_64 = destination.GetRegister();
            AssemblerRegister32 dest_32 = Reg64ToReg32(dest_64);

            // Special case that we can check at compile time
            if (op2Const && ((uint)op2Value) == 0)
            {
                Asm.xor(dest_64, dest_64);
                return;
            }

            Label div = Asm.CreateLabel();
            Label zero = Asm.CreateLabel();
            Label done = Asm.CreateLabel();

            // Move numerator to eax
            if (op1Const)
            {
                Asm.mov(eax, (uint)op2Value);
            }
            else
            {
                Asm.mov(eax, Reg64ToReg32(GetLocation(op1).GetRegister()));
            }

            // use dest to hold denominator 
            if (op2Const)
            {
                Asm.mov(dest_32, (uint)op2Value);
            }
            else
            {
                Asm.mov(dest_32, Reg64ToReg32(GetLocation(op2).GetRegister()));
            }

            AssemblerRegister32 denominator = dest_32;

            // Emit runtime check 
            // denominator == 0 ?
            Asm.test(denominator, denominator);
            Asm.jz(zero);

            // numerator == 0x8000_0000 ?
            Asm.cmp(eax, 0x8000_0000);
            Asm.jnz(div);

            // denominator == -1 ?
            Asm.cmp(denominator, -1);
            Asm.jnz(div);

            Asm.jmp(zero);

            Asm.Label(ref div);

            Asm.cdq();             // Sign extend eax into edx:eax for the division
            Asm.idiv(denominator); // divide
            Asm.mov(dest_32, eax); // mov result and zero extend
            Asm.jmp(done);

            Asm.Label(ref zero);
            Asm.xor(dest_64, dest_64);

            Asm.Label(ref done);
        }

        private void ShiftLeft(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.shl(dest, (byte)op2Value);
            }
            else
            {
                Asm.shlx(dest, dest, GetLocation(op2).GetRegister()); // BMI2 extension
            }
        }

        private void ShiftRight(IRInstruction instruction, bool arithmetic)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                if (arithmetic)
                {
                    Asm.sar(dest, (byte)op2Value);
                }
                else
                {
                    Asm.shr(dest, (byte)op2Value);
                }
            }
            else
            {
                if (arithmetic)
                {
                    Asm.sarx(dest, dest, GetLocation(op2).GetRegister());  // BMI2 extension
                }
                else
                {
                    Asm.shrx(dest, dest, GetLocation(op2).GetRegister());  // BMI2 extension
                }
            }
        }

        private void RotateLeft(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.rol(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                Asm.rol(dest, Reg64ToReg8(GetLocation(op2).GetRegister()));
                throw new Exception($"todo: rol");
            }
        }

        private void RotateRight(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.ror(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                Asm.ror(dest, Reg64ToReg8(GetLocation(op2).GetRegister()));
                throw new Exception($"todo: ror");
            }
        }

        private void SetEq(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                Asm.cmp(dest, GetLocation(op2).GetRegister());

            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            Asm.sete(Reg64ToReg8(dest));
            Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void SetNotEq(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            Asm.setne(Reg64ToReg8(dest));
            Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void SetLessThan(IRInstruction instruction, bool signed)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                Asm.setl(Reg64ToReg8(dest));
            }
            else
            {
                Asm.setb(Reg64ToReg8(dest));
            }

            Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void SetGreaterThan(IRInstruction instruction, bool signed)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                Asm.setg(Reg64ToReg8(dest));
            }
            else
            {
                Asm.seta(Reg64ToReg8(dest));
            }

            Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void SetLessThanEq(IRInstruction instruction, bool signed)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                Asm.setle(Reg64ToReg8(dest));
            }
            else
            {
                Asm.setbe(Reg64ToReg8(dest));
            }

            Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void SetGreaterThanEq(IRInstruction instruction, bool signed)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (TryGetConst(op1, out ulong op1Value))
            {
                Asm.mov(dest, op1Value);
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
                Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                Asm.mov(dest, GetLocation(op1).GetRegister());
                Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                Asm.setge(Reg64ToReg8(dest));
            }
            else
            {
                Asm.setae(Reg64ToReg8(dest));
            }

            Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void Select(IRInstruction instruction)
        {
            Label done = Asm.CreateLabel();

            Location destination = GetLocation(instruction.Destination);
            Location condition = GetLocation(instruction.Operand1);

            Value op2 = instruction.Operand2;
            Value op3 = instruction.Operand3;

            AssemblerRegister64 cond = condition.GetRegister();
            AssemblerRegister64 dest = destination.GetRegister();

            // test the condition
            Asm.test(cond, cond);

            // assume non zero
            if (TryGetConst(op2, out ulong op2Value))
            {
                Asm.mov(dest, op2Value);
            }
            else
            {
                Location location2 = GetLocation(instruction.Operand2);
                Asm.mov(dest, location2.GetRegister());
            }

            // branch
            Asm.jnz(done);

            // if zero fallthough, overwrite

            if (TryGetConst(op3, out ulong op3Value))
            {
                Asm.mov(dest, op3Value);
            }
            else
            {
                Location location3 = GetLocation(instruction.Operand3);
                Asm.mov(dest, location3.GetRegister());
            }


            Asm.Label(ref done);
        }

        internal void ZeroExtend(IRInstruction instruction)
        {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister64 operandReg = GetLocation(instruction.Operand1).GetRegister();

            // using a constant would be weird, a possible IR bug
            if (IsConst(instruction.Operand1))
            {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            switch (destination.Type)
            {
                case Value.ValueType.I64:
                    ZeroExtendTo64(destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I32:
                    ZeroExtendTo32(destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I16:
                    ZeroExtendTo16(destinationReg, operand.Type, operandReg);
                    break;

                default: throw new Exception($"Unsupported zero extend to {destination.Type}");
            }
        }

        internal void ZeroExtendTo64(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    Asm.movzx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    Asm.movzx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    Asm.mov(Reg64ToReg32(dest), Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        internal void ZeroExtendTo32(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    Asm.movzx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    Asm.movzx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        internal void ZeroExtendTo16(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    Asm.movzx(Reg64ToReg16(dest), Reg64ToReg8(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I16");
            }
        }

        internal void SignExtend(IRInstruction instruction)
        {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister64 operandReg = GetLocation(instruction.Operand1).GetRegister();

            // using a constant would be weird, a possible IR bug
            if (IsConst(instruction.Operand1))
            {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            switch (destination.Type)
            {
                case Value.ValueType.I64:
                    SignExtendTo64(destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I32:
                    SignExtendTo32(destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I16:
                    SignExtendTo16(destinationReg, operand.Type, operandReg);
                    break;

                default: throw new Exception($"Unsupported zero extend to {destination.Type}");
            }
        }

        internal void SignExtendTo64(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    Asm.movsx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    Asm.movsx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    Asm.movsxd(dest, Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        internal void SignExtendTo32(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    Asm.movsx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    Asm.movsx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        internal void SignExtendTo16(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    Asm.movsx(Reg64ToReg16(dest), Reg64ToReg8(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I16");
            }
        }

        internal void Truncate(IRInstruction instruction)
        {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = GetLocation(instruction.Destination).GetRegister();

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                uint mask;

                switch (destination.Type)
                {
                    case Value.ValueType.I64:
                        return;

                    case Value.ValueType.I32:
                        mask = uint.MaxValue;
                        break;

                    case Value.ValueType.I16:
                        mask = ushort.MaxValue;
                        break;

                    case Value.ValueType.I8:
                        mask = byte.MaxValue;
                        break;

                    default: throw new Exception($"Unsupported truncate to {destination.Type}");
                }

                Asm.mov(destinationReg, value & mask);

            }
            else
            {
                AssemblerRegister64 operandReg = GetLocation(operand).GetRegister();

                switch (destination.Type)
                {
                    case Value.ValueType.I64:
                        return;

                    case Value.ValueType.I32:
                        Asm.mov(Reg64ToReg32(destinationReg), Reg64ToReg32(operandReg));
                        break;

                    case Value.ValueType.I16:
                        Asm.movzx(destinationReg, Reg64ToReg16(operandReg));
                        break;

                    case Value.ValueType.I8:
                        Asm.movzx(destinationReg, Reg64ToReg8(operandReg));
                        break;

                    default: throw new Exception($"Unsupported zero extend to {destination.Type}");
                }
            }
        }
    }
}
