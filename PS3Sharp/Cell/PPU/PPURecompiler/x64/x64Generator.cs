using Iced.Intel;
using PS3Sharp.Memory_Manager;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.PPU;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;
using static PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64 {
    public static unsafe partial class x64Generator {
        private static int _currentStackOffset = 0;                                // Current stack offset in bytes
        private static List<AssemblerRegister64> _staticBorrowPool = new();        // List of all borrowable registers for this block
        private static Stack<AssemblerRegister64> _borrowedRegisters = new();      // Currently borrowed registers by the current instruction operands/handler
        private static List<AssemblerRegister64> _reservedRegisters = new();       // Currently reserved registers by the current instruction permenant registers mapping
        private static List<AssemblerRegister64> _inUseVolatileRegisters = new();  // Currently active volatile registers (across the block)

        private static Dictionary<Value, Location> _registerMap;              // Current register mapping
        private static Dictionary<Value, ulong> _knownConstants;              // Current constants dictionary


        private static AssemblerRegister32 Reg64ToReg32(AssemblerRegister64 reg) {
            switch (reg.Value) {
                case Register.RAX: return eax;
                case Register.RCX: return ecx;
                case Register.RDX: return edx;
                case Register.RSI: return esi;
                case Register.RDI: return edi;

               // case Register.R8: 

                case Register.R9: return r9d;
                case Register.R10: return r10d;
                case Register.R11: return r11d;
                case Register.R12: return r12d;
                case Register.R13: return r13d;
                case Register.R14: return r14d;
                case Register.R15: return r15d;

                default: throw new Exception($"[x64 Generator] Invalid register: {reg.Value}");
            }
        }

        private static AssemblerRegister8 Reg64ToReg8(AssemblerRegister64 reg) {
            switch (reg.Value) {
                case Register.RAX: return al;
                case Register.RCX: return cl;
                case Register.RDX: return dl;
                case Register.RBX: return bl;
                case Register.RSI: return sil;
                case Register.RDI: return dil;
                case Register.RBP: return bpl;
                case Register.RSP: return spl;

                // case Register.R8: 

                case Register.R9: return r9b;
                case Register.R10: return r10b;
                case Register.R11: return r11b;
                case Register.R12: return r12b;
                case Register.R13: return r13b;
                case Register.R14: return r14b;
                case Register.R15: return r15b;

                default: throw new Exception($"[x64 Generator] Invalid register: {reg.Value}");
            }
        }

        private static AssemblerRegister16 Reg64ToReg16(AssemblerRegister64 reg) {
            switch (reg.Value) {
                case Register.RAX: return ax;
                case Register.RCX: return cx;
                case Register.RDX: return dx;
                case Register.RBX: return bx;
                case Register.RSI: return si;
                case Register.RDI: return di;
                case Register.RBP: return bp;
                case Register.RSP: return sp;

                // case Register.R8: 

                case Register.R8: return r8w;
                case Register.R9: return r9w;
                case Register.R10: return r10w;
                case Register.R11: return r11w;
                case Register.R12: return r12w;
                case Register.R13: return r13w;
                case Register.R14: return r14w;
                case Register.R15: return r15w;

                default: throw new Exception($"[x64 Generator] Invalid register: {reg.Value}");
            }
        }



        private static AssemblerRegister64 Reg32ToReg64(AssemblerRegister32 reg) {
            switch (reg.Value) {
                case Register.EAX: return rax;
                case Register.ECX: return rcx;
                case Register.EDX: return rdx;
                case Register.ESI: return rsi;
                case Register.EDI: return rdi;

                // case Register.R8D: 

                case Register.R9D: return r9;
                case Register.R10D: return r10;
                case Register.R11D: return r11;
                case Register.R12D: return r12;
                case Register.R13D: return r13;
                case Register.R14D: return r14;
                case Register.R15D: return r15;
                default: throw new ArgumentOutOfRangeException(nameof(reg), reg, null);
            }
        }


        private static void PrintNativeCode(Assembler asm) {
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

        private static Span<byte> AssembleBlock(Assembler asm) {
            var stream = new MemoryStream();
            var result = asm.Assemble(new StreamCodeWriter(stream), 0);
            return stream.GetBuffer().AsSpan();
        } 

        public static Span<byte> GenerateNativeCode(IRBlock block, PPU ppuThread) {
            Reset();

            _registerMap = RegisterAllocator.AllocateHostRegisters(block.IRCode, block.Constants);
            _staticBorrowPool = RegisterAllocator.CreateRegisterPool(block.IRCode);
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

            EmitBlockExit(asm, allocatedNonVolatileRegisters, stackSlots);

            PrintNativeCode(asm);

            return AssembleBlock(asm);
        }

        private static void Reset() {
            _currentStackOffset = 0;
            _borrowedRegisters.Clear();
            _inUseVolatileRegisters.Clear();
            _staticBorrowPool.Clear();
        }

        private static int Padding16(int offset) {
            // Returns the padding needed to be 16-byte aligned
            return (16 - (offset & 15)) & 15;
        }


        private static AssemblerRegister64 BorrowRegister(Assembler asm) {
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

        private static void ReturnBorrowedRegister(Assembler asm, AssemblerRegister64 tmp) {
            // Ensure that the register was borrowed and that is is the most recent borrow
            bool validReturn = _borrowedRegisters.Contains(tmp) && _borrowedRegisters.Peek() == tmp;

            if (!validReturn) {
                throw new Exception($"[x64 Generator] Invalid return for register: {tmp.Value}");
            }

            _borrowedRegisters.Pop();
            asm.pop(tmp);

            _currentStackOffset -= 8;
        }

        private static void EmitBlockEntry(Assembler asm, int spillSlotCount, void* ppuState, List<AssemblerRegister64> nonVolatileRegs, bool hasMemoryOp) {
            int spillBytes = spillSlotCount * 8;

            // Stack bytes to push 
            int pushedBytes = 8 * (2 + nonVolatileRegs.Count);

            // Padding needed so RSP is 16-byte aligned
            // This padding includes pushes and subtracts 
            // int padding = (16 - ((currentOffset + extraBytes) & 15)) & 15;

            // No need to preserve R8

            asm.push(rbx);
            asm.push(rbp);

            foreach (var reg in nonVolatileRegs) {
                asm.push(reg);
            }

            if (spillBytes > 0) {
                asm.sub(rsp, spillBytes);       // Allocate array for spill slots
                asm.mov(rbp, rsp);              // RBP Points at array[0]
            }

            asm.mov(rbx, (UIntPtr)ppuState);

            if (hasMemoryOp) {
                asm.mov(RegisterAllocator.MemoryBaseRegister, (UIntPtr)MemoryManager.GetGuestMemoryBase());
            }

            // Current offset includes the 8 bytes for the return address
            _currentStackOffset = 8 + pushedBytes + spillBytes;
        }

        private static void EmitBlockExit(Assembler asm, List<AssemblerRegister64> nonVolatileRegs, int spillSlotCount) {
            // Reverse everything

            // Deallocate spill slots
            int spillBytes = spillSlotCount * 8;

            if (spillBytes > 0) {
                asm.add(rsp, spillBytes);
            }

            // Pop pushed registers in reverse order
            for (int i = nonVolatileRegs.Count - 1; i >= 0; i--) {
                asm.pop(nonVolatileRegs[i]);
            }

            asm.pop(rbp);
            asm.pop(rbx);

            // R8 is volatile

            asm.ret();
        }

        private static int GetRequiredStackSlots() {
            int lastIndex = -1;

            foreach (var item in _registerMap) { 
                Location location = item.Value;
                if (location.IsSpilled) {
                    lastIndex = Math.Max(lastIndex, location.SpillSlot);
                }
            }

            return lastIndex + 1;
        }

        private static bool HasCalls(List<IRInstruction> instructions) {
            for (int i = 0; i < instructions.Count; i++) {
                if (RequiresCall(instructions[i])) {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMemoryOp(List<IRInstruction> instructions) {
            for (int i = 0; i < instructions.Count; i++) {
                if (IsMemoryOp(instructions[i])) {
                    return true;
                }
            }

            return false;
        }

        private static bool RequiresCall(IRInstruction instruction) {
            bool requresCall = 
                instruction.Op == IR.IRInstructions.Op.Syscall || 
                instruction.Op == IR.IRInstructions.Op.InterpreterFallback;

            return requresCall;
        }

        private static bool IsMemoryOp(IRInstruction instruction) {
            bool isMemOp =
                instruction.Op == IR.IRInstructions.Op.Load8 ||
                instruction.Op == IR.IRInstructions.Op.Load16 ||
                instruction.Op == IR.IRInstructions.Op.Load16Rev ||
                instruction.Op == IR.IRInstructions.Op.Load32 ||
                instruction.Op == IR.IRInstructions.Op.Load32Rev ||
                instruction.Op == IR.IRInstructions.Op.Load32Rx ||
                instruction.Op == IR.IRInstructions.Op.Load64 ||
                instruction.Op == IR.IRInstructions.Op.Load64Rx ||
                instruction.Op == IR.IRInstructions.Op.Load128 ||
                instruction.Op == IR.IRInstructions.Op.Store8 ||
                instruction.Op == IR.IRInstructions.Op.Store16 ||
                instruction.Op == IR.IRInstructions.Op.Store16Rev ||
                instruction.Op == IR.IRInstructions.Op.Store32 ||
                instruction.Op == IR.IRInstructions.Op.Store32Rev ||
                instruction.Op == IR.IRInstructions.Op.Store32CX ||
                instruction.Op == IR.IRInstructions.Op.Store64 ||
                instruction.Op == IR.IRInstructions.Op.Store64CX ||
                instruction.Op == IR.IRInstructions.Op.Store128;

            return isMemOp;
        }

        private static List<AssemblerRegister64> GetNonVolatileMappedRegisters() {
            List<AssemblerRegister64> regs = new();

            foreach (var item in _registerMap) {
                Location location = item.Value;

                if (!location.Value.IsValid) {
                    continue;
                }

                // Check if the register needs to be saved
                if (!location.IsSpilled && StaticNonVolatileRegisterPool64.Contains(location.GetRegister())) {

                    // Ensure that we did not save already
                    if (!regs.Contains(location.GetRegister())) {
                        regs.Add(location.GetRegister());
                    }
                }
            }

            return regs;
        }

     
        private static int GetPCOffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.PC));
            return offset;
        }

        private static int GetLROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.LR));
            return offset;
        }

        private static int GetCROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.CR));
            return offset;
        }

        private static int GetCTROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.CTR));
            return offset;
        }

        private static int GetXEROffset() {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.XER));
            return offset;
        }

        private static int GetGPROffset(int index) {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUState.GPR)) + (index * Unsafe.SizeOf<PPUGPR>());
            return offset;
        }

        private static int SpillOffset(Location location) {
            return location.SpillSlot * 8;
        }

       
        private static bool ValueIsRegister(Value value) {
            // Returns true if value is a valid non constant
            if (!value.IsValid) {
                return false;
            }

            if (_knownConstants.ContainsKey(value)) {
                return false;
            }

            return true;
        }
       
        private static void LoadOperands(Assembler asm, IRInstruction instruction) {
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

        private static void ReserveRegister(Assembler asm, Location location) {
            AssemblerRegister64 reg = location.GetRegister();
            if (!_reservedRegisters.Contains(reg)) {
                _reservedRegisters.Add(reg);
            }
        }

        private static void MarkActiveVolatile(Assembler asm, Location location) {
            // permenant mapped volatile register need to be added to this list
            // to be able to save them before calls

            AssemblerRegister64 reg = location.GetRegister();

            if (StaticVolatileRegisterPool64.Contains(reg)) {

                if (!_inUseVolatileRegisters.Contains(reg)) {
                    _inUseVolatileRegisters.Add(reg);
                }
            }
        }

        private static void LoadOperand(Assembler asm, Location location) {
            AssemblerRegister64 temp = BorrowRegister(asm);
            asm.mov(temp, __qword_ptr[rbp + SpillOffset(location)]);
            location.AssignRegister(temp);
        }

        private static void FreeOperands(Assembler asm, IRInstruction instruction) {
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

        private static void UnreserveAllRegisters() {
            _reservedRegisters.Clear();
        }

        private static void WriteBackOperand(Assembler asm, Location location) {
            // Write back
            AssemblerRegister64 temp = location.GetRegister();
            asm.mov(__qword_ptr[rbp + SpillOffset(location)], temp);

            // Return the register
            ReturnBorrowedRegister(asm, temp);
            location.UnassignRegister();
        }

        private static void PrepareForCall(Assembler asm, AssemblerRegister64 skippedRegister) {
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

        private static void ReturnFromCall(Assembler asm, AssemblerRegister64 skippedRegister) {
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

        private static void IRToX64(Assembler asm, IRInstruction instruction) {
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

        private static bool ImmediateRequiresRegister(ulong imm) {
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
