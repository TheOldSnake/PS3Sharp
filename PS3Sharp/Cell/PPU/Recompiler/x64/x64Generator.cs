using Iced.Intel;
using PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator;
using PS3Sharp.Memory;
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
        private class X64GeneratorState
        {
            internal List<AssemblerRegister64> BorrowPool { get; }                    // List of all borrowable registers for this block, based on the allocator
            internal List<AssemblerRegister64> InUseVolatileRegisters { get; } = [];  // Currently active volatile registers (across the block)
            internal Stack<AssemblerRegister64> BorrowedRegisters { get; } = [];      // Currently borrowed registers by the current instruction operands/handler
            internal List<AssemblerRegister64> ReservedRegisters { get; } = [];       // Currently reserved registers by the current instruction permenant registers mapping
            internal Dictionary<Value, Location> RegisterMap { get; }                 // Current register mapping
            internal Dictionary<Value, ulong> KnownConstants { get; }                 // Current constants dictionary                                        
            internal int CurrentStackOffset { get; set; }                             // Current stack offset in bytes
            internal Assembler Asm { get; }                                           // Current assembler

            internal X64GeneratorState(List<AssemblerRegister64> borrowPool, Dictionary<Value, Location> registerMap, Dictionary<Value, ulong> constants)
            {
                BorrowPool = borrowPool;
                RegisterMap = registerMap;
                KnownConstants = constants;
                Asm = new(64);
            }
        }

        private X64GeneratorState _currentState = null!;
        private X64RegisterAllocator _registerAllocator = new();

        // Helpers
        private Assembler Asm => _currentState.Asm;
        private List<AssemblerRegister64> BorrowPool => _currentState.BorrowPool;
        private List<AssemblerRegister64> InUseVolatileRegisters => _currentState.InUseVolatileRegisters;
        private Stack<AssemblerRegister64> BorrowedRegisters => _currentState.BorrowedRegisters;
        private List<AssemblerRegister64> ReservedRegisters => _currentState.ReservedRegisters;
        private Dictionary<Value, Location> RegisterMap => _currentState.RegisterMap;
        private Dictionary<Value, ulong> KnownConstants => _currentState.KnownConstants;

        private AssemblerRegister64 PPUState => _registerAllocator.PPUStatePointer;
        private AssemblerRegister64 Stack => _registerAllocator.StackPointer;
        private AssemblerRegister64 SpillArray => _registerAllocator.SpillArrayPointer;
        private AssemblerRegister64 MemoryBase => _registerAllocator.MemoryBasePointer;

        private bool IsVolatile(AssemblerRegister64 reg) => _registerAllocator.IsVolatile(reg);

        private bool IsNonVolatile(AssemblerRegister64 reg) => _registerAllocator.IsNonVolatile(reg);

        private bool IsConst(Value value) => KnownConstants.ContainsKey(value);

        private bool TryGetConst(Value value, out ulong result) => KnownConstants.TryGetValue(value, out result);

        private Location GetLocation(Value value) => RegisterMap[value];

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
            AssemblerResult result = Asm.Assemble(new StreamCodeWriter(stream), 0);
            byte[] buffer = stream.GetBuffer();
            return buffer.AsSpan().Slice(0, (int)stream.Position);
        }

        private void SetupState(IRBlock block)
        {
            Dictionary<Value, Location> registerMap = _registerAllocator.AllocateHostRegisters(block.IRCode, block.Constants);
            List<AssemblerRegister64> borrowPool = _registerAllocator.CreateRegisterPool(block.IRCode);
            Dictionary<Value, ulong> constants = block.Constants;
            _currentState = new(borrowPool, registerMap, constants);
        }

        internal Span<byte> GenerateNativeCode(IRBlock block, PPUThread ppuThread)
        {
            SetupState(block);

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
            for (int i = 0; i < BorrowPool.Count; i++)
            {
                AssemblerRegister64 reg = BorrowPool[i];

                // Borrow a register that was not borrowed before and is not reserved by the current instruction
                if (!BorrowedRegisters.Contains(reg) && !ReservedRegisters.Contains(reg))
                {
                    BorrowedRegisters.Push(reg);
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
            // Ensure that the register was borrowed and that is is the most recent borrow
            bool validReturn = BorrowedRegisters.Contains(tmp) && BorrowedRegisters.Peek() == tmp;

            if (!validReturn)
            {
                throw new Exception($"[x64 Generator] Invalid return for register: {tmp.Value}");
            }

            BorrowedRegisters.Pop();
            Asm.pop(tmp);

            _currentState.CurrentStackOffset -= 8;
        }

        private void EmitBlockEntry(int spillSlotCount, List<AssemblerRegister64> nonVolatileRegs, bool hasMemoryOp, MemoryManager memoryManager)
        {

            // Size of spill array
            int spillBytes = spillSlotCount * 8;

            // Stack bytes to push 
            int pushedBytes = 8 * (2 + nonVolatileRegs.Count);

            if (IsNonVolatile(PPUState))
            {
                Asm.push(PPUState);
            }

            if (IsNonVolatile(SpillArray))
            {
                Asm.push(SpillArray);
            }

            if (hasMemoryOp)
            {

                if (IsNonVolatile(MemoryBase))
                {
                    Asm.push(MemoryBase);
                }

                UIntPtr addr = (UIntPtr)memoryManager.GetGuestMemoryBase();
                Asm.mov(MemoryBase, addr);
            }

            foreach (AssemblerRegister64 reg in nonVolatileRegs)
            {
                Asm.push(reg);
            }

            if (spillBytes > 0)
            {
                Asm.sub(Stack, spillBytes);     // Allocate array for spill slots
                Asm.mov(SpillArray, Stack);     // SpillArray Points at array[0]
            }

            // Move ppu state pointer argument from rcx 
            Asm.mov(PPUState, rcx);

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
                Asm.add(Stack, spillBytes);
            }

            // Pop pushed registers in reverse order
            for (int i = nonVolatileRegs.Count - 1; i >= 0; i--)
            {
                Asm.pop(nonVolatileRegs[i]);
            }

            if (hasMemoryOp)
            {
                if (IsNonVolatile(MemoryBase))
                {
                    Asm.pop(MemoryBase);
                }
            }

            if (IsNonVolatile(SpillArray))
            {
                Asm.pop(SpillArray);
            }

            if (IsNonVolatile(PPUState))
            {
                Asm.pop(PPUState);
            }

            Asm.ret();
        }

        private int GetRequiredStackSlots()
        {
            int lastIndex = -1;

            foreach (KeyValuePair<Value, Location> item in RegisterMap)
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
            List<AssemblerRegister64> regs = new();

            foreach (KeyValuePair<Value, Location> item in RegisterMap)
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
            if (!ReservedRegisters.Contains(reg))
            {
                ReservedRegisters.Add(reg);
            }
        }

        private void MarkActiveVolatile(Location location)
        {
            // permenant mapped volatile register need to be added to this list
            // to be able to save them before calls

            AssemblerRegister64 reg = location.GetRegister();

            if (IsVolatile(reg))
            {

                if (!InUseVolatileRegisters.Contains(reg))
                {
                    InUseVolatileRegisters.Add(reg);
                }
            }
        }

        private void LoadOperand(Location location)
        {
            AssemblerRegister64 temp = BorrowRegister(Asm);
            Asm.mov(temp, __qword_ptr[rbp + SpillOffset(location)]);
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
            ReservedRegisters.Clear();
        }

        private void WriteBackOperand(Location location)
        {
            // Write back
            AssemblerRegister64 temp = location.GetRegister();
            Asm.mov(__qword_ptr[rbp + SpillOffset(location)], temp);

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

            for (int i = 0; i < InUseVolatileRegisters.Count; i++)
            {
                if (InUseVolatileRegisters[i] != skippedRegister)
                {
                    Asm.push(InUseVolatileRegisters[i]);
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

            for (int i = InUseVolatileRegisters.Count - 1; i >= 0; i--)
            {

                if (InUseVolatileRegisters[i] != skippedRegister)
                {
                    Asm.pop(InUseVolatileRegisters[i]);
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


        // unused

        private bool ImmediateRequiresRegister(ulong imm)
        {
            ulong high = imm >> 32;
            return (high != 0 && high != 0xFFFFFFFF);
        }

        /*   private static void MovLocation(AssemblerRegister64 destReg, Location location, Dictionary<Value, ulong> knownConstants) {
               bool isConst = knownConstants.ContainsKey(location.Value);

               if (isConst) {
                   asm.mov(destReg, knownConstants[location.Value]);

               } else {
                   if (!location.IsSpilled) {
                       asm.mov(destReg, location.Register!.Value);

                   } else {
                       int spillOffset = SpillOffset(location);
                       asm.mov(destReg, __qword_ptr[rbp + spillOffset]);
                   }
               }
           }

           private static void AddLocation(AssemblerRegister64 destReg, Location location, Dictionary<Value, ulong> knownConstants) {
               bool isConst = knownConstants.ContainsKey(location.Value);

               if (isConst) {
                   AddConst(destReg, knownConstants[location.Value]);

               } else {
                   if (!location.IsSpilled) {
                       asm.add(destReg, location.Register!.Value);

                   } else {
                       int spillOffset = SpillOffset(location);
                       asm.add(destReg, __qword_ptr[rbp + spillOffset]);
                   }
               }
           }

           private static void AddConst(AssemblerRegister64 destReg, ulong constant) {
               if (ImmediateRequiresRegister(constant)) {
                   (AssemblerRegister64 temp, bool preserve) = BorrowRegister();

                   if (preserve) {
                       asm.push(temp);
                   }

                   asm.mov(temp, constant);
                   asm.add(destReg, temp);

                   if (preserve) {
                       asm.pop(temp);
                   }

               } else {
                   // Safe to cast
                   asm.add(destReg, (int)constant);
               }
           }*/

    }
}
