using Iced.Intel;
using System.Buffers.Binary;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal unsafe sealed partial class X64Generator
    {
        private void MemoryWriteImm64(Assembler asm, AssemblerMemoryOperand addr, ulong value, bool isBE)
        {
            if (isBE)
            {
                if (value != 0)
                {
                    BorrowAndWrite();

                }
                else
                {
                    asm.mov(addr, 0);
                }

            }
            else
            {
                if (ImmediateRequiresRegister(value))
                {
                    BorrowAndWrite();

                }
                else
                {
                    asm.mov(addr, (int)value);
                }
            }

            void BorrowAndWrite()
            {
                AssemblerRegister64 reg = BorrowRegister(asm);
                asm.mov(reg, value);
                asm.mov(addr, reg);
                ReturnBorrowedRegister(asm, reg);
            }
        }

        private void PCStore(Assembler asm, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetPCOffset();

            // PC is 32-bit
            if (TryGetConst(src, out ulong value))
            {
                asm.mov(__dword_ptr[PPUState + offset], (uint)value);
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                asm.mov(__dword_ptr[PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private void LRLoad(Assembler asm, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetLROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // LR is 32-bit
            asm.mov(reg, __dword_ptr[PPUState + offset]);
        }

        private void LRStore(Assembler asm, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetLROffset();

            // LR is 32-bit
            if (TryGetConst(src, out ulong value))
            {
                asm.mov(__dword_ptr[PPUState + offset], (uint)value);

            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                asm.mov(__dword_ptr[PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private void CRLoad(Assembler asm, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetCROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // CR is 32-bit
            asm.mov(reg, __dword_ptr[PPUState + offset]);
        }

        private void CRStore(Assembler asm, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetCROffset();

            // CR is 32-bit
            if (TryGetConst(src, out ulong value))
            {
                asm.mov(__dword_ptr[PPUState + offset], (uint)value);

            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                asm.mov(__dword_ptr[PPUState + offset], Reg64ToReg32(reg));
            }
        }

        private void CTRLoad(Assembler asm, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetCTROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // CTR is 64-bit
            asm.mov(reg, __qword_ptr[PPUState + offset]);
        }

        private void CTRStore(Assembler asm, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetCTROffset();

            // CTR is 64-bit
            if (TryGetConst(src, out ulong value))
            {
                MemoryWriteImm64(asm, __qword_ptr[PPUState + offset], value, isBE: false);
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                asm.mov(__dword_ptr[PPUState + offset], reg);
            }
        }

        private void XERLoad(Assembler asm, IRInstruction instruction)
        {
            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            int offset = GetXEROffset();
            AssemblerRegister64 reg = dest.GetRegister();

            // XER is 64-bit
            asm.mov(reg, __qword_ptr[PPUState + offset]);
        }

        private void XERStore(Assembler asm, IRInstruction instruction)
        {
            Value src = instruction.Destination;
            int offset = GetXEROffset();

            // XER is 64-bit
            if (TryGetConst(src, out ulong value))
            {
                MemoryWriteImm64(asm, __qword_ptr[PPUState + offset], value, isBE: false);
            }
            else
            {
                Location dest = GetLocation(instruction.Destination);
                AssemblerRegister64 reg = dest.GetRegister();
                asm.mov(__dword_ptr[PPUState + offset], reg);
            }
        }

        private void GPRLoad(Assembler asm, IRInstruction instruction)
        {
            int gpr = (int)instruction.Immediate1;
            int gprOffset = GetGPROffset(gpr);

            // Destination is always a register
            Location dest = GetLocation(instruction.Destination);
            AssemblerRegister64 reg = dest.GetRegister();
            asm.mov(reg, __qword_ptr[PPUState + gprOffset]);
        }

        private void GPRStore(Assembler asm, IRInstruction instruction)
        {
            int gpr = (int)instruction.Immediate1;
            int gprOffset = GetGPROffset(gpr);

            Value src = instruction.Destination;

            if (TryGetConst(src, out ulong value))
            {
                MemoryWriteImm64(asm, __qword_ptr[PPUState + gprOffset], value, isBE: false);

            }
            else
            {
                AssemblerRegister64 reg = GetLocation(src).GetRegister();
                asm.mov(__qword_ptr[PPUState + gprOffset], reg);
            }
        }

        internal AssemblerMemoryOperand GetPointer<T>(Value memoryAddress) where T : unmanaged
        {
            if (TryGetConst(memoryAddress, out ulong virtualAddress))
            {
                return GetPointerImmOffset<T>((uint)virtualAddress);

            }
            else
            {
                AssemblerRegister64 virtualAddressReg = GetLocation(memoryAddress).GetRegister();
                return GetPointerRegOffset<T>(virtualAddressReg);
            }
        }

        internal AssemblerMemoryOperand GetPointerImmOffset<T>(uint offset) where T : unmanaged
        {
            int size = sizeof(T);

            return size switch
            {
                1 => __byte_ptr[MemoryBase + offset],
                2 => __word_ptr[MemoryBase + offset],
                4 => __dword_ptr[MemoryBase + offset],
                8 => __qword_ptr[MemoryBase + offset],
                _ => throw new Exception($"[x64 Generator] Invalid pointer size = {size}"),
            };
        }

        internal AssemblerMemoryOperand GetPointerRegOffset<T>(AssemblerRegister64 offset) where T : unmanaged
        {
            int size = sizeof(T);

            return size switch
            {
                1 => __byte_ptr[MemoryBase + offset],
                2 => __word_ptr[MemoryBase + offset],
                4 => __dword_ptr[MemoryBase + offset],
                8 => __qword_ptr[MemoryBase + offset],
                _ => throw new Exception($"[x64 Generator] Invalid pointer size = {size}"),
            };
        }

        private void Store64(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<ulong>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                ulong swapped = BinaryPrimitives.ReverseEndianness(value);
                MemoryWriteImm64(asm, dest, swapped, isBE: true);

            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                asm.movbe(dest, valueReg);
            }
        }

        private void Load64(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<ulong>(memoryAddress);

            // Destination is always a register
            // use movbe to swap bytes
            asm.movbe(GetLocation(instruction.Destination).GetRegister(), src);
        }

        private void Store32(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<uint>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                uint swapped = BinaryPrimitives.ReverseEndianness((uint)value);
                asm.mov(dest, swapped);

            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                asm.movbe(dest, Reg64ToReg32(valueReg));
            }
        }

        private void Load32(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<uint>(memoryAddress);

            // Destination is always a register
            AssemblerRegister64 dest64 = GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister32 dest32 = Reg64ToReg32(dest64);

            // use movbe to swap bytes
            // automatically zero extends to 64-bits

            asm.movbe(dest32, src);
        }

        private void Store16(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<ushort>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                // convert to BE in place
                ushort swapped = BinaryPrimitives.ReverseEndianness((ushort)value);
                asm.mov(dest, swapped);

            }
            else
            {
                // Value is in a register 
                // use movbe to swap bytes
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                asm.movbe(dest, Reg64ToReg16(valueReg));
            }
        }

        private void Load16(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<ushort>(memoryAddress);

            // Destination is always a register
            AssemblerRegister64 dest64 = GetLocation(instruction.Destination).GetRegister();
            AssemblerRegister16 dest16 = Reg64ToReg16(dest64);

            // use movbe to swap bytes
            asm.movbe(dest16, src);

            // zero extend to 64-bits
            asm.movzx(dest64, dest16);
        }

        private void Store8(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Destination;
            Value src = instruction.Operand1;

            AssemblerMemoryOperand dest = GetPointer<byte>(memoryAddress);

            if (TryGetConst(instruction.Operand1, out ulong value))
            {
                // Immediate value
                asm.mov(dest, (byte)value);

            }
            else
            {
                // Value is in a register 
                AssemblerRegister64 valueReg = GetLocation(src).GetRegister();
                asm.mov(dest, Reg64ToReg8(valueReg));
            }
        }

        private void Load8(Assembler asm, IRInstruction instruction)
        {
            Value memoryAddress = instruction.Operand1;
            AssemblerMemoryOperand src = GetPointer<byte>(memoryAddress);

            // Destination is always a register
            // use movzx zero extend
            asm.movzx(GetLocation(instruction.Destination).GetRegister(), src);
        }
    }

    //internal void ComputeAddress(Assembler asm, AssemblerRegister64 addressReg, Value memoryAddress) {
    //    if (_knownConstants.TryGetValue(memoryAddress, out ulong virtualAddress)) {
    //        TranslateAddress(asm, addressReg, (uint)virtualAddress);
    //    } else {
    //        AssemblerRegister64 virtualAddressReg = _registerMap[memoryAddress].GetRegister();
    //        TranslateAddress(asm, addressReg, virtualAddressReg);
    //    }
    //}


    //internal void TranslateAddress(Assembler asm, AssemblerRegister64 destination, uint address) {
    //    UIntPtr pageTable = (UIntPtr)MemoryManager.GetPageTable();
    //    uint page = address >> (int)Fastmem.PAGE_SHIFT;
    //    uint offset = address & (int)Fastmem.PAGE_MASK;
    //    //ulong pageStart = pageTable + (ulong)(page * UIntPtr.Size);

    //    // We fetch the page at runtime
    //    asm.mov(destination, __qword_ptr[r8 + (page * 8)]);
    //    asm.add(destination, (int)offset);
    //}

    //internal void TranslateAddress(Assembler asm, AssemblerRegister64 destination, AssemblerRegister64 address) {
    //    // Save the original register as it's mapped to a guest register
    //    asm.push(address);

    //    // Copy 
    //    asm.mov(destination, address);

    //    // page number
    //    asm.shr(destination, (byte)Fastmem.PAGE_SHIFT);

    //    // page start pointer (r8 points to page table)
    //    asm.mov(destination, __qword_ptr[r8 + (destination * 8)]);

    //    // offset
    //    asm.and(address, (int)Fastmem.PAGE_MASK);

    //    // final address
    //    asm.add(destination, address);

    //    // Restore original register
    //    asm.pop(address);
    //}
}

