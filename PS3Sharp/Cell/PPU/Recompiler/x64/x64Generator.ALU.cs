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
            _asm.mov(destReg, imm);
            _asm.add(destReg, srcReg);
        }

        private void Add(AssemblerRegister64 destReg, AssemblerRegister64 src1Reg, AssemblerRegister64 src2Reg)
        {
            _asm.mov(destReg, src1Reg);
            _asm.add(destReg, src2Reg);
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
                _asm.mov(dest, op1Value);
                reg2 = GetLocation(op2).GetRegister();
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                reg2 = GetLocation(op1).GetRegister();
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                reg2 = GetLocation(op2).GetRegister();
            }

            switch (instruction.Opcode)
            {
                case IRInstruction.Op.Add:
                    _asm.add(dest, reg2);
                    break;

                case IRInstruction.Op.And:
                    _asm.and(dest, reg2);
                    break;

                case IRInstruction.Op.Or:
                    _asm.or(dest, reg2);
                    break;

                case IRInstruction.Op.Xor:
                    _asm.xor(dest, reg2);
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
                _asm.mov(dest, op1Value);
                _asm.sub(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                // - B + A
                _asm.mov(dest, -(long)op2Value);
                _asm.add(dest, GetLocation(op1).GetRegister());
            }
            else
            {
                // A - B
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.sub(dest, GetLocation(op2).GetRegister());
            }
        }

        private void Not(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                _asm.mov(dest, ~op1Value);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.not(dest);
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
                _asm.mov(dest, op1Value);
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _asm.imul(dest, op2Reg);
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _asm.imul(dest, op1Reg);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _asm.mov(dest, op1Reg);
                _asm.imul(dest, op2Reg);
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
                _asm.mov(rax, op1Value);
                _asm.imul(GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(rax, op2Value);
                _asm.imul(GetLocation(op1).GetRegister());
            }
            else
            {
                _asm.mov(rax, GetLocation(op1).GetRegister());
                _asm.imul(GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            _asm.mov(dest, rdx);
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
                _asm.mov(rax, op1Value);
                _asm.mul(GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(rax, op2Value);
                _asm.mul(GetLocation(op1).GetRegister());
            }
            else
            {
                _asm.mov(rax, GetLocation(op1).GetRegister());
                _asm.mul(GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            _asm.mov(dest, rdx);
        }

        private void IMulLW(IRInstruction instruction)
        {
            // Signed 32 × 32 → Full 64-bit

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(_asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                _asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                _asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            // No shift needed
            _asm.imul(dest, temp);

            ReturnBorrowedRegister(temp);
        }

        private void IMulHW(IRInstruction instruction)
        {
            // Signed 32 × 32 → High 32-bit, Sign-Extended

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(_asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                _asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                _asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            _asm.imul(dest, temp);
            _asm.sar(dest, 32);   // arithmetic shift

            ReturnBorrowedRegister(temp);
        }

        private void MulHW(IRInstruction instruction)
        {
            // Unsigned 32 × 32 → High 32-bit, Zero-Extended

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(_asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                uint zeroExtended = (uint)op1Value;
                _asm.mov(dest, zeroExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _asm.mov(Reg64ToReg32(dest), Reg64ToReg32(op1Reg)); // automatic zero extend
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                _asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _asm.mov(Reg64ToReg32(temp), Reg64ToReg32(op2Reg)); // automatic zero extend
            }

            _asm.imul(dest, temp);
            _asm.shr(dest, 32);   // logical shift

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
            Label zero = _asm.CreateLabel();
            Label done = _asm.CreateLabel();

            bool op1Const = TryGetConst(op1, out ulong op1Value);
            bool op2Const = TryGetConst(op2, out ulong op2Value);

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                _asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Zero the numerator top 64-bit
            _asm.xor(rdx, rdx);

            // Mov numerator to rax
            if (op1Const)
            {
                _asm.mov(rax, op1Value);
            }
            else
            {
                _asm.mov(rax, GetLocation(op1).GetRegister());
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                _asm.mov(dest, op2Value);                    // use dest to hold denominator
                _asm.div(dest);                              // divide
                _asm.mov(dest, rax);                         // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister64 denominator = GetLocation(op2).GetRegister();

            // denominator == 0 ?
            _asm.test(denominator, denominator);
            _asm.jz(zero);

            _asm.div(denominator);       // divide
            _asm.mov(dest, rax);         // mov result and zero extend
            _asm.jmp(done);

            _asm.Label(ref zero);
            _asm.xor(dest, dest);

            _asm.Label(ref done);
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
            Label div = _asm.CreateLabel();
            Label zero = _asm.CreateLabel();
            Label done = _asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                _asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Move numerator to rax
            if (op1Const)
            {
                _asm.mov(rax, op1Value);
            }
            else
            {
                _asm.mov(rax, GetLocation(op1).GetRegister());
            }

            // Move denominator to dest
            if (op2Const)
            {
                _asm.mov(dest, op2Value);
            }
            else
            {
                _asm.mov(dest, GetLocation(op2).GetRegister());
            }

            AssemblerRegister64 denominator = dest;

            // Emit runtime check 
            // denominator == 0 ?
            _asm.test(denominator, denominator);
            _asm.jz(zero);

            // use rdx to hold 0x8000_0000_0000_0000 temporarily for the check
            // numerator == 0x8000_0000_0000_0000 ?
            _asm.mov(rdx, 0x8000_0000_0000_0000);
            _asm.cmp(rax, rdx);
            _asm.jnz(div);

            // denominator == -1 ?
            _asm.cmp(denominator, -1);
            _asm.jnz(div);
            _asm.jmp(zero);

            _asm.Label(ref div);

            _asm.cqo();              // Sign extend rax into rdx:rax for the division
            _asm.idiv(denominator);  // divide
            _asm.mov(dest, rax);     // mov result
            _asm.jmp(done);

            _asm.Label(ref zero);
            _asm.xor(dest, dest);

            _asm.Label(ref done);
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

            Label zero = _asm.CreateLabel();
            Label done = _asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                _asm.xor(dest_64, dest_64);
                return;
            }

            // Actual division required 

            // Zero the top 64-bit of numerator
            _asm.xor(rdx, rdx);

            // Move numerator to eax
            if (op1Const)
            {
                _asm.mov(eax, (uint)op1Value);
            }
            else
            {
                _asm.mov(eax, Reg64ToReg32(GetLocation(op1).GetRegister()));
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                _asm.mov(dest_32, (uint)op2Value);               // use dest to hold denominator
                _asm.div(dest_32);                               // divide
                _asm.mov(dest_32, eax);                          // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister32 denominator = Reg64ToReg32(GetLocation(op2).GetRegister());

            // denominator == 0 ?
            _asm.test(denominator, denominator);
            _asm.jz(zero);

            _asm.div(denominator);       // divide
            _asm.mov(dest_32, eax);      // mov result and zero extend
            _asm.jmp(done);

            _asm.Label(ref zero);
            _asm.xor(dest_64, dest_64);

            _asm.Label(ref done);
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
                _asm.xor(dest_64, dest_64);
                return;
            }

            Label div = _asm.CreateLabel();
            Label zero = _asm.CreateLabel();
            Label done = _asm.CreateLabel();

            // Move numerator to eax
            if (op1Const)
            {
                _asm.mov(eax, (uint)op2Value);
            }
            else
            {
                _asm.mov(eax, Reg64ToReg32(GetLocation(op1).GetRegister()));
            }

            // use dest to hold denominator 
            if (op2Const)
            {
                _asm.mov(dest_32, (uint)op2Value);
            }
            else
            {
                _asm.mov(dest_32, Reg64ToReg32(GetLocation(op2).GetRegister()));
            }

            AssemblerRegister32 denominator = dest_32;

            // Emit runtime check 
            // denominator == 0 ?
            _asm.test(denominator, denominator);
            _asm.jz(zero);

            // numerator == 0x8000_0000 ?
            _asm.cmp(eax, 0x8000_0000);
            _asm.jnz(div);

            // denominator == -1 ?
            _asm.cmp(denominator, -1);
            _asm.jnz(div);

            _asm.jmp(zero);

            _asm.Label(ref div);

            _asm.cdq();             // Sign extend eax into edx:eax for the division
            _asm.idiv(denominator); // divide
            _asm.mov(dest_32, eax); // mov result and zero extend
            _asm.jmp(done);

            _asm.Label(ref zero);
            _asm.xor(dest_64, dest_64);

            _asm.Label(ref done);
        }

        private void ShiftLeft(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                _asm.mov(dest, op1Value);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.shl(dest, (byte)op2Value);
            }
            else
            {
                _asm.shlx(dest, dest, GetLocation(op2).GetRegister()); // BMI2 extension
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
                _asm.mov(dest, op1Value);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                if (arithmetic)
                {
                    _asm.sar(dest, (byte)op2Value);
                }
                else
                {
                    _asm.shr(dest, (byte)op2Value);
                }
            }
            else
            {
                if (arithmetic)
                {
                    _asm.sarx(dest, dest, GetLocation(op2).GetRegister());  // BMI2 extension
                }
                else
                {
                    _asm.shrx(dest, dest, GetLocation(op2).GetRegister());  // BMI2 extension
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
                _asm.mov(dest, op1Value);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.rol(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                _asm.rol(dest, Reg64ToReg8(GetLocation(op2).GetRegister()));
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
                _asm.mov(dest, op1Value);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.ror(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                _asm.ror(dest, Reg64ToReg8(GetLocation(op2).GetRegister()));
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
                _asm.mov(dest, op1Value);
                _asm.cmp(dest, GetLocation(op2).GetRegister());

            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                _asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            _asm.sete(Reg64ToReg8(dest));
            _asm.movzx(dest, Reg64ToReg8(dest));
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
                _asm.mov(dest, op1Value);
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                _asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            _asm.setne(Reg64ToReg8(dest));
            _asm.movzx(dest, Reg64ToReg8(dest));
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
                _asm.mov(dest, op1Value);
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                _asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _asm.setl(Reg64ToReg8(dest));
            }
            else
            {
                _asm.setb(Reg64ToReg8(dest));
            }

            _asm.movzx(dest, Reg64ToReg8(dest));
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
                _asm.mov(dest, op1Value);
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                _asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _asm.setg(Reg64ToReg8(dest));
            }
            else
            {
                _asm.seta(Reg64ToReg8(dest));
            }

            _asm.movzx(dest, Reg64ToReg8(dest));
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
                _asm.mov(dest, op1Value);
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                _asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _asm.setle(Reg64ToReg8(dest));
            }
            else
            {
                _asm.setbe(Reg64ToReg8(dest));
            }

            _asm.movzx(dest, Reg64ToReg8(dest));
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
                _asm.mov(dest, op1Value);
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
                _asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _asm.mov(dest, GetLocation(op1).GetRegister());
                _asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _asm.setge(Reg64ToReg8(dest));
            }
            else
            {
                _asm.setae(Reg64ToReg8(dest));
            }

            _asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void Select(IRInstruction instruction)
        {
            Label done = _asm.CreateLabel();

            Location destination = GetLocation(instruction.Destination);
            Location condition = GetLocation(instruction.Operand1);

            Value op2 = instruction.Operand2;
            Value op3 = instruction.Operand3;

            AssemblerRegister64 cond = condition.GetRegister();
            AssemblerRegister64 dest = destination.GetRegister();

            // test the condition
            _asm.test(cond, cond);

            // assume non zero
            if (TryGetConst(op2, out ulong op2Value))
            {
                _asm.mov(dest, op2Value);
            }
            else
            {
                Location location2 = GetLocation(instruction.Operand2);
                _asm.mov(dest, location2.GetRegister());
            }

            // branch
            _asm.jnz(done);

            // if zero fallthough, overwrite

            if (TryGetConst(op3, out ulong op3Value))
            {
                _asm.mov(dest, op3Value);
            }
            else
            {
                Location location3 = GetLocation(instruction.Operand3);
                _asm.mov(dest, location3.GetRegister());
            }


            _asm.Label(ref done);
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
                    _asm.movzx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _asm.movzx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    _asm.mov(Reg64ToReg32(dest), Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        internal void ZeroExtendTo32(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _asm.movzx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _asm.movzx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        internal void ZeroExtendTo16(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _asm.movzx(Reg64ToReg16(dest), Reg64ToReg8(operand));
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
                    _asm.movsx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _asm.movsx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    _asm.movsxd(dest, Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        internal void SignExtendTo32(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _asm.movsx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _asm.movsx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        internal void SignExtendTo16(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _asm.movsx(Reg64ToReg16(dest), Reg64ToReg8(operand));
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

                _asm.mov(destinationReg, value & mask);

            }
            else
            {
                AssemblerRegister64 operandReg = GetLocation(operand).GetRegister();

                switch (destination.Type)
                {
                    case Value.ValueType.I64:
                        return;

                    case Value.ValueType.I32:
                        _asm.mov(Reg64ToReg32(destinationReg), Reg64ToReg32(operandReg));
                        break;

                    case Value.ValueType.I16:
                        _asm.movzx(destinationReg, Reg64ToReg16(operandReg));
                        break;

                    case Value.ValueType.I8:
                        _asm.movzx(destinationReg, Reg64ToReg8(operandReg));
                        break;

                    default: throw new Exception($"Unsupported zero extend to {destination.Type}");
                }
            }
        }
    }
}
