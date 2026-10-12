using Iced.Intel;
using System.Buffers.Binary;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal unsafe sealed partial class X64Generator
    {
        private void PCStore(IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetPCOffset();

            // PC is 32-bit
            if (TryGetConst(src, out ulong value))
            {
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private void LRLoad(IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetLROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // LR is 32-bit
            _currentState.Asm.mov(reg, __dword_ptr[PPUState + offset]);
        }

        private void LRStore(IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetLROffset();

            // LR is 32-bit
            if (TryGetConst(src, out ulong value))
            {
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private void CRLoad(IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetCROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // CR is 32-bit
            _currentState.Asm.mov(reg, __dword_ptr[PPUState + offset]);
        }

        private void CRStore(IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetCROffset();

            // CR is 32-bit
            if (TryGetConst(src, out ulong value))
            {
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private void CTRLoad(IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetCTROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // CTR is 64-bit
            _currentState.Asm.mov(reg, __qword_ptr[PPUState + offset]);
        }

        private void CTRStore(IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetCTROffset();

            // CTR is 64-bit
            if (TryGetConst(src, out ulong value))
            {
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], (uint)value);
                _currentState.Asm.mov(__dword_ptr[PPUState + (offset + 4)], (uint)(value >> 32));
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                _currentState.Asm.mov(__qword_ptr[PPUState + offset], reg);
            }
        }

        private void XERLoad(IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetXEROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // XER is 64-bit
            _currentState.Asm.mov(reg, __qword_ptr[PPUState + offset]);
        }

        private void XERStore(IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetXEROffset();

            // XER is 64-bit
            if (TryGetConst(src, out ulong value))
            {
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], (uint)value);
                _currentState.Asm.mov(__dword_ptr[PPUState + (offset + 4)], (uint)(value >> 32));
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                _currentState.Asm.mov(__qword_ptr[PPUState + offset], reg);
            }
        }

        private void GPRLoad(IRInstruction instruction)
        {
            int gpr = (int)instruction.Immediate1;
            int gprOffset = GetGPROffset(gpr);

            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            AssemblerRegister64 reg = dest.GetRegister();
            _currentState.Asm.mov(reg, __qword_ptr[PPUState + gprOffset]);
        }

        private void GPRStore(IRInstruction instruction)
        {
            int gpr = (int)instruction.Immediate1;
            int offset = GetGPROffset(gpr);

            Value src = instruction.Destination;

            // GPR is 64-bit
            if (TryGetConst(src, out ulong value))
            {
                _currentState.Asm.mov(__dword_ptr[PPUState + offset], (uint)value);
                _currentState.Asm.mov(__dword_ptr[PPUState + (offset + 4)], (uint)(value >> 32));
            }
            else
            {
                AssemblerRegister64 reg = GetLocation(src).GetRegister();
                _currentState.Asm.mov(__qword_ptr[PPUState + offset], reg);
            }
        }

        internal AssemblerMemoryOperand GetPointer<T>(Value memoryAddress, uint offset = 0) where T : unmanaged
        {
            if (TryGetConst(memoryAddress, out ulong virtualAddress))
            {
                return GetPointerImmOffset<T>((uint)virtualAddress, offset);
            }
            else
            {
                AssemblerRegister64 virtualAddressReg = GetLocation(memoryAddress).GetRegister();
                return GetPointerRegOffset<T>(virtualAddressReg, offset);
            }
        }

        internal AssemblerMemoryOperand GetPointerImmOffset<T>(uint baseOffset, uint extraOffset = 0) where T : unmanaged
        {
            int size = sizeof(T);
            uint finalOffset = baseOffset + extraOffset;

            return size switch
            {
                1 => __byte_ptr[MemoryBase + finalOffset],
                2 => __word_ptr[MemoryBase + finalOffset],
                4 => __dword_ptr[MemoryBase + finalOffset],
                8 => __qword_ptr[MemoryBase + finalOffset],
                _ => throw new Exception($"[x64 Generator] Invalid pointer size = {size}"),
            };
        }

        internal AssemblerMemoryOperand GetPointerRegOffset<T>(AssemblerRegister64 baseOffset, uint extraOffset = 0) where T : unmanaged
        {
            int size = sizeof(T);

            return size switch
            {
                1 => __byte_ptr[MemoryBase + baseOffset + extraOffset],
                2 => __word_ptr[MemoryBase + baseOffset + extraOffset],
                4 => __dword_ptr[MemoryBase + baseOffset + extraOffset],
                8 => __qword_ptr[MemoryBase + baseOffset + extraOffset],
                _ => throw new Exception($"[x64 Generator] Invalid pointer size = {size}"),
            };
        }

        private void Store64(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand destPtrQ = GetPointer<ulong>(memoryAddress);
            AssemblerMemoryOperand destPtrD0 = GetPointer<uint>(memoryAddress, offset: 0);
            AssemblerMemoryOperand destPtrD1 = GetPointer<uint>(memoryAddress, offset: 4);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate 64-bit value - convert to BE in place before writing
                ulong swapped = BinaryPrimitives.ReverseEndianness(value);
                _currentState.Asm.mov(destPtrD0, (uint)swapped);
                _currentState.Asm.mov(destPtrD1, (uint)(swapped >> 32));
            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                _currentState.Asm.movbe(destPtrQ, valueReg);
            }
        }

        private void Load64(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<ulong>(memoryAddress);

            // Destination is always a register
            // use movbe to swap bytes
            _currentState.Asm.movbe(GetLocation(instruction.Destination).GetRegister(), src);
        }

        private void Store32(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<uint>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                uint swapped = BinaryPrimitives.ReverseEndianness((uint)value);
                _currentState.Asm.mov(dest, swapped);
            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                _currentState.Asm.movbe(dest, Reg64ToReg32(valueReg));
            }
        }

        private void Load32(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<uint>(memoryAddress);

            // Destination is always a register
            AssemblerRegister64 dest64 = GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister32 dest32 = Reg64ToReg32(dest64);

            // use movbe to swap bytes
            // automatically zero extends to 64-bits

            _currentState.Asm.movbe(dest32, src);
        }

        private void Store16(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<ushort>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                ushort swapped = BinaryPrimitives.ReverseEndianness((ushort)value);
                _currentState.Asm.mov(dest, swapped);
            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                _currentState.Asm.movbe(dest, Reg64ToReg16(valueReg));
            }
        }

        private void Load16(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<ushort>(memoryAddress);

            // Destination is always a register
            AssemblerRegister64 dest64 = GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister16 dest16 = Reg64ToReg16(dest64);

            // use movbe to swap bytes
            _currentState.Asm.movbe(dest16, src);

            // zero extend to 64-bits
            _currentState.Asm.movzx(dest64, dest16);
        }

        private void Store8(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<byte>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                _currentState.Asm.mov(dest, (byte)value);
            }
            else
            {
                // Value is in a register 
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                _currentState.Asm.mov(dest, Reg64ToReg8(valueReg));
            }
        }

        private void Load8(IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<byte>(memoryAddress);

            // Destination is always a register
            // use movzx zero extend
            _currentState.Asm.movzx(GetLocation(instruction.Destination).GetRegister(), src);
        }
    }
}

