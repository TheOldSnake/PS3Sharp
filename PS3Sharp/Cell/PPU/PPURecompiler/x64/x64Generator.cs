using Iced.Intel;
using PS3Sharp.Memory_Manager;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.PPU;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;
using static PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator.RegisterAllocator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64 {
    public unsafe partial class X64Generator {
        private int _currentStackOffset = 0;                                // Current stack offset in bytes
        private List<AssemblerRegister64> _staticBorrowPool = new();        // List of all borrowable registers for this block
        private Stack<AssemblerRegister64> _borrowedRegisters = new();      // Currently borrowed registers by the current instruction operands/handler
        private List<AssemblerRegister64> _reservedRegisters = new();       // Currently reserved registers by the current instruction permenant registers mapping
        private List<AssemblerRegister64> _inUseVolatileRegisters = new();  // Currently active volatile registers (across the block)

        private Dictionary<Value, Location> _registerMap;              // Current register mapping
        private Dictionary<Value, ulong> _knownConstants;              // Current constants dictionary

        private RegisterAllocator.RegisterAllocator _registerAllocator = new();

        private void PrintNativeCode(Assembler asm) {
            var stream = new MemoryStream();
            asm.Assemble(new StreamCodeWriter(stream), 0);

            stream.Position = 0;
            var reader = new StreamCodeReader(stream);
            var decoder = Decoder.Create(64, reader);
            decoder.IP = 0;

            while (stream.Position < stream.Length) {
                decoder.Decode(out var instr);
                ulong ip = instr.IP;
                Console.WriteLine($"{instr}");
            }
        }

        private Span<byte> AssembleBlock(Assembler asm) {
            var stream = new MemoryStream();
            var result = asm.Assemble(new StreamCodeWriter(stream), 0);
            var buffer = stream.GetBuffer();
            return buffer.AsSpan().Slice(0, (int)stream.Position);
        } 

        public Span<byte> GenerateNativeCode(IRBlock block, PPU ppuThread) {
            Reset();

            _registerMap = _registerAllocator.AllocateHostRegisters(block.IRCode, block.Constants);
            _staticBorrowPool = _registerAllocator.CreateRegisterPool(block.IRCode);
            _knownConstants = block.Constants;
            
            Assembler asm = new Assembler(64);
            
            void* state = ppuThread.State;
            int stackSlots = GetRequiredStackSlots();
            List<AssemblerRegister64> allocatedNonVolatileRegisters = GetNonVolatileMappedRegisters();
           
            bool hasCalls = HasCalls(block.IRCode);
            bool hasMemoryOp = HasMemoryOp(block.IRCode);

            EmitBlockEntry(asm, stackSlots, state, allocatedNonVolatileRegisters, hasMemoryOp);

            for (int i = 0; i < block.IRCode.Count; i++) {
                IRToX64(asm, block.IRCode[i]);
            }

            EmitBlockExit(asm, allocatedNonVolatileRegisters, stackSlots, hasMemoryOp);

            return AssembleBlock(asm);
        }

        private void Reset() {
            _currentStackOffset = 0;
            _borrowedRegisters.Clear();
            _inUseVolatileRegisters.Clear();
            _staticBorrowPool.Clear();
            _registerMap = null;
            _knownConstants = null;
        }

        private AssemblerRegister64 BorrowRegister(Assembler asm) {
            for (int i = 0; i < _staticBorrowPool.Count; i++) {
                AssemblerRegister64 reg = _staticBorrowPool[i];

                // Borrow a register that was not borrowed before and is not reserved by the current instruction
                if (!_borrowedRegisters.Contains(reg) && !_reservedRegisters.Contains(reg)) {
                    _borrowedRegisters.Push(reg);
                    asm.push(reg);

                    _currentStackOffset += 8;
                    return reg;
                }
            }


            // If we fail this means all registers are corrently borrowed,
            // something must gone very wrong
            throw new Exception($"[x64 Generator] Too many borrows");
        }

        private void ReturnBorrowedRegister(Assembler asm, AssemblerRegister64 tmp) {
            // Ensure that the register was borrowed and that is is the most recent borrow
            bool validReturn = _borrowedRegisters.Contains(tmp) && _borrowedRegisters.Peek() == tmp;

            if (!validReturn) {
                throw new Exception($"[x64 Generator] Invalid return for register: {tmp.Value}");
            }

            _borrowedRegisters.Pop();
            asm.pop(tmp);

            _currentStackOffset -= 8;
        }

        private void EmitBlockEntry(Assembler asm, int spillSlotCount, void* ppuState, List<AssemblerRegister64> nonVolatileRegs, bool hasMemoryOp) {
            AssemblerRegister64 state = _registerAllocator.PPUStatePointer;
            AssemblerRegister64 spill = _registerAllocator.SpillArrayPointer;
            AssemblerRegister64 memory = _registerAllocator.MemoryBasePointer;
            AssemblerRegister64 stack = _registerAllocator.StackPointer;

            // Size of spill array
            int spillBytes = spillSlotCount * 8;

            // Stack bytes to push 
            int pushedBytes = 8 * (2 + nonVolatileRegs.Count);

            if (_registerAllocator.IsNonVolatile(state)) {
                asm.push(state);
            }

            if (_registerAllocator.IsNonVolatile(spill)) {
                asm.push(spill);
            }

            if (hasMemoryOp) {

                if (_registerAllocator.IsNonVolatile(memory)) {
                    asm.push(memory);
                }

                UIntPtr addr = (UIntPtr)MemoryManager.GetGuestMemoryBase();
                asm.mov(memory, addr);
            }

            foreach (var reg in nonVolatileRegs) {
                asm.push(reg);
            }

            if (spillBytes > 0) {
                asm.sub(stack, spillBytes);                     // Allocate array for spill slots
                asm.mov(spill, stack);                          // SpillArrayPointer Points at array[0]
            }

            asm.mov(state, (UIntPtr)ppuState);

            // Current offset includes the 8 bytes for the return address
            _currentStackOffset = 8 + pushedBytes + spillBytes;
        }

        private void EmitBlockExit(Assembler asm, List<AssemblerRegister64> nonVolatileRegs, int spillSlotCount, bool hasMemoryOp) {
            // Reverse everything

            AssemblerRegister64 state = _registerAllocator.PPUStatePointer;
            AssemblerRegister64 spill = _registerAllocator.SpillArrayPointer;
            AssemblerRegister64 memory = _registerAllocator.MemoryBasePointer;
            AssemblerRegister64 stack = _registerAllocator.StackPointer;

            // Deallocate spill slots
            int spillBytes = spillSlotCount * 8;

            if (spillBytes > 0) {
                asm.add(stack, spillBytes);
            }

            // Pop pushed registers in reverse order
            for (int i = nonVolatileRegs.Count - 1; i >= 0; i--) {
                asm.pop(nonVolatileRegs[i]);
            }

            if (hasMemoryOp) {
                if (_registerAllocator.IsNonVolatile(memory)) {
                    asm.pop(memory);
                }
            }

            if (_registerAllocator.IsNonVolatile(spill)) {
                asm.pop(spill);
            }

            if (_registerAllocator.IsNonVolatile(state)) {
                asm.pop(state);
            }

            asm.ret();
        }

        private int GetRequiredStackSlots() {
            int lastIndex = -1;

            foreach (var item in _registerMap) { 
                Location location = item.Value;
                if (location.IsSpilled) {
                    lastIndex = Math.Max(lastIndex, location.SpillSlot);
                }
            }

            return lastIndex + 1;
        }

        private List<AssemblerRegister64> GetNonVolatileMappedRegisters() {
            List<AssemblerRegister64> regs = new();

            foreach (var item in _registerMap) {
                Location location = item.Value;

                if (!location.Value.IsValid) {
                    continue;
                }

                // Check if the register needs to be saved
                if (!location.IsSpilled && _registerAllocator.IsVolatile(location.GetRegister())) {

                    // Ensure that we did not save already
                    if (!regs.Contains(location.GetRegister())) {
                        regs.Add(location.GetRegister());
                    }
                }
            }

            return regs;
        }

     
        private int GetPCOffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.PC));
            return offset;
        }

        private int GetLROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.LR));
            return offset;
        }

        private int GetCROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.CR));
            return offset;
        }

        private int GetCTROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.CTR));
            return offset;
        }

        private int GetXEROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.XER));
            return offset;
        }

        private int GetGPROffset(int index) {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.GPR)) + (index * Unsafe.SizeOf<PPUGPR>());
            return offset;
        }

        private int SpillOffset(Location location) {
            return location.SpillSlot * 8;
        }

       
        private bool ValueIsRegister(Value value) {
            // Returns true if value is a valid non constant
            if (!value.IsValid) {
                return false;
            }

            if (_knownConstants.ContainsKey(value)) {
                return false;
            }

            return true;
        }
       
        private void LoadOperands(Assembler asm, IRInstruction instruction) {
            // Ensure that all valid operands are present in hardware registers
            // This is slightly inefficient, but it simplifies the handlers a lot

            Value[] values = [instruction.Destination, instruction.Operand1, instruction.Operand2, instruction.Operand3];
            values = values.Distinct().ToArray();

            // First pass reserves permenant registers (so they don't get borrowed)
            Location location;

            foreach (Value value in values) {
                if (ValueIsRegister(value)) {
                    location = _registerMap[value];

                    if (!location.IsSpilled) {
                        ReserveRegister(asm, _registerMap[value]);
                        MarkActiveVolatile(asm, _registerMap[value]);   
                    }
                }
            }

            // Second pass loads spilled registers from stack
            foreach (Value value in values) {
                if (ValueIsRegister(value)) {
                    location = _registerMap[value];

                    if (location.IsSpilled) {
                        LoadOperand(asm, _registerMap[value]);
                    }
                }
            }
        }

        private void ReserveRegister(Assembler asm, Location location) {
            AssemblerRegister64 reg = location.GetRegister();
            if (!_reservedRegisters.Contains(reg)) {
                _reservedRegisters.Add(reg);
            }
        }

        private void MarkActiveVolatile(Assembler asm, Location location) {
            // permenant mapped volatile register need to be added to this list
            // to be able to save them before calls

            AssemblerRegister64 reg = location.GetRegister();

            if (_registerAllocator.IsVolatile(reg)) {

                if (!_inUseVolatileRegisters.Contains(reg)) {
                    _inUseVolatileRegisters.Add(reg);
                }
            }
        }

        private void LoadOperand(Assembler asm, Location location) {
            AssemblerRegister64 temp = BorrowRegister(asm);
            asm.mov(temp, __qword_ptr[rbp + SpillOffset(location)]);
            location.AssignRegister(temp);
        }

        private void FreeOperands(Assembler asm, IRInstruction instruction) {
            Value[] values = [instruction.Destination, instruction.Operand1, instruction.Operand2, instruction.Operand3];

            // Reverse the order
            values = values.Distinct().Reverse().ToArray();

            // One pass is enough here
            foreach (Value value in values) {
                if (ValueIsRegister(value)) {
                    Location loc = _registerMap[value];

                    if (loc.IsSpilled) {
                        WriteBackOperand(asm, _registerMap[value]);
                    } 
                }
            }

            UnreserveAllRegisters();
        }

        private void UnreserveAllRegisters() {
            _reservedRegisters.Clear();
        }

        private void WriteBackOperand(Assembler asm, Location location) {
            // Write back
            AssemblerRegister64 temp = location.GetRegister();
            asm.mov(__qword_ptr[rbp + SpillOffset(location)], temp);

            // Return the register
            ReturnBorrowedRegister(asm, temp);
            location.UnassignRegister();
        }

        private void PrepareForCall(Assembler asm, AssemblerRegister64 skippedRegister) {
            // Note, on Windows x64:

            /*
                rcx — 1st argument
                rdx — 2nd argument
                r8 — 3rd argument
                r9 — 4th argument
            */

            // Push all currently active volatile registers

            for (int i = 0; i < _inUseVolatileRegisters.Count; i++) {
                if (_inUseVolatileRegisters[i] != skippedRegister) {
                    asm.push(_inUseVolatileRegisters[i]);
                    _currentStackOffset += 8;
                }
            }

            // todo push borrowed registers if volatile

            // todo: align stack and shadow space
        }

        private void ReturnFromCall(Assembler asm, AssemblerRegister64 skippedRegister) {
            // todo: undo shadow space first

            // Then:
            // Pop all currently active volatile registers in reverse 

            for (int i = _inUseVolatileRegisters.Count - 1; i >= 0; i--) {

                if (_inUseVolatileRegisters[i] != skippedRegister) {
                    asm.pop(_inUseVolatileRegisters[i]);
                    _currentStackOffset -= 8;
                }
            }
        }

        private void IRToX64(Assembler asm, IRInstruction instruction) {
            if (instruction.Op == IR.IRInstructions.Op.Ret) {
                return;
            }

            LoadOperands(asm, instruction);
          
            switch (instruction.Op) {
                case IR.IRInstructions.Op.PCStore:
                    PCStore(asm, instruction);
                    break;

                case IR.IRInstructions.Op.LRLoad:
                    LRLoad(asm, instruction);
                    break;

                case IR.IRInstructions.Op.LRStore:
                    LRStore(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CRLoad:
                    CRLoad(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CRStore:
                    CRStore(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CTRLoad:
                    CTRLoad(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CTRStore:
                    CTRStore(asm, instruction);
                    break;

                case IR.IRInstructions.Op.XERLoad:
                    XERLoad(asm, instruction);
                    break;

                case IR.IRInstructions.Op.XERStore:
                    XERStore(asm, instruction);
                    break;

                case IR.IRInstructions.Op.GPRLoad: 
                    GPRLoad(asm, instruction);
                    break;

                case IR.IRInstructions.Op.GPRStore:
                    GPRStore(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Add:
                case IR.IRInstructions.Op.And:
                case IR.IRInstructions.Op.Or:
                case IR.IRInstructions.Op.Xor:
                    CommutativeOp(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Not:
                    Not(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Sub:
                    Sub(asm, instruction);
                    break;


                case IR.IRInstructions.Op.IMulLD:
                    IMulLD(asm, instruction);
                    break;

                case IR.IRInstructions.Op.IMulHD:
                    IMulHD(asm, instruction);
                    break;

                case IR.IRInstructions.Op.MulHD:
                    MulHD(asm, instruction);
                    break;

                case IR.IRInstructions.Op.IMulLW:
                    IMulLW(asm, instruction);
                    break;

                case IR.IRInstructions.Op.IMulHW:
                    IMulHW(asm, instruction);
                    break;

                case IR.IRInstructions.Op.MulHW:
                    MulHW(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CheckedDivD:
                    CheckedDivD(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CheckedIDivD:
                    CheckedIDivD(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CheckedDivW:
                    CheckedDivW(asm, instruction);
                    break;

                case IR.IRInstructions.Op.CheckedIDivW:
                    CheckedIDivW(asm, instruction);
                    break;

                case IR.IRInstructions.Op.ShiftLeft:
                    ShiftLeft(asm, instruction);
                    break;

                case IR.IRInstructions.Op.ShiftRight:
                    ShiftRight(asm, instruction, arithmetic: false);
                    break;

                case IR.IRInstructions.Op.ShiftRightArithmetic:
                    ShiftRight(asm, instruction, arithmetic: true);
                    break;

                case IR.IRInstructions.Op.RotateLeft:
                    RotateLeft(asm, instruction);
                    break;

                case IR.IRInstructions.Op.RotateRight:
                    RotateRight(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Load64:
                    Load64(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Store64:
                    Store64(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Load32:
                    Load32(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Store32:
                    Store32(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Load16:
                    Load16(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Store16:
                    Store16(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Load8:
                    Load8(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Store8:
                    Store8(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Select:
                    Select(asm, instruction);
                    break;

                case IR.IRInstructions.Op.SetEq:
                    SetEq(asm, instruction);
                    break;

                case IR.IRInstructions.Op.SetNotEq:
                    SetNotEq(asm, instruction);
                    break;

                case IR.IRInstructions.Op.SetLessThanSigned:
                    SetLessThan(asm, instruction, signed: true);
                    break;

                case IR.IRInstructions.Op.SetGreaterThanSigned:
                    SetGreaterThan(asm, instruction, signed: true);
                    break;

                case IR.IRInstructions.Op.SetLessThanUnsigned:
                    SetLessThan(asm, instruction, signed: false);
                    break;

                case IR.IRInstructions.Op.SetGreaterThanUnsigned:
                    SetGreaterThan(asm, instruction, signed: false);
                    break;

                case IR.IRInstructions.Op.SetLessThanEqSigned:
                    SetLessThanEq(asm, instruction, signed: true);
                    break;

                case IR.IRInstructions.Op.SetGreaterThanEqSigned:
                    SetGreaterThanEq(asm, instruction, signed: true);
                    break;

                case IR.IRInstructions.Op.SetLessThanEqUnsigned:
                    SetLessThanEq(asm, instruction, signed: false);
                    break;

                case IR.IRInstructions.Op.SetGreaterThanEqUnsigned:
                    SetGreaterThanEq(asm, instruction, signed: false);
                    break;

                case IR.IRInstructions.Op.ZeroExtend:
                    ZeroExtend(asm, instruction);
                    break;

                case IR.IRInstructions.Op.SignExtend:
                    SignExtend(asm, instruction);
                    break;

                case IR.IRInstructions.Op.Truncate:
                    Truncate(asm, instruction);
                    break;
    
                default:
                    throw new Exception($"TODO: IR [{instruction.Op}] -> x64");
            }

            FreeOperands(asm, instruction);
        }

  
        // unused

        private bool ImmediateRequiresRegister(ulong imm) {
            ulong high = imm >> 32;
            return (high != 0 && high != 0xFFFFFFFF);
        }

     /*   private static void MovLocation(Assembler asm, AssemblerRegister64 destReg, Location location, Dictionary<Value, ulong> knownConstants) {
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

        private static void AddLocation(Assembler asm, AssemblerRegister64 destReg, Location location, Dictionary<Value, ulong> knownConstants) {
            bool isConst = knownConstants.ContainsKey(location.Value);

            if (isConst) {
                AddConst(asm, destReg, knownConstants[location.Value]);

            } else {
                if (!location.IsSpilled) {
                    asm.add(destReg, location.Register!.Value);

                } else {
                    int spillOffset = SpillOffset(location);
                    asm.add(destReg, __qword_ptr[rbp + spillOffset]);
                }
            }
        }

        private static void AddConst(Assembler asm, AssemblerRegister64 destReg, ulong constant) {
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
