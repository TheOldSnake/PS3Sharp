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
            _currentState.Asm.mov(destReg, imm);
            _currentState.Asm.add(destReg, srcReg);
        }

        private void Add(AssemblerRegister64 destReg, AssemblerRegister64 src1Reg, AssemblerRegister64 src2Reg)
        {
            _currentState.Asm.mov(destReg, src1Reg);
            _currentState.Asm.add(destReg, src2Reg);
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
                _currentState.Asm.mov(dest, op1Value);
                reg2 = GetLocation(op2).GetRegister();
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                reg2 = GetLocation(op1).GetRegister();
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                reg2 = GetLocation(op2).GetRegister();
            }

            switch (instruction.Opcode)
            {
                case IRInstruction.Op.Add:
                    _currentState.Asm.add(dest, reg2);
                    break;

                case IRInstruction.Op.And:
                    _currentState.Asm.and(dest, reg2);
                    break;

                case IRInstruction.Op.Or:
                    _currentState.Asm.or(dest, reg2);
                    break;

                case IRInstruction.Op.Xor:
                    _currentState.Asm.xor(dest, reg2);
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.sub(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                // - B + A
                _currentState.Asm.mov(dest, -(long)op2Value);
                _currentState.Asm.add(dest, GetLocation(op1).GetRegister());
            }
            else
            {
                // A - B
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.sub(dest, GetLocation(op2).GetRegister());
            }
        }

        private void Not(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                _currentState.Asm.mov(dest, ~op1Value);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.not(dest);
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
                _currentState.Asm.mov(dest, op1Value);
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _currentState.Asm.imul(dest, op2Reg);
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _currentState.Asm.imul(dest, op1Reg);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _currentState.Asm.mov(dest, op1Reg);
                _currentState.Asm.imul(dest, op2Reg);
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
                _currentState.Asm.mov(rax, op1Value);
                _currentState.Asm.imul(GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(rax, op2Value);
                _currentState.Asm.imul(GetLocation(op1).GetRegister());
            }
            else
            {
                _currentState.Asm.mov(rax, GetLocation(op1).GetRegister());
                _currentState.Asm.imul(GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            _currentState.Asm.mov(dest, rdx);
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
                _currentState.Asm.mov(rax, op1Value);
                _currentState.Asm.mul(GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(rax, op2Value);
                _currentState.Asm.mul(GetLocation(op1).GetRegister());
            }
            else
            {
                _currentState.Asm.mov(rax, GetLocation(op1).GetRegister());
                _currentState.Asm.mul(GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            _currentState.Asm.mov(dest, rdx);
        }

        private void IMulLW(IRInstruction instruction)
        {
            // Signed 32 × 32 → Full 64-bit

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(_currentState.Asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                _currentState.Asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _currentState.Asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                _currentState.Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _currentState.Asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            // No shift needed
            _currentState.Asm.imul(dest, temp);

            ReturnBorrowedRegister(temp);
        }

        private void IMulHW(IRInstruction instruction)
        {
            // Signed 32 × 32 → High 32-bit, Sign-Extended

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(_currentState.Asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                _currentState.Asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _currentState.Asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                _currentState.Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _currentState.Asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            _currentState.Asm.imul(dest, temp);
            _currentState.Asm.sar(dest, 32);   // arithmetic shift

            ReturnBorrowedRegister(temp);
        }

        private void MulHW(IRInstruction instruction)
        {
            // Unsigned 32 × 32 → High 32-bit, Zero-Extended

            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = BorrowRegister(_currentState.Asm);

            if (TryGetConst(op1, out ulong op1Value))
            {
                uint zeroExtended = (uint)op1Value;
                _currentState.Asm.mov(dest, zeroExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = GetLocation(op1).GetRegister();
                _currentState.Asm.mov(Reg64ToReg32(dest), Reg64ToReg32(op1Reg)); // automatic zero extend
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                _currentState.Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = GetLocation(op2).GetRegister();
                _currentState.Asm.mov(Reg64ToReg32(temp), Reg64ToReg32(op2Reg)); // automatic zero extend
            }

            _currentState.Asm.imul(dest, temp);
            _currentState.Asm.shr(dest, 32);   // logical shift

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
            Label zero = _currentState.Asm.CreateLabel();
            Label done = _currentState.Asm.CreateLabel();

            bool op1Const = TryGetConst(op1, out ulong op1Value);
            bool op2Const = TryGetConst(op2, out ulong op2Value);

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                _currentState.Asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Zero the numerator top 64-bit
            _currentState.Asm.xor(rdx, rdx);

            // Mov numerator to rax
            if (op1Const)
            {
                _currentState.Asm.mov(rax, op1Value);
            }
            else
            {
                _currentState.Asm.mov(rax, GetLocation(op1).GetRegister());
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                _currentState.Asm.mov(dest, op2Value);                    // use dest to hold denominator
                _currentState.Asm.div(dest);                              // divide
                _currentState.Asm.mov(dest, rax);                         // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister64 denominator = GetLocation(op2).GetRegister();

            // denominator == 0 ?
            _currentState.Asm.test(denominator, denominator);
            _currentState.Asm.jz(zero);

            _currentState.Asm.div(denominator);       // divide
            _currentState.Asm.mov(dest, rax);         // mov result and zero extend
            _currentState.Asm.jmp(done);

            _currentState.Asm.Label(ref zero);
            _currentState.Asm.xor(dest, dest);

            _currentState.Asm.Label(ref done);
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
            Label div = _currentState.Asm.CreateLabel();
            Label zero = _currentState.Asm.CreateLabel();
            Label done = _currentState.Asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                _currentState.Asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Move numerator to rax
            if (op1Const)
            {
                _currentState.Asm.mov(rax, op1Value);
            }
            else
            {
                _currentState.Asm.mov(rax, GetLocation(op1).GetRegister());
            }

            // Move denominator to dest
            if (op2Const)
            {
                _currentState.Asm.mov(dest, op2Value);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op2).GetRegister());
            }

            AssemblerRegister64 denominator = dest;

            // Emit runtime check 
            // denominator == 0 ?
            _currentState.Asm.test(denominator, denominator);
            _currentState.Asm.jz(zero);

            // use rdx to hold 0x8000_0000_0000_0000 temporarily for the check
            // numerator == 0x8000_0000_0000_0000 ?
            _currentState.Asm.mov(rdx, 0x8000_0000_0000_0000);
            _currentState.Asm.cmp(rax, rdx);
            _currentState.Asm.jnz(div);

            // denominator == -1 ?
            _currentState.Asm.cmp(denominator, -1);
            _currentState.Asm.jnz(div);
            _currentState.Asm.jmp(zero);

            _currentState.Asm.Label(ref div);

            _currentState.Asm.cqo();              // Sign extend rax into rdx:rax for the division
            _currentState.Asm.idiv(denominator);  // divide
            _currentState.Asm.mov(dest, rax);     // mov result
            _currentState.Asm.jmp(done);

            _currentState.Asm.Label(ref zero);
            _currentState.Asm.xor(dest, dest);

            _currentState.Asm.Label(ref done);
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

            Label zero = _currentState.Asm.CreateLabel();
            Label done = _currentState.Asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                _currentState.Asm.xor(dest_64, dest_64);
                return;
            }

            // Actual division required 

            // Zero the top 64-bit of numerator
            _currentState.Asm.xor(rdx, rdx);

            // Move numerator to eax
            if (op1Const)
            {
                _currentState.Asm.mov(eax, (uint)op1Value);
            }
            else
            {
                _currentState.Asm.mov(eax, Reg64ToReg32(GetLocation(op1).GetRegister()));
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                _currentState.Asm.mov(dest_32, (uint)op2Value);               // use dest to hold denominator
                _currentState.Asm.div(dest_32);                               // divide
                _currentState.Asm.mov(dest_32, eax);                          // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister32 denominator = Reg64ToReg32(GetLocation(op2).GetRegister());

            // denominator == 0 ?
            _currentState.Asm.test(denominator, denominator);
            _currentState.Asm.jz(zero);

            _currentState.Asm.div(denominator);       // divide
            _currentState.Asm.mov(dest_32, eax);      // mov result and zero extend
            _currentState.Asm.jmp(done);

            _currentState.Asm.Label(ref zero);
            _currentState.Asm.xor(dest_64, dest_64);

            _currentState.Asm.Label(ref done);
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
                _currentState.Asm.xor(dest_64, dest_64);
                return;
            }

            Label div = _currentState.Asm.CreateLabel();
            Label zero = _currentState.Asm.CreateLabel();
            Label done = _currentState.Asm.CreateLabel();

            // Move numerator to eax
            if (op1Const)
            {
                _currentState.Asm.mov(eax, (uint)op2Value);
            }
            else
            {
                _currentState.Asm.mov(eax, Reg64ToReg32(GetLocation(op1).GetRegister()));
            }

            // use dest to hold denominator 
            if (op2Const)
            {
                _currentState.Asm.mov(dest_32, (uint)op2Value);
            }
            else
            {
                _currentState.Asm.mov(dest_32, Reg64ToReg32(GetLocation(op2).GetRegister()));
            }

            AssemblerRegister32 denominator = dest_32;

            // Emit runtime check 
            // denominator == 0 ?
            _currentState.Asm.test(denominator, denominator);
            _currentState.Asm.jz(zero);

            // numerator == 0x8000_0000 ?
            _currentState.Asm.cmp(eax, 0x8000_0000);
            _currentState.Asm.jnz(div);

            // denominator == -1 ?
            _currentState.Asm.cmp(denominator, -1);
            _currentState.Asm.jnz(div);

            _currentState.Asm.jmp(zero);

            _currentState.Asm.Label(ref div);

            _currentState.Asm.cdq();             // Sign extend eax into edx:eax for the division
            _currentState.Asm.idiv(denominator); // divide
            _currentState.Asm.mov(dest_32, eax); // mov result and zero extend
            _currentState.Asm.jmp(done);

            _currentState.Asm.Label(ref zero);
            _currentState.Asm.xor(dest_64, dest_64);

            _currentState.Asm.Label(ref done);
        }

        private void ShiftLeft(IRInstruction instruction)
        {
            Location destination = GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (TryGetConst(op1, out ulong op1Value))
            {
                _currentState.Asm.mov(dest, op1Value);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.shl(dest, (byte)op2Value);
            }
            else
            {
                _currentState.Asm.shlx(dest, dest, GetLocation(op2).GetRegister()); // BMI2 extension
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
                _currentState.Asm.mov(dest, op1Value);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                if (arithmetic)
                {
                    _currentState.Asm.sar(dest, (byte)op2Value);
                }
                else
                {
                    _currentState.Asm.shr(dest, (byte)op2Value);
                }
            }
            else
            {
                if (arithmetic)
                {
                    _currentState.Asm.sarx(dest, dest, GetLocation(op2).GetRegister());  // BMI2 extension
                }
                else
                {
                    _currentState.Asm.shrx(dest, dest, GetLocation(op2).GetRegister());  // BMI2 extension
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
                _currentState.Asm.mov(dest, op1Value);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.rol(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                _currentState.Asm.rol(dest, Reg64ToReg8(GetLocation(op2).GetRegister()));
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
                _currentState.Asm.mov(dest, op1Value);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
            }

            if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.ror(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                _currentState.Asm.ror(dest, Reg64ToReg8(GetLocation(op2).GetRegister()));
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());

            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                _currentState.Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            _currentState.Asm.sete(Reg64ToReg8(dest));
            _currentState.Asm.movzx(dest, Reg64ToReg8(dest));
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                _currentState.Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            _currentState.Asm.setne(Reg64ToReg8(dest));
            _currentState.Asm.movzx(dest, Reg64ToReg8(dest));
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                _currentState.Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _currentState.Asm.setl(Reg64ToReg8(dest));
            }
            else
            {
                _currentState.Asm.setb(Reg64ToReg8(dest));
            }

            _currentState.Asm.movzx(dest, Reg64ToReg8(dest));
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                _currentState.Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _currentState.Asm.setg(Reg64ToReg8(dest));
            }
            else
            {
                _currentState.Asm.seta(Reg64ToReg8(dest));
            }

            _currentState.Asm.movzx(dest, Reg64ToReg8(dest));
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                _currentState.Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _currentState.Asm.setle(Reg64ToReg8(dest));
            }
            else
            {
                _currentState.Asm.setbe(Reg64ToReg8(dest));
            }

            _currentState.Asm.movzx(dest, Reg64ToReg8(dest));
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
                _currentState.Asm.mov(dest, op1Value);
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }
            else if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
                _currentState.Asm.cmp(GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                _currentState.Asm.mov(dest, GetLocation(op1).GetRegister());
                _currentState.Asm.cmp(dest, GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                _currentState.Asm.setge(Reg64ToReg8(dest));
            }
            else
            {
                _currentState.Asm.setae(Reg64ToReg8(dest));
            }

            _currentState.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private void Select(IRInstruction instruction)
        {
            Label done = _currentState.Asm.CreateLabel();

            Location destination = GetLocation(instruction.Destination);
            Location condition = GetLocation(instruction.Operand1);

            Value op2 = instruction.Operand2;
            Value op3 = instruction.Operand3;

            AssemblerRegister64 cond = condition.GetRegister();
            AssemblerRegister64 dest = destination.GetRegister();

            // test the condition
            _currentState.Asm.test(cond, cond);

            // assume non zero
            if (TryGetConst(op2, out ulong op2Value))
            {
                _currentState.Asm.mov(dest, op2Value);
            }
            else
            {
                Location location2 = GetLocation(instruction.Operand2);
                _currentState.Asm.mov(dest, location2.GetRegister());
            }

            // branch
            _currentState.Asm.jnz(done);

            // if zero fallthough, overwrite

            if (TryGetConst(op3, out ulong op3Value))
            {
                _currentState.Asm.mov(dest, op3Value);
            }
            else
            {
                Location location3 = GetLocation(instruction.Operand3);
                _currentState.Asm.mov(dest, location3.GetRegister());
            }


            _currentState.Asm.Label(ref done);
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
                    _currentState.Asm.movzx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _currentState.Asm.movzx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    _currentState.Asm.mov(Reg64ToReg32(dest), Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        internal void ZeroExtendTo32(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _currentState.Asm.movzx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _currentState.Asm.movzx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        internal void ZeroExtendTo16(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _currentState.Asm.movzx(Reg64ToReg16(dest), Reg64ToReg8(operand));
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
                    _currentState.Asm.movsx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _currentState.Asm.movsx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    _currentState.Asm.movsxd(dest, Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        internal void SignExtendTo32(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _currentState.Asm.movsx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    _currentState.Asm.movsx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        internal void SignExtendTo16(AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    _currentState.Asm.movsx(Reg64ToReg16(dest), Reg64ToReg8(operand));
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

                _currentState.Asm.mov(destinationReg, value & mask);

            }
            else
            {
                AssemblerRegister64 operandReg = GetLocation(operand).GetRegister();

                switch (destination.Type)
                {
                    case Value.ValueType.I64:
                        return;

                    case Value.ValueType.I32:
                        _currentState.Asm.mov(Reg64ToReg32(destinationReg), Reg64ToReg32(operandReg));
                        break;

                    case Value.ValueType.I16:
                        _currentState.Asm.movzx(destinationReg, Reg64ToReg16(operandReg));
                        break;

                    case Value.ValueType.I8:
                        _currentState.Asm.movzx(destinationReg, Reg64ToReg8(operandReg));
                        break;

                    default: throw new Exception($"Unsupported zero extend to {destination.Type}");
                }
            }
        }
    }
}
