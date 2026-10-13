using Iced.Intel;
using System.Buffers.Binary;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal unsafe sealed partial class X64Generator
    {
        private static void PCStore(X64Context ctx, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetPCOffset();

            // PC is 32-bit
            if (ctx.TryGetConst(src, out ulong value))
            {
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = ctx.GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private static void LRLoad(X64Context ctx, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = ctx.GetLocation(instruction.Destination);
            int offset = GetLROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // LR is 32-bit
            ctx.Asm.mov(reg, __dword_ptr[ctx.PPUState + offset]);
        }

        private static void LRStore(X64Context ctx, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetLROffset();

            // LR is 32-bit
            if (ctx.TryGetConst(src, out ulong value))
            {
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = ctx.GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private static void CRLoad(X64Context ctx, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = ctx.GetLocation(instruction.Destination);
            int offset = GetCROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // CR is 32-bit
            ctx.Asm.mov(reg, __dword_ptr[ctx.PPUState + offset]);
        }

        private static void CRStore(X64Context ctx, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetCROffset();

            // CR is 32-bit
            if (ctx.TryGetConst(src, out ulong value))
            {
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = ctx.GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private static void CTRLoad(X64Context ctx, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = ctx.GetLocation(instruction.Destination);
            int offset = GetCTROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // CTR is 64-bit
            ctx.Asm.mov(reg, __qword_ptr[ctx.PPUState + offset]);
        }

        private static void CTRStore(X64Context ctx, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetCTROffset();

            // CTR is 64-bit
            if (ctx.TryGetConst(src, out ulong value))
            {
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], (uint)value);
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + (offset + 4)], (uint)(value >> 32));
            }
            else
            {
                Location dest = ctx.GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                ctx.Asm.mov(__qword_ptr[ctx.PPUState + offset], reg);
            }
        }

        private static void XERLoad(X64Context ctx, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = ctx.GetLocation(instruction.Destination);
            int offset = GetXEROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // XER is 64-bit
            ctx.Asm.mov(reg, __qword_ptr[ctx.PPUState + offset]);
        }

        private static void XERStore(X64Context ctx, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetXEROffset();

            // XER is 64-bit
            if (ctx.TryGetConst(src, out ulong value))
            {
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], (uint)value);
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + (offset + 4)], (uint)(value >> 32));
            }
            else
            {
                Location dest = ctx.GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                ctx.Asm.mov(__qword_ptr[ctx.PPUState + offset], reg);
            }
        }

        private static void GPRLoad(X64Context ctx, IRInstruction instruction)
        {
            int gpr = (int)instruction.Immediate1;
            int gprOffset = GetGPROffset(gpr);

            // Destination is always a register
            Location dest = ctx.GetLocation(instruction.Destination);
            AssemblerRegister64 reg = dest.GetRegister();
            ctx.Asm.mov(reg, __qword_ptr[ctx.PPUState + gprOffset]);
        }

        private static void GPRStore(X64Context ctx, IRInstruction instruction)
        {
            int gpr = (int)instruction.Immediate1;
            int offset = GetGPROffset(gpr);

            Value src = instruction.Destination;

            // GPR is 64-bit
            if (ctx.TryGetConst(src, out ulong value))
            {
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + offset], (uint)value);
                ctx.Asm.mov(__dword_ptr[ctx.PPUState + (offset + 4)], (uint)(value >> 32));
            }
            else
            {
                AssemblerRegister64 reg = ctx.GetLocation(src).GetRegister();
                ctx.Asm.mov(__qword_ptr[ctx.PPUState + offset], reg);
            }
        }

        private static AssemblerMemoryOperand GetPointer<T>(X64Context ctx, Value memoryAddress, uint offset = 0) where T : unmanaged
        {
            if (ctx.TryGetConst(memoryAddress, out ulong virtualAddress))
            {
                return GetPointerImmOffset<T>(ctx, (uint)virtualAddress, offset);
            }
            else
            {
                AssemblerRegister64 virtualAddressReg = ctx.GetLocation(memoryAddress).GetRegister();
                return GetPointerRegOffset<T>(ctx, virtualAddressReg, offset);
            }
        }

        private static AssemblerMemoryOperand GetPointerImmOffset<T>(X64Context ctx, uint baseOffset, uint extraOffset = 0) where T : unmanaged
        {
            int size = sizeof(T);
            uint finalOffset = baseOffset + extraOffset;

            return size switch
            {
                1 => __byte_ptr[ctx.MemoryBase + finalOffset],
                2 => __word_ptr[ctx.MemoryBase + finalOffset],
                4 => __dword_ptr[ctx.MemoryBase + finalOffset],
                8 => __qword_ptr[ctx.MemoryBase + finalOffset],
                _ => throw new Exception($"[x64 Generator] Invalid pointer size = {size}"),
            };
        }

        private static AssemblerMemoryOperand GetPointerRegOffset<T>(X64Context ctx, AssemblerRegister64 baseOffset, uint extraOffset = 0) where T : unmanaged
        {
            int size = sizeof(T);

            return size switch
            {
                1 => __byte_ptr[ctx.MemoryBase + baseOffset + extraOffset],
                2 => __word_ptr[ctx.MemoryBase + baseOffset + extraOffset],
                4 => __dword_ptr[ctx.MemoryBase + baseOffset + extraOffset],
                8 => __qword_ptr[ctx.MemoryBase + baseOffset + extraOffset],
                _ => throw new Exception($"[x64 Generator] Invalid pointer size = {size}"),
            };
        }

        private static void Store64(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand destPtrQ = GetPointer<ulong>(ctx, memoryAddress);
            AssemblerMemoryOperand destPtrD0 = GetPointer<uint>(ctx, memoryAddress, offset: 0);
            AssemblerMemoryOperand destPtrD1 = GetPointer<uint>(ctx, memoryAddress, offset: 4);

            if (ctx.TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate 64-bit value - convert to BE in place before writing
                ulong swapped = BinaryPrimitives.ReverseEndianness(value);
                ctx.Asm.mov(destPtrD0, (uint)swapped);
                ctx.Asm.mov(destPtrD1, (uint)(swapped >> 32));
            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = ctx.GetLocation(src).GetRegister();
                ctx.Asm.movbe(destPtrQ, valueReg);
            }
        }

        private static void Load64(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<ulong>(ctx, memoryAddress);

            // Destination is always a register
            // use movbe to swap bytes
            ctx.Asm.movbe(ctx.GetLocation(instruction.Destination).GetRegister(), src);
        }

        private static void Store32(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<uint>(ctx, memoryAddress);

            if (ctx.TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                uint swapped = BinaryPrimitives.ReverseEndianness((uint)value);
                ctx.Asm.mov(dest, swapped);
            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = ctx.GetLocation(src).GetRegister();
                ctx.Asm.movbe(dest, Reg64ToReg32(valueReg));
            }
        }

        private static void Load32(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<uint>(ctx, memoryAddress);

            // Destination is always a register
            AssemblerRegister64 dest64 = ctx.GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister32 dest32 = Reg64ToReg32(dest64);

            // use movbe to swap bytes
            // automatically zero extends to 64-bits

            ctx.Asm.movbe(dest32, src);
        }

        private static void Store16(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<ushort>(ctx, memoryAddress);

            if (ctx.TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                ushort swapped = BinaryPrimitives.ReverseEndianness((ushort)value);
                ctx.Asm.mov(dest, swapped);
            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = ctx.GetLocation(src).GetRegister();
                ctx.Asm.movbe(dest, Reg64ToReg16(valueReg));
            }
        }

        private static void Load16(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<ushort>(ctx, memoryAddress);

            // Destination is always a register
            AssemblerRegister64 dest64 = ctx.GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister16 dest16 = Reg64ToReg16(dest64);

            // use movbe to swap bytes
            ctx.Asm.movbe(dest16, src);

            // zero extend to 64-bits
            ctx.Asm.movzx(dest64, dest16);
        }

        private static void Store8(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<byte>(ctx, memoryAddress);

            if (ctx.TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                ctx.Asm.mov(dest, (byte)value);
            }
            else
            {
                // Value is in a register 
                AssemblerRegister64 valueReg = ctx.GetLocation(src).GetRegister();
                ctx.Asm.mov(dest, Reg64ToReg8(valueReg));
            }
        }

        private static void Load8(X64Context ctx, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<byte>(ctx, memoryAddress);

            // Destination is always a register
            // use movzx zero extend
            ctx.Asm.movzx(ctx.GetLocation(instruction.Destination).GetRegister(), src);
        }
    }
}