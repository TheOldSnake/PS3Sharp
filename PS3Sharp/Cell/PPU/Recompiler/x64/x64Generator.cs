using Iced.Intel;
using PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator;
using PS3Sharp.Memory;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.PPUThread;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal unsafe sealed partial class X64Generator
    {
        private X64GeneratorState _currentState = null!;
        private readonly X64RegisterAllocator _registerAllocator = new();

        // Helpers
        private bool IsVolatile(AssemblerRegister64 reg) => _registerAllocator.IsVolatile(reg);

        private bool IsNonVolatile(AssemblerRegister64 reg) => _registerAllocator.IsNonVolatile(reg);

        private bool IsConst(Value value) => _currentState.KnownConstants.ContainsKey(value);

        private bool TryGetConst(Value value, out ulong result) => _currentState.KnownConstants.TryGetValue(value, out result);

        private Location GetLocation(Value value) => _currentState.RegisterMap[value];

        private void PrintNativeCode(Assembler asm)
        {
            MemoryStream stream = new();
            asm.Assemble(new StreamCodeWriter(stream), 0);

            stream.Position = 0;
            StreamCodeReader reader = new(stream);
            Decoder decoder = Decoder.Create(64, reader);
            decoder.IP = 0;

            while (stream.Position < stream.Length)
            {
                decoder.Decode(out Instruction instr);
                ulong ip = instr.IP;
                Console.WriteLine($"{instr}");
            }
        }

        private Span<byte> AssembleBlock()
        {
            MemoryStream stream = new();
            AssemblerResult result = _currentState.Asm.Assemble(new StreamCodeWriter(stream), 0);
            byte[] buffer = stream.GetBuffer();
            return buffer.AsSpan().Slice(0, (int)stream.Position);
        }

        private X64GeneratorState CreateState(IRBlock block)
        {
            Dictionary<Value, Location> registerMap = _registerAllocator.AllocateHostRegisters(block.IRCode, block.Constants);
            List<AssemblerRegister64> borrowPool = _registerAllocator.CreateRegisterPool(block.IRCode);
            Dictionary<Value, ulong> constants = block.Constants;
            AssemblerRegister64 state = _registerAllocator.PPUStatePointer;
            AssemblerRegister64 stack = _registerAllocator.StackPointer;
            AssemblerRegister64 spillArray = _registerAllocator.SpillArrayPointer;
            AssemblerRegister64 memoryBase = _registerAllocator.MemoryBasePointer;
            return new(borrowPool, registerMap, constants, state, stack, spillArray, memoryBase);
        }

        internal Span<byte> GenerateNativeCode(IRBlock block, PPUThread ppuThread)
        {
            _currentState = CreateState(block);

            int stackSlots = GetRequiredStackSlots();
            List<AssemblerRegister64> allocatedNonVolatileRegisters = GetNonVolatileMappedRegisters();

            bool hasCalls = HasCalls(block.IRCode);
            bool hasMemoryOp = HasMemoryOp(block.IRCode);

            EmitBlockEntry(stackSlots, allocatedNonVolatileRegisters, hasMemoryOp, ppuThread.MemoryManager);

            for (int i = 0; i < block.IRCode.Count; i++)
            {
                IRToX64(block.IRCode[i]);
            }

            EmitBlockExit(allocatedNonVolatileRegisters, stackSlots, hasMemoryOp);

            return AssembleBlock();
        }

        private AssemblerRegister64 BorrowRegister(Assembler asm)
        {
            ImmutableList<AssemblerRegister64> pool = _currentState.BorrowPool;

            for (int i = 0; i < pool.Count; i++)
            {
                AssemblerRegister64 reg = pool[i];

                // Borrow a register that was not borrowed before and is not reserved by the current instruction
                if (!_currentState.IsBorrowedRegister(reg) && !_currentState.IsReservedRegister(reg))
                {
                    _currentState.AddBorrowedRegister(reg);
                    asm.push(reg);

                    _currentState.CurrentStackOffset += 8;
                    return reg;
                }
            }


            // If we fail this means all registers are corrently borrowed,
            // something must gone very wrong
            throw new Exception($"[x64 Generator] Too many borrows");
        }

        private void ReturnBorrowedRegister(AssemblerRegister64 tmp)
        {
            _currentState.RemoveBorrowedRegister(tmp);
            _currentState.Asm.pop(tmp);

            _currentState.CurrentStackOffset -= 8;
        }

        private void EmitBlockEntry(int spillSlotCount, List<AssemblerRegister64> nonVolatileRegs, bool hasMemoryOp, MemoryManager memoryManager)
        {

            // Size of spill array
            int spillBytes = spillSlotCount * 8;

            // Stack bytes to push 
            int pushedBytes = 8 * (2 + nonVolatileRegs.Count);

            if (IsNonVolatile(_currentState.PPUState))
            {
                _currentState.Asm.push(_currentState.PPUState);
            }

            if (IsNonVolatile(_currentState.SpillArray))
            {
                _currentState.Asm.push(_currentState.SpillArray);
            }

            if (hasMemoryOp)
            {

                if (IsNonVolatile(_currentState.MemoryBase))
                {
                    _currentState.Asm.push(_currentState.MemoryBase);
                }

                UIntPtr addr = (UIntPtr)memoryManager.GetGuestMemoryBase();
                _currentState.Asm.mov(_currentState.MemoryBase, addr);
            }

            foreach (AssemblerRegister64 reg in nonVolatileRegs)
            {
                _currentState.Asm.push(reg);
            }

            if (spillBytes > 0)
            {
                _currentState.Asm.sub(_currentState.Stack, spillBytes);                   // Allocate array for spill slots
                _currentState.Asm.mov(_currentState.SpillArray, _currentState.Stack);     // SpillArray Points at array[0]
            }

            // Move ppu state pointer argument from rcx 
            _currentState.Asm.mov(_currentState.PPUState, rcx);

            // Current offset includes the 8 bytes for the return address
            _currentState.CurrentStackOffset = 8 + pushedBytes + spillBytes;
        }

        private void EmitBlockExit(List<AssemblerRegister64> nonVolatileRegs, int spillSlotCount, bool hasMemoryOp)
        {
            // Reverse everything

            // Deallocate spill slots
            int spillBytes = spillSlotCount * 8;

            if (spillBytes > 0)
            {
                _currentState.Asm.add(_currentState.Stack, spillBytes);
            }

            // Pop pushed registers in reverse order
            for (int i = nonVolatileRegs.Count - 1; i >= 0; i--)
            {
                _currentState.Asm.pop(nonVolatileRegs[i]);
            }

            if (hasMemoryOp)
            {
                if (IsNonVolatile(_currentState.MemoryBase))
                {
                    _currentState.Asm.pop(_currentState.MemoryBase);
                }
            }

            if (IsNonVolatile(_currentState.SpillArray))
            {
                _currentState.Asm.pop(_currentState.SpillArray);
            }

            if (IsNonVolatile(_currentState.PPUState))
            {
                _currentState.Asm.pop(_currentState.PPUState);
            }

            _currentState.Asm.ret();
        }

        private int GetRequiredStackSlots()
        {
            ImmutableDictionary<Value, Location> registerMap = _currentState.RegisterMap;
            int lastIndex = -1;

            foreach (KeyValuePair<Value, Location> item in registerMap)
            {
                Location location = item.Value;
                if (location.IsSpilled)
                {
                    lastIndex = Math.Max(lastIndex, location.SpillSlot);
                }
            }

            return lastIndex + 1;
        }

        private List<AssemblerRegister64> GetNonVolatileMappedRegisters()
        {
            ImmutableDictionary<Value, Location> registerMap = _currentState.RegisterMap;
            List<AssemblerRegister64> regs = new();

            foreach (KeyValuePair<Value, Location> item in registerMap)
            {
                Location location = item.Value;

                if (!location.Value.IsValid)
                {
                    continue;
                }

                // Check if the register needs to be saved
                if (!location.IsSpilled && IsVolatile(location.GetRegister()))
                {

                    // Ensure that we did not save already
                    if (!regs.Contains(location.GetRegister()))
                    {
                        regs.Add(location.GetRegister());
                    }
                }
            }

            return regs;
        }

        private int GetPCOffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.PC));
            return offset;
        }

        private int GetLROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.LR));
            return offset;
        }

        private int GetCROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.CR));
            return offset;
        }

        private int GetCTROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.CTR));
            return offset;
        }

        private int GetXEROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.XER));
            return offset;
        }

        private int GetGPROffset(int index)
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.GPR)) + (index * Unsafe.SizeOf<PPUGPR>());
            return offset;
        }

        private int SpillOffset(Location location)
        {
            return location.SpillSlot * 8;
        }

        private bool ValueIsRegister(Value value)
        {
            // Returns true if value is a valid non constant
            if (!value.IsValid)
            {
                return false;
            }

            if (IsConst(value))
            {
                return false;
            }

            return true;
        }

        private void LoadOperands(IRInstruction instruction)
        {
            // Ensure that all valid operands are present in hardware registers
            // This is slightly inefficient, but it simplifies the handlers a lot

            Value[] values = [instruction.Destination, instruction.Operand1, instruction.Operand2, instruction.Operand3];
            values = values.Distinct().ToArray();

            // First pass reserves permenant registers (so they don't get borrowed)
            Location location;

            foreach (Value value in values)
            {
                if (ValueIsRegister(value))
                {
                    location = GetLocation(value);

                    if (!location.IsSpilled)
                    {
                        ReserveRegister(location);
                        MarkActiveVolatile(location);
                    }
                }
            }

            // Second pass loads spilled registers from stack
            foreach (Value value in values)
            {
                if (ValueIsRegister(value))
                {
                    location = GetLocation(value);

                    if (location.IsSpilled)
                    {
                        LoadOperand(location);
                    }
                }
            }
        }

        private void ReserveRegister(Location location)
        {
            AssemblerRegister64 reg = location.GetRegister();
            _currentState.AddReservedRegister(reg);
        }

        private void MarkActiveVolatile(Location location)
        {
            // permenant mapped volatile register need to be added to this list
            // to be able to save them before calls

            AssemblerRegister64 reg = location.GetRegister();

            if (IsVolatile(reg))
            {
                _currentState.AddInUseVolatileRegister(reg);
            }
        }

        private void LoadOperand(Location location)
        {
            AssemblerRegister64 temp = BorrowRegister(_currentState.Asm);
            _currentState.Asm.mov(temp, __qword_ptr[rbp + SpillOffset(location)]);
            location.AssignRegister(temp);
        }

        private void FreeOperands(IRInstruction instruction)
        {
            Value[] values = [instruction.Destination, instruction.Operand1, instruction.Operand2, instruction.Operand3];

            // Reverse the order
            values = values.Distinct().Reverse().ToArray();

            // One pass is enough here
            foreach (Value value in values)
            {
                if (ValueIsRegister(value))
                {
                    Location location = GetLocation(value);

                    if (location.IsSpilled)
                    {
                        WriteBackOperand(location);
                    }
                }
            }

            UnreserveAllRegisters();
        }

        private void UnreserveAllRegisters()
        {
            _currentState.ClearReservedRegister();
        }

        private void WriteBackOperand(Location location)
        {
            // Write back
            AssemblerRegister64 temp = location.GetRegister();
            _currentState.Asm.mov(__qword_ptr[rbp + SpillOffset(location)], temp);

            // Return the register
            ReturnBorrowedRegister(temp);
            location.UnassignRegister();
        }

        private void PrepareForCall(AssemblerRegister64 skippedRegister)
        {
            // Note, on Windows x64:

            /*
                rcx — 1st argument
                rdx — 2nd argument
                r8 — 3rd argument
                r9 — 4th argument
            */

            // Push all currently active volatile registers
            ReadOnlySpan<AssemblerRegister64> inUseVolatileRegisters = _currentState.GetInUseVolatileRegisters();

            for (int i = 0; i < inUseVolatileRegisters.Length; i++)
            {
                AssemblerRegister64 register = inUseVolatileRegisters[i];
                if (register != skippedRegister)
                {
                    _currentState.Asm.push(register);
                    _currentState.CurrentStackOffset += 8;
                }
            }

            // todo push borrowed registers if volatile

            // todo: align stack and shadow space
        }

        private void ReturnFromCall(AssemblerRegister64 skippedRegister)
        {
            // todo: undo shadow space first

            // Then:
            // Pop all currently active volatile registers in reverse 
            ReadOnlySpan<AssemblerRegister64> inUseVolatileRegisters = _currentState.GetInUseVolatileRegisters();

            for (int i = inUseVolatileRegisters.Length - 1; i >= 0; i--)
            {
                AssemblerRegister64 register = inUseVolatileRegisters[i];
                if (register != skippedRegister)
                {
                    _currentState.Asm.pop(register);
                    _currentState.CurrentStackOffset -= 8;
                }
            }
        }

        private void IRToX64(IRInstruction instruction)
        {
            if (instruction.Opcode == IRInstruction.Op.Ret)
            {
                return;
            }

            LoadOperands(instruction);

            switch (instruction.Opcode)
            {
                case IRInstruction.Op.PCStore:
                    PCStore(instruction);
                    break;

                case IRInstruction.Op.LRLoad:
                    LRLoad(instruction);
                    break;

                case IRInstruction.Op.LRStore:
                    LRStore(instruction);
                    break;

                case IRInstruction.Op.CRLoad:
                    CRLoad(instruction);
                    break;

                case IRInstruction.Op.CRStore:
                    CRStore(instruction);
                    break;

                case IRInstruction.Op.CTRLoad:
                    CTRLoad(instruction);
                    break;

                case IRInstruction.Op.CTRStore:
                    CTRStore(instruction);
                    break;

                case IRInstruction.Op.XERLoad:
                    XERLoad(instruction);
                    break;

                case IRInstruction.Op.XERStore:
                    XERStore(instruction);
                    break;

                case IRInstruction.Op.GPRLoad:
                    GPRLoad(instruction);
                    break;

                case IRInstruction.Op.GPRStore:
                    GPRStore(instruction);
                    break;

                case IRInstruction.Op.Add:
                case IRInstruction.Op.And:
                case IRInstruction.Op.Or:
                case IRInstruction.Op.Xor:
                    CommutativeOp(instruction);
                    break;

                case IRInstruction.Op.Not:
                    Not(instruction);
                    break;

                case IRInstruction.Op.Sub:
                    Sub(instruction);
                    break;


                case IRInstruction.Op.IMulLD:
                    IMulLD(instruction);
                    break;

                case IRInstruction.Op.IMulHD:
                    IMulHD(instruction);
                    break;

                case IRInstruction.Op.MulHD:
                    MulHD(instruction);
                    break;

                case IRInstruction.Op.IMulLW:
                    IMulLW(instruction);
                    break;

                case IRInstruction.Op.IMulHW:
                    IMulHW(instruction);
                    break;

                case IRInstruction.Op.MulHW:
                    MulHW(instruction);
                    break;

                case IRInstruction.Op.CheckedDivD:
                    CheckedDivD(instruction);
                    break;

                case IRInstruction.Op.CheckedIDivD:
                    CheckedIDivD(instruction);
                    break;

                case IRInstruction.Op.CheckedDivW:
                    CheckedDivW(instruction);
                    break;

                case IRInstruction.Op.CheckedIDivW:
                    CheckedIDivW(instruction);
                    break;

                case IRInstruction.Op.ShiftLeft:
                    ShiftLeft(instruction);
                    break;

                case IRInstruction.Op.ShiftRight:
                    ShiftRight(instruction, arithmetic: false);
                    break;

                case IRInstruction.Op.ShiftRightArithmetic:
                    ShiftRight(instruction, arithmetic: true);
                    break;

                case IRInstruction.Op.RotateLeft:
                    RotateLeft(instruction);
                    break;

                case IRInstruction.Op.RotateRight:
                    RotateRight(instruction);
                    break;

                case IRInstruction.Op.Load64:
                    Load64(instruction);
                    break;

                case IRInstruction.Op.Store64:
                    Store64(instruction);
                    break;

                case IRInstruction.Op.Load32:
                    Load32(instruction);
                    break;

                case IRInstruction.Op.Store32:
                    Store32(instruction);
                    break;

                case IRInstruction.Op.Load16:
                    Load16(instruction);
                    break;

                case IRInstruction.Op.Store16:
                    Store16(instruction);
                    break;

                case IRInstruction.Op.Load8:
                    Load8(instruction);
                    break;

                case IRInstruction.Op.Store8:
                    Store8(instruction);
                    break;

                case IRInstruction.Op.Select:
                    Select(instruction);
                    break;

                case IRInstruction.Op.SetEq:
                    SetEq(instruction);
                    break;

                case IRInstruction.Op.SetNotEq:
                    SetNotEq(instruction);
                    break;

                case IRInstruction.Op.SetLessThanSigned:
                    SetLessThan(instruction, signed: true);
                    break;

                case IRInstruction.Op.SetGreaterThanSigned:
                    SetGreaterThan(instruction, signed: true);
                    break;

                case IRInstruction.Op.SetLessThanUnsigned:
                    SetLessThan(instruction, signed: false);
                    break;

                case IRInstruction.Op.SetGreaterThanUnsigned:
                    SetGreaterThan(instruction, signed: false);
                    break;

                case IRInstruction.Op.SetLessThanEqSigned:
                    SetLessThanEq(instruction, signed: true);
                    break;

                case IRInstruction.Op.SetGreaterThanEqSigned:
                    SetGreaterThanEq(instruction, signed: true);
                    break;

                case IRInstruction.Op.SetLessThanEqUnsigned:
                    SetLessThanEq(instruction, signed: false);
                    break;

                case IRInstruction.Op.SetGreaterThanEqUnsigned:
                    SetGreaterThanEq(instruction, signed: false);
                    break;

                case IRInstruction.Op.ZeroExtend:
                    ZeroExtend(instruction);
                    break;

                case IRInstruction.Op.SignExtend:
                    SignExtend(instruction);
                    break;

                case IRInstruction.Op.Truncate:
                    Truncate(instruction);
                    break;

                default:
                    throw new Exception($"TODO: IR [{instruction.Opcode}] -> x64");
            }

            FreeOperands(instruction);
        }

        private bool ImmediateRequiresRegister(ulong imm)
        {
            ulong high = imm >> 32;
            return (high != 0 && high != 0xFFFFFFFF);
        }
    }
}
