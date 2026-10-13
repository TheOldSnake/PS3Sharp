using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal sealed partial class X64Generator
    {
        private static void Add(X64Context ctx, AssemblerRegister64 destReg, AssemblerRegister64 srcReg, ulong imm)
        {
            ctx.Asm.mov(destReg, imm);
            ctx.Asm.add(destReg, srcReg);
        }

        private static void Add(X64Context ctx, AssemblerRegister64 destReg, AssemblerRegister64 src1Reg, AssemblerRegister64 src2Reg)
        {
            ctx.Asm.mov(destReg, src1Reg);
            ctx.Asm.add(destReg, src2Reg);
        }

        private static void CommutativeOp(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 reg2;

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                reg2 = ctx.GetLocation(op2).GetRegister();
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                reg2 = ctx.GetLocation(op1).GetRegister();
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                reg2 = ctx.GetLocation(op2).GetRegister();
            }

            switch (instruction.Opcode)
            {
                case IRInstruction.Op.Add:
                    ctx.Asm.add(dest, reg2);
                    break;

                case IRInstruction.Op.And:
                    ctx.Asm.and(dest, reg2);
                    break;

                case IRInstruction.Op.Or:
                    ctx.Asm.or(dest, reg2);
                    break;

                case IRInstruction.Op.Xor:
                    ctx.Asm.xor(dest, reg2);
                    break;

                default:
                    throw new NotImplementedException($"Unknown CommutativeOp = {instruction.Opcode}");
            }
        }

        private static void Sub(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                // A - B
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.sub(dest, ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                // - B + A
                ctx.Asm.mov(dest, -(long)op2Value);
                ctx.Asm.add(dest, ctx.GetLocation(op1).GetRegister());
            }
            else
            {
                // A - B
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.sub(dest, ctx.GetLocation(op2).GetRegister());
            }
        }

        private static void Not(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, ~op1Value);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.not(dest);
            }
        }

        private static void IMulLD(X64Context ctx, IRInstruction instruction)
        {
            // Signed 64 × 64 → Low 64-bit

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                AssemblerRegister64 op2Reg = ctx.GetLocation(op2).GetRegister();
                ctx.Asm.imul(dest, op2Reg);
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                AssemblerRegister64 op1Reg = ctx.GetLocation(op1).GetRegister();
                ctx.Asm.imul(dest, op1Reg);
            }
            else
            {
                AssemblerRegister64 op1Reg = ctx.GetLocation(op1).GetRegister();
                AssemblerRegister64 op2Reg = ctx.GetLocation(op2).GetRegister();
                ctx.Asm.mov(dest, op1Reg);
                ctx.Asm.imul(dest, op2Reg);
            }
        }

        private static void IMulHD(X64Context ctx, IRInstruction instruction)
        {
            // Signed 64 × 64 → High 64-bit

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // Use imul(reg) form

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(rax, op1Value);
                ctx.Asm.imul(ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(rax, op2Value);
                ctx.Asm.imul(ctx.GetLocation(op1).GetRegister());
            }
            else
            {
                ctx.Asm.mov(rax, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.imul(ctx.GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            ctx.Asm.mov(dest, rdx);
        }

        private static void MulHD(X64Context ctx, IRInstruction instruction)
        {
            // Unsigned 64 × 64 → High 64-bit

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // Use mul(reg) form

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(rax, op1Value);
                ctx.Asm.mul(ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(rax, op2Value);
                ctx.Asm.mul(ctx.GetLocation(op1).GetRegister());
            }
            else
            {
                ctx.Asm.mov(rax, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.mul(ctx.GetLocation(op2).GetRegister());
            }

            // rdx contains high 64-bits
            ctx.Asm.mov(dest, rdx);
        }

        private static void IMulLW(X64Context ctx, IRInstruction instruction)
        {
            // Signed 32 × 32 → Full 64-bit

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = ctx.BorrowRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                ctx.Asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = ctx.GetLocation(op1).GetRegister();
                ctx.Asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                ctx.Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = ctx.GetLocation(op2).GetRegister();
                ctx.Asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            // No shift needed
            ctx.Asm.imul(dest, temp);

            ctx.ReturnBorrowedRegister(temp);
        }

        private static void IMulHW(X64Context ctx, IRInstruction instruction)
        {
            // Signed 32 × 32 → High 32-bit, Sign-Extended

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = ctx.BorrowRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                long signExtended = (int)op1Value;
                ctx.Asm.mov(dest, signExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = ctx.GetLocation(op1).GetRegister();
                ctx.Asm.movsxd(dest, Reg64ToReg32(op1Reg));
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                ctx.Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = ctx.GetLocation(op2).GetRegister();
                ctx.Asm.movsxd(temp, Reg64ToReg32(op2Reg));
            }

            ctx.Asm.imul(dest, temp);
            ctx.Asm.sar(dest, 32);   // arithmetic shift

            ctx.ReturnBorrowedRegister(temp);
        }

        private static void MulHW(X64Context ctx, IRInstruction instruction)
        {
            // Unsigned 32 × 32 → High 32-bit, Zero-Extended

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            AssemblerRegister64 temp = ctx.BorrowRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                uint zeroExtended = (uint)op1Value;
                ctx.Asm.mov(dest, zeroExtended);
            }
            else
            {
                AssemblerRegister64 op1Reg = ctx.GetLocation(op1).GetRegister();
                ctx.Asm.mov(Reg64ToReg32(dest), Reg64ToReg32(op1Reg)); // automatic zero extend
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                long signExtended = (int)op2Value;
                ctx.Asm.mov(temp, signExtended);
            }
            else
            {
                AssemblerRegister64 op2Reg = ctx.GetLocation(op2).GetRegister();
                ctx.Asm.mov(Reg64ToReg32(temp), Reg64ToReg32(op2Reg)); // automatic zero extend
            }

            ctx.Asm.imul(dest, temp);
            ctx.Asm.shr(dest, 32);   // logical shift

            ctx.ReturnBorrowedRegister(temp);
        }

        private static void CheckedDivD(X64Context ctx, IRInstruction instruction)
        {
            // Unsigned 64/64, result -> 0 If denominator is zero

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();
            Label zero = ctx.Asm.CreateLabel();
            Label done = ctx.Asm.CreateLabel();

            bool op1Const = ctx.TryGetConst(op1, out ulong op1Value);
            bool op2Const = ctx.TryGetConst(op2, out ulong op2Value);

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                ctx.Asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Zero the numerator top 64-bit
            ctx.Asm.xor(rdx, rdx);

            // Mov numerator to rax
            if (op1Const)
            {
                ctx.Asm.mov(rax, op1Value);
            }
            else
            {
                ctx.Asm.mov(rax, ctx.GetLocation(op1).GetRegister());
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                ctx.Asm.mov(dest, op2Value);                    // use dest to hold denominator
                ctx.Asm.div(dest);                              // divide
                ctx.Asm.mov(dest, rax);                         // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister64 denominator = ctx.GetLocation(op2).GetRegister();

            // denominator == 0 ?
            ctx.Asm.test(denominator, denominator);
            ctx.Asm.jz(zero);

            ctx.Asm.div(denominator);       // divide
            ctx.Asm.mov(dest, rax);         // mov result and zero extend
            ctx.Asm.jmp(done);

            ctx.Asm.Label(ref zero);
            ctx.Asm.xor(dest, dest);

            ctx.Asm.Label(ref done);
        }

        private static void CheckedIDivD(X64Context ctx, IRInstruction instruction)
        {
            // Signed 64/64, result -> 0 If denominator is zero || (numerator is 0x8000_0000_0000_0000 && denominator is -1)

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            // TODO: Find a better way to do this

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = ctx.TryGetConst(op1, out ulong op1Value);
            bool op2Const = ctx.TryGetConst(op2, out ulong op2Value);

            AssemblerRegister64 dest = destination.GetRegister();
            Label div = ctx.Asm.CreateLabel();
            Label zero = ctx.Asm.CreateLabel();
            Label done = ctx.Asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                ctx.Asm.xor(dest, dest);
                return;
            }

            // Actual division required 

            // Move numerator to rax
            if (op1Const)
            {
                ctx.Asm.mov(rax, op1Value);
            }
            else
            {
                ctx.Asm.mov(rax, ctx.GetLocation(op1).GetRegister());
            }

            // Move denominator to dest
            if (op2Const)
            {
                ctx.Asm.mov(dest, op2Value);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op2).GetRegister());
            }

            AssemblerRegister64 denominator = dest;

            // Emit runtime check 
            // denominator == 0 ?
            ctx.Asm.test(denominator, denominator);
            ctx.Asm.jz(zero);

            // use rdx to hold 0x8000_0000_0000_0000 temporarily for the check
            // numerator == 0x8000_0000_0000_0000 ?
            ctx.Asm.mov(rdx, 0x8000_0000_0000_0000);
            ctx.Asm.cmp(rax, rdx);
            ctx.Asm.jnz(div);

            // denominator == -1 ?
            ctx.Asm.cmp(denominator, -1);
            ctx.Asm.jnz(div);
            ctx.Asm.jmp(zero);

            ctx.Asm.Label(ref div);

            ctx.Asm.cqo();              // Sign extend rax into rdx:rax for the division
            ctx.Asm.idiv(denominator);  // divide
            ctx.Asm.mov(dest, rax);     // mov result
            ctx.Asm.jmp(done);

            ctx.Asm.Label(ref zero);
            ctx.Asm.xor(dest, dest);

            ctx.Asm.Label(ref done);
        }

        private static void CheckedDivW(X64Context ctx, IRInstruction instruction)
        {
            // Unsigned 32/32, result -> 0 If denominator is zero

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = ctx.TryGetConst(op1, out ulong op1Value);
            bool op2Const = ctx.TryGetConst(op2, out ulong op2Value);

            AssemblerRegister64 dest_64 = destination.GetRegister();
            AssemblerRegister32 dest_32 = Reg64ToReg32(dest_64);

            Label zero = ctx.Asm.CreateLabel();
            Label done = ctx.Asm.CreateLabel();

            // Special case that we can check at compile time
            if (op2Const && op2Value == 0)
            {
                ctx.Asm.xor(dest_64, dest_64);
                return;
            }

            // Actual division required 

            // Zero the top 64-bit of numerator
            ctx.Asm.xor(rdx, rdx);

            // Move numerator to eax
            if (op1Const)
            {
                ctx.Asm.mov(eax, (uint)op1Value);
            }
            else
            {
                ctx.Asm.mov(eax, Reg64ToReg32(ctx.GetLocation(op1).GetRegister()));
            }

            // if op2 is constant no runtime check is needed
            if (op2Const)
            {
                ctx.Asm.mov(dest_32, (uint)op2Value);               // use dest to hold denominator
                ctx.Asm.div(dest_32);                               // divide
                ctx.Asm.mov(dest_32, eax);                          // mov result and zero extend
                return;
            }

            // Othwewise a runtime check is required
            AssemblerRegister32 denominator = Reg64ToReg32(ctx.GetLocation(op2).GetRegister());

            // denominator == 0 ?
            ctx.Asm.test(denominator, denominator);
            ctx.Asm.jz(zero);

            ctx.Asm.div(denominator);       // divide
            ctx.Asm.mov(dest_32, eax);      // mov result and zero extend
            ctx.Asm.jmp(done);

            ctx.Asm.Label(ref zero);
            ctx.Asm.xor(dest_64, dest_64);

            ctx.Asm.Label(ref done);
        }

        private static void CheckedIDivW(X64Context ctx, IRInstruction instruction)
        {
            // Signed 32/32, result -> 0 If denominator is zero || (numerator is 0x8000_0000 && denominator is -1)

            // Note: the existence of this opcode means that RAX and RDX are guaranteed to be free by the allocator

            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            bool op1Const = ctx.TryGetConst(op1, out ulong op1Value);
            bool op2Const = ctx.TryGetConst(op2, out ulong op2Value);

            AssemblerRegister64 dest_64 = destination.GetRegister();
            AssemblerRegister32 dest_32 = Reg64ToReg32(dest_64);

            // Special case that we can check at compile time
            if (op2Const && ((uint)op2Value) == 0)
            {
                ctx.Asm.xor(dest_64, dest_64);
                return;
            }

            Label div = ctx.Asm.CreateLabel();
            Label zero = ctx.Asm.CreateLabel();
            Label done = ctx.Asm.CreateLabel();

            // Move numerator to eax
            if (op1Const)
            {
                ctx.Asm.mov(eax, (uint)op2Value);
            }
            else
            {
                ctx.Asm.mov(eax, Reg64ToReg32(ctx.GetLocation(op1).GetRegister()));
            }

            // use dest to hold denominator 
            if (op2Const)
            {
                ctx.Asm.mov(dest_32, (uint)op2Value);
            }
            else
            {
                ctx.Asm.mov(dest_32, Reg64ToReg32(ctx.GetLocation(op2).GetRegister()));
            }

            AssemblerRegister32 denominator = dest_32;

            // Emit runtime check 
            // denominator == 0 ?
            ctx.Asm.test(denominator, denominator);
            ctx.Asm.jz(zero);

            // numerator == 0x8000_0000 ?
            ctx.Asm.cmp(eax, 0x8000_0000);
            ctx.Asm.jnz(div);

            // denominator == -1 ?
            ctx.Asm.cmp(denominator, -1);
            ctx.Asm.jnz(div);

            ctx.Asm.jmp(zero);

            ctx.Asm.Label(ref div);

            ctx.Asm.cdq();             // Sign extend eax into edx:eax for the division
            ctx.Asm.idiv(denominator); // divide
            ctx.Asm.mov(dest_32, eax); // mov result and zero extend
            ctx.Asm.jmp(done);

            ctx.Asm.Label(ref zero);
            ctx.Asm.xor(dest_64, dest_64);

            ctx.Asm.Label(ref done);
        }

        private static void ShiftLeft(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.shl(dest, (byte)op2Value);
            }
            else
            {
                ctx.Asm.shlx(dest, dest, ctx.GetLocation(op2).GetRegister()); // BMI2 extension
            }
        }

        private static void ShiftRight(X64Context ctx, IRInstruction instruction, bool arithmetic)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                if (arithmetic)
                {
                    ctx.Asm.sar(dest, (byte)op2Value);
                }
                else
                {
                    ctx.Asm.shr(dest, (byte)op2Value);
                }
            }
            else
            {
                if (arithmetic)
                {
                    ctx.Asm.sarx(dest, dest, ctx.GetLocation(op2).GetRegister());  // BMI2 extension
                }
                else
                {
                    ctx.Asm.shrx(dest, dest, ctx.GetLocation(op2).GetRegister());  // BMI2 extension
                }
            }
        }

        private static void RotateLeft(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.rol(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                ctx.Asm.rol(dest, Reg64ToReg8(ctx.GetLocation(op2).GetRegister()));
                throw new Exception($"todo: rol");
            }
        }

        private static void RotateRight(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
            }

            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.ror(dest, (byte)op2Value);
            }
            else
            {
                // we need to use CL
                ctx.Asm.ror(dest, Reg64ToReg8(ctx.GetLocation(op2).GetRegister()));
                throw new Exception($"todo: ror");
            }
        }

        private static void SetEq(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());

            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                ctx.Asm.cmp(ctx.GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }

            ctx.Asm.sete(Reg64ToReg8(dest));
            ctx.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetNotEq(X64Context ctx, IRInstruction instruction)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                ctx.Asm.cmp(ctx.GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }

            ctx.Asm.setne(Reg64ToReg8(dest));
            ctx.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetLessThan(X64Context ctx, IRInstruction instruction, bool signed)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                ctx.Asm.cmp(ctx.GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                ctx.Asm.setl(Reg64ToReg8(dest));
            }
            else
            {
                ctx.Asm.setb(Reg64ToReg8(dest));
            }

            ctx.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetGreaterThan(X64Context ctx, IRInstruction instruction, bool signed)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                ctx.Asm.cmp(ctx.GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                ctx.Asm.setg(Reg64ToReg8(dest));
            }
            else
            {
                ctx.Asm.seta(Reg64ToReg8(dest));
            }

            ctx.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetLessThanEq(X64Context ctx, IRInstruction instruction, bool signed)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                ctx.Asm.cmp(ctx.GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                ctx.Asm.setle(Reg64ToReg8(dest));
            }
            else
            {
                ctx.Asm.setbe(Reg64ToReg8(dest));
            }

            ctx.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void SetGreaterThanEq(X64Context ctx, IRInstruction instruction, bool signed)
        {
            Location destination = ctx.GetLocation(instruction.Destination);
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;

            AssemblerRegister64 dest = destination.GetRegister();

            // asm.xor(dest, dest);

            // always compare 1 vs 2
            if (ctx.TryGetConst(op1, out ulong op1Value))
            {
                ctx.Asm.mov(dest, op1Value);
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }
            else if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
                ctx.Asm.cmp(ctx.GetLocation(op1).GetRegister(), dest);
            }
            else
            {
                ctx.Asm.mov(dest, ctx.GetLocation(op1).GetRegister());
                ctx.Asm.cmp(dest, ctx.GetLocation(op2).GetRegister());
            }

            if (signed)
            {
                ctx.Asm.setge(Reg64ToReg8(dest));
            }
            else
            {
                ctx.Asm.setae(Reg64ToReg8(dest));
            }

            ctx.Asm.movzx(dest, Reg64ToReg8(dest));
        }

        private static void Select(X64Context ctx, IRInstruction instruction)
        {
            Label done = ctx.Asm.CreateLabel();

            Location destination = ctx.GetLocation(instruction.Destination);
            Location condition = ctx.GetLocation(instruction.Operand1);

            Value op2 = instruction.Operand2;
            Value op3 = instruction.Operand3;

            AssemblerRegister64 cond = condition.GetRegister();
            AssemblerRegister64 dest = destination.GetRegister();

            // test the condition
            ctx.Asm.test(cond, cond);

            // assume non zero
            if (ctx.TryGetConst(op2, out ulong op2Value))
            {
                ctx.Asm.mov(dest, op2Value);
            }
            else
            {
                Location location2 = ctx.GetLocation(instruction.Operand2);
                ctx.Asm.mov(dest, location2.GetRegister());
            }

            // branch
            ctx.Asm.jnz(done);

            // if zero fallthough, overwrite

            if (ctx.TryGetConst(op3, out ulong op3Value))
            {
                ctx.Asm.mov(dest, op3Value);
            }
            else
            {
                Location location3 = ctx.GetLocation(instruction.Operand3);
                ctx.Asm.mov(dest, location3.GetRegister());
            }


            ctx.Asm.Label(ref done);
        }

        private static void ZeroExtend(X64Context ctx, IRInstruction instruction)
        {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = ctx.GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister64 operandReg = ctx.GetLocation(instruction.Operand1).GetRegister();

            // using a constant would be weird, a possible IR bug
            if (ctx.IsConst(instruction.Operand1))
            {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            switch (destination.Type)
            {
                case Value.ValueType.I64:
                    ZeroExtendTo64(ctx, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I32:
                    ZeroExtendTo32(ctx, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I16:
                    ZeroExtendTo16(ctx, destinationReg, operand.Type, operandReg);
                    break;

                default: throw new Exception($"Unsupported zero extend to {destination.Type}");
            }
        }

        private static void ZeroExtendTo64(X64Context ctx, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    ctx.Asm.movzx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    ctx.Asm.movzx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    ctx.Asm.mov(Reg64ToReg32(dest), Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        private static void ZeroExtendTo32(X64Context ctx, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    ctx.Asm.movzx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    ctx.Asm.movzx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        private static void ZeroExtendTo16(X64Context ctx, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    ctx.Asm.movzx(Reg64ToReg16(dest), Reg64ToReg8(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I16");
            }
        }

        private static void SignExtend(X64Context ctx, IRInstruction instruction)
        {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = ctx.GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister64 operandReg = ctx.GetLocation(instruction.Operand1).GetRegister();

            // using a constant would be weird, a possible IR bug
            if (ctx.IsConst(instruction.Operand1))
            {
                throw new Exception($"Unexpected Zero exten with constant operand");
            }

            switch (destination.Type)
            {
                case Value.ValueType.I64:
                    SignExtendTo64(ctx, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I32:
                    SignExtendTo32(ctx, destinationReg, operand.Type, operandReg);
                    break;

                case Value.ValueType.I16:
                    SignExtendTo16(ctx, destinationReg, operand.Type, operandReg);
                    break;

                default: throw new Exception($"Unsupported zero extend to {destination.Type}");
            }
        }

        private static void SignExtendTo64(X64Context ctx, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    ctx.Asm.movsx(dest, Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    ctx.Asm.movsx(dest, Reg64ToReg16(operand));
                    break;

                case Value.ValueType.I32:
                    ctx.Asm.movsxd(dest, Reg64ToReg32(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I64");
            }
        }

        private static void SignExtendTo32(X64Context ctx, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    ctx.Asm.movsx(Reg64ToReg32(dest), Reg64ToReg8(operand));
                    break;

                case Value.ValueType.I16:
                    ctx.Asm.movsx(Reg64ToReg32(dest), Reg64ToReg16(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I32");
            }
        }

        private static void SignExtendTo16(X64Context ctx, AssemblerRegister64 dest, Value.ValueType operandType, AssemblerRegister64 operand)
        {
            switch (operandType)
            {
                case Value.ValueType.I8:
                    ctx.Asm.movsx(Reg64ToReg16(dest), Reg64ToReg8(operand));
                    break;

                default: throw new Exception($"Unsupported zero extend from {operandType} to I16");
            }
        }

        private static void Truncate(X64Context ctx, IRInstruction instruction)
        {
            Value destination = instruction.Destination;
            Value operand = instruction.Operand1;

            AssemblerRegister64 destinationReg = ctx.GetLocation(instruction.Destination).GetRegister();

            if (ctx.TryGetConst(instruction.Operand1, out ulong value))
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

                ctx.Asm.mov(destinationReg, value & mask);

            }
            else
            {
                AssemblerRegister64 operandReg = ctx.GetLocation(operand).GetRegister();

                switch (destination.Type)
                {
                    case Value.ValueType.I64:
                        return;

                    case Value.ValueType.I32:
                        ctx.Asm.mov(Reg64ToReg32(destinationReg), Reg64ToReg32(operandReg));
                        break;

                    case Value.ValueType.I16:
                        ctx.Asm.movzx(destinationReg, Reg64ToReg16(operandReg));
                        break;

                    case Value.ValueType.I8:
                        ctx.Asm.movzx(destinationReg, Reg64ToReg8(operandReg));
                        break;

                    default: throw new Exception($"Unsupported zero extend to {destination.Type}");
                }
            }
        }
    }
}