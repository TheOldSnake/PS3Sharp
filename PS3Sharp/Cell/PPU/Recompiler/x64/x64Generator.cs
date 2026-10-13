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
        private readonly X64RegisterAllocator _registerAllocator = new();

        internal Span<byte> GenerateNativeCode(IRBlock block, PPUThread ppuThread)
        {
            X64Context ctx = CreateContext(block);

            int stackSlots = GetRequiredStackSlots(ctx);
            List<AssemblerRegister64> allocatedNonVolatileRegisters = GetNonVolatileMappedRegisters(ctx);

            bool hasCalls = HasCalls(block.IRCode);
            bool hasMemoryOp = HasMemoryOp(block.IRCode);

            EmitBlockEntry(ctx, stackSlots, allocatedNonVolatileRegisters, hasMemoryOp, ppuThread.MemoryManager);

            for (int i = 0; i < block.IRCode.Count; i++)
            {
                IRToX64(ctx, block.IRCode[i]);
            }

            EmitBlockExit(ctx, allocatedNonVolatileRegisters, stackSlots, hasMemoryOp);

            return AssembleBlock(ctx);
        }

        private static Span<byte> AssembleBlock(X64Context ctx)
        {
            MemoryStream stream = new();
            AssemblerResult result = ctx.Asm.Assemble(new StreamCodeWriter(stream), 0);
            byte[] buffer = stream.GetBuffer();
            return buffer.AsSpan().Slice(0, (int)stream.Position);
        }

        private X64Context CreateContext(IRBlock block)
        {
            Dictionary<Value, Location> registerMap = _registerAllocator.AllocateHostRegisters(block.IRCode, block.Constants);
            List<AssemblerRegister64> borrowPool = _registerAllocator.CreateRegisterPool(block.IRCode);
            Dictionary<Value, ulong> constants = block.Constants;
            AssemblerRegister64 ctx = PPUStatePointer;
            AssemblerRegister64 stack = StackPointer;
            AssemblerRegister64 spillArray = SpillArrayPointer;
            AssemblerRegister64 memoryBase = MemoryBasePointer;
            return new(borrowPool, registerMap, constants, ctx, stack, spillArray, memoryBase);
        }

        private static AssemblerRegister64 BorrowRegister(X64Context ctx)
        {
            ImmutableList<AssemblerRegister64> pool = ctx.BorrowPool;

            for (int i = 0; i < pool.Count; i++)
            {
                AssemblerRegister64 reg = pool[i];

                // Borrow a register that was not borrowed before and is not reserved by the current instruction
                if (!ctx.IsBorrowedRegister(reg) && !ctx.IsReservedRegister(reg))
                {
                    ctx.AddBorrowedRegister(reg);
                    ctx.Asm.push(reg);

                    ctx.CurrentStackOffset += 8;
                    return reg;
                }
            }

            // If we fail this means all registers are corrently borrowed,
            // something must gone very wrong
            throw new Exception($"[x64 Generator] Too many borrows");
        }

        private static void ReturnBorrowedRegister(X64Context ctx, AssemblerRegister64 tmp)
        {
            ctx.RemoveBorrowedRegister(tmp);
            ctx.Asm.pop(tmp);
            ctx.CurrentStackOffset -= 8;
        }

        private static void EmitBlockEntry(X64Context ctx, int spillSlotCount, List<AssemblerRegister64> nonVolatileRegs, bool hasMemoryOp, MemoryManager memoryManager)
        {
            // Size of spill array
            int spillBytes = spillSlotCount * 8;

            // Stack bytes to push 
            int pushedBytes = 8 * (2 + nonVolatileRegs.Count);

            if (IsNonVolatile(ctx.PPUState))
            {
                ctx.Asm.push(ctx.PPUState);
            }

            if (IsNonVolatile(ctx.SpillArray))
            {
                ctx.Asm.push(ctx.SpillArray);
            }

            if (hasMemoryOp)
            {

                if (IsNonVolatile(ctx.MemoryBase))
                {
                    ctx.Asm.push(ctx.MemoryBase);
                }

                UIntPtr addr = (UIntPtr)memoryManager.GetGuestMemoryBase();
                ctx.Asm.mov(ctx.MemoryBase, addr);
            }

            foreach (AssemblerRegister64 reg in nonVolatileRegs)
            {
                ctx.Asm.push(reg);
            }

            if (spillBytes > 0)
            {
                ctx.Asm.sub(ctx.Stack, spillBytes);                   // Allocate array for spill slots
                ctx.Asm.mov(ctx.SpillArray, ctx.Stack);             // SpillArray Points at array[0]
            }

            // Move ppu ctx pointer argument from rcx 
            ctx.Asm.mov(ctx.PPUState, rcx);

            // Current offset includes the 8 bytes for the return address
            ctx.CurrentStackOffset = 8 + pushedBytes + spillBytes;
        }

        private static void EmitBlockExit(X64Context ctx, List<AssemblerRegister64> nonVolatileRegs, int spillSlotCount, bool hasMemoryOp)
        {
            // Reverse everything

            // Deallocate spill slots
            int spillBytes = spillSlotCount * 8;

            if (spillBytes > 0)
            {
                ctx.Asm.add(ctx.Stack, spillBytes);
            }

            // Pop pushed registers in reverse order
            for (int i = nonVolatileRegs.Count - 1; i >= 0; i--)
            {
                ctx.Asm.pop(nonVolatileRegs[i]);
            }

            if (hasMemoryOp)
            {
                if (IsNonVolatile(ctx.MemoryBase))
                {
                    ctx.Asm.pop(ctx.MemoryBase);
                }
            }

            if (IsNonVolatile(ctx.SpillArray))
            {
                ctx.Asm.pop(ctx.SpillArray);
            }

            if (IsNonVolatile(ctx.PPUState))
            {
                ctx.Asm.pop(ctx.PPUState);
            }

            ctx.Asm.ret();
        }

        private static int GetRequiredStackSlots(X64Context ctx)
        {
            ImmutableDictionary<Value, Location> registerMap = ctx.RegisterMap;
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

        private static List<AssemblerRegister64> GetNonVolatileMappedRegisters(X64Context ctx)
        {
            ImmutableDictionary<Value, Location> registerMap = ctx.RegisterMap;
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

        private static int GetPCOffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.PC));
            return offset;
        }

        private static int GetLROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.LR));
            return offset;
        }

        private static int GetCROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.CR));
            return offset;
        }

        private static int GetCTROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.CTR));
            return offset;
        }

        private static int GetXEROffset()
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.XER));
            return offset;
        }

        private static int GetGPROffset(int index)
        {
            int offset = (int)Marshal.OffsetOf<PPUState>(nameof(PPUThread.PPUState.GPR)) + (index * Unsafe.SizeOf<PPUGPR>());
            return offset;
        }

        private static int SpillOffset(Location location)
        {
            return location.SpillSlot * 8;
        }

        private static bool ValueIsRegister(X64Context ctx, Value value)
        {
            // Returns true if value is a valid non constant
            if (!value.IsValid)
            {
                return false;
            }

            if (ctx.IsConst(value))
            {
                return false;
            }

            return true;
        }

        private static void LoadOperands(X64Context ctx, IRInstruction instruction)
        {
            // Ensure that all valid operands are present in hardware registers
            // This is slightly inefficient, but it simplifies the handlers a lot

            Value[] values = [instruction.Destination, instruction.Operand1, instruction.Operand2, instruction.Operand3];
            values = values.Distinct().ToArray();

            // First pass reserves permenant registers (so they don't get borrowed)
            Location location;

            foreach (Value value in values)
            {
                if (ValueIsRegister(ctx, value))
                {
                    location = ctx.GetLocation(value);

                    if (!location.IsSpilled)
                    {
                        ReserveRegister(ctx, location);
                        MarkActiveVolatile(ctx, location);
                    }
                }
            }

            // Second pass loads spilled registers from stack
            foreach (Value value in values)
            {
                if (ValueIsRegister(ctx, value))
                {
                    location = ctx.GetLocation(value);

                    if (location.IsSpilled)
                    {
                        LoadOperand(ctx, location);
                    }
                }
            }
        }

        private static void ReserveRegister(X64Context ctx, Location location)
        {
            AssemblerRegister64 reg = location.GetRegister();
            ctx.AddReservedRegister(reg);
        }

        private static void MarkActiveVolatile(X64Context ctx, Location location)
        {
            // permenant mapped volatile register need to be added to this list
            // to be able to save them before calls

            AssemblerRegister64 reg = location.GetRegister();

            if (IsVolatile(reg))
            {
                ctx.AddInUseVolatileRegister(reg);
            }
        }

        private static void LoadOperand(X64Context ctx, Location location)
        {
            AssemblerRegister64 temp = BorrowRegister(ctx);
            ctx.Asm.mov(temp, __qword_ptr[rbp + SpillOffset(location)]);
            location.AssignRegister(temp);
        }

        private static void FreeOperands(X64Context ctx, IRInstruction instruction)
        {
            Value[] values = [instruction.Destination, instruction.Operand1, instruction.Operand2, instruction.Operand3];

            // Reverse the order
            values = values.Distinct().Reverse().ToArray();

            // One pass is enough here
            foreach (Value value in values)
            {
                if (ValueIsRegister(ctx, value))
                {
                    Location location = ctx.GetLocation(value);

                    if (location.IsSpilled)
                    {
                        WriteBackOperand(ctx, location);
                    }
                }
            }

            UnreserveAllRegisters(ctx);
        }

        private static void UnreserveAllRegisters(X64Context ctx)
        {
            ctx.ClearReservedRegister();
        }

        private static void WriteBackOperand(X64Context ctx, Location location)
        {
            // Write back
            AssemblerRegister64 temp = location.GetRegister();
            ctx.Asm.mov(__qword_ptr[rbp + SpillOffset(location)], temp);

            // Return the register
            ReturnBorrowedRegister(ctx, temp);
            location.UnassignRegister();
        }

        private static void PrepareForCall(X64Context ctx, AssemblerRegister64 skippedRegister)
        {
            // Note, on Windows x64:

            /*
                rcx — 1st argument
                rdx — 2nd argument
                r8 — 3rd argument
                r9 — 4th argument
            */

            // Push all currently active volatile registers
            ReadOnlySpan<AssemblerRegister64> inUseVolatileRegisters = ctx.GetInUseVolatileRegisters();

            for (int i = 0; i < inUseVolatileRegisters.Length; i++)
            {
                AssemblerRegister64 register = inUseVolatileRegisters[i];
                if (register != skippedRegister)
                {
                    ctx.Asm.push(register);
                    ctx.CurrentStackOffset += 8;
                }
            }

            // todo push borrowed registers if volatile

            // todo: align stack and shadow space
        }

        private static void ReturnFromCall(X64Context ctx, AssemblerRegister64 skippedRegister)
        {
            // todo: undo shadow space first

            // Then:
            // Pop all currently active volatile registers in reverse 
            ReadOnlySpan<AssemblerRegister64> inUseVolatileRegisters = ctx.GetInUseVolatileRegisters();

            for (int i = inUseVolatileRegisters.Length - 1; i >= 0; i--)
            {
                AssemblerRegister64 register = inUseVolatileRegisters[i];
                if (register != skippedRegister)
                {
                    ctx.Asm.pop(register);
                    ctx.CurrentStackOffset -= 8;
                }
            }
        }

        private static void IRToX64(X64Context ctx, IRInstruction instruction)
        {
            if (instruction.Opcode == IRInstruction.Op.Ret)
            {
                return;
            }

            LoadOperands(ctx, instruction);

            switch (instruction.Opcode)
            {
                case IRInstruction.Op.PCStore:
                    PCStore(ctx, instruction);
                    break;

                case IRInstruction.Op.LRLoad:
                    LRLoad(ctx, instruction);
                    break;

                case IRInstruction.Op.LRStore:
                    LRStore(ctx, instruction);
                    break;

                case IRInstruction.Op.CRLoad:
                    CRLoad(ctx, instruction);
                    break;

                case IRInstruction.Op.CRStore:
                    CRStore(ctx, instruction);
                    break;

                case IRInstruction.Op.CTRLoad:
                    CTRLoad(ctx, instruction);
                    break;

                case IRInstruction.Op.CTRStore:
                    CTRStore(ctx, instruction);
                    break;

                case IRInstruction.Op.XERLoad:
                    XERLoad(ctx, instruction);
                    break;

                case IRInstruction.Op.XERStore:
                    XERStore(ctx, instruction);
                    break;

                case IRInstruction.Op.GPRLoad:
                    GPRLoad(ctx, instruction);
                    break;

                case IRInstruction.Op.GPRStore:
                    GPRStore(ctx, instruction);
                    break;

                case IRInstruction.Op.Add:
                case IRInstruction.Op.And:
                case IRInstruction.Op.Or:
                case IRInstruction.Op.Xor:
                    CommutativeOp(ctx, instruction);
                    break;

                case IRInstruction.Op.Not:
                    Not(ctx, instruction);
                    break;

                case IRInstruction.Op.Sub:
                    Sub(ctx, instruction);
                    break;


                case IRInstruction.Op.IMulLD:
                    IMulLD(ctx, instruction);
                    break;

                case IRInstruction.Op.IMulHD:
                    IMulHD(ctx, instruction);
                    break;

                case IRInstruction.Op.MulHD:
                    MulHD(ctx, instruction);
                    break;

                case IRInstruction.Op.IMulLW:
                    IMulLW(ctx, instruction);
                    break;

                case IRInstruction.Op.IMulHW:
                    IMulHW(ctx, instruction);
                    break;

                case IRInstruction.Op.MulHW:
                    MulHW(ctx, instruction);
                    break;

                case IRInstruction.Op.CheckedDivD:
                    CheckedDivD(ctx, instruction);
                    break;

                case IRInstruction.Op.CheckedIDivD:
                    CheckedIDivD(ctx, instruction);
                    break;

                case IRInstruction.Op.CheckedDivW:
                    CheckedDivW(ctx, instruction);
                    break;

                case IRInstruction.Op.CheckedIDivW:
                    CheckedIDivW(ctx, instruction);
                    break;

                case IRInstruction.Op.ShiftLeft:
                    ShiftLeft(ctx, instruction);
                    break;

                case IRInstruction.Op.ShiftRight:
                    ShiftRight(ctx, instruction, arithmetic: false);
                    break;

                case IRInstruction.Op.ShiftRightArithmetic:
                    ShiftRight(ctx, instruction, arithmetic: true);
                    break;

                case IRInstruction.Op.RotateLeft:
                    RotateLeft(ctx, instruction);
                    break;

                case IRInstruction.Op.RotateRight:
                    RotateRight(ctx, instruction);
                    break;

                case IRInstruction.Op.Load64:
                    Load64(ctx, instruction);
                    break;

                case IRInstruction.Op.Store64:
                    Store64(ctx, instruction);
                    break;

                case IRInstruction.Op.Load32:
                    Load32(ctx, instruction);
                    break;

                case IRInstruction.Op.Store32:
                    Store32(ctx, instruction);
                    break;

                case IRInstruction.Op.Load16:
                    Load16(ctx, instruction);
                    break;

                case IRInstruction.Op.Store16:
                    Store16(ctx, instruction);
                    break;

                case IRInstruction.Op.Load8:
                    Load8(ctx, instruction);
                    break;

                case IRInstruction.Op.Store8:
                    Store8(ctx, instruction);
                    break;

                case IRInstruction.Op.Select:
                    Select(ctx, instruction);
                    break;

                case IRInstruction.Op.SetEq:
                    SetEq(ctx, instruction);
                    break;

                case IRInstruction.Op.SetNotEq:
                    SetNotEq(ctx, instruction);
                    break;

                case IRInstruction.Op.SetLessThanSigned:
                    SetLessThan(ctx, instruction, signed: true);
                    break;

                case IRInstruction.Op.SetGreaterThanSigned:
                    SetGreaterThan(ctx, instruction, signed: true);
                    break;

                case IRInstruction.Op.SetLessThanUnsigned:
                    SetLessThan(ctx, instruction, signed: false);
                    break;

                case IRInstruction.Op.SetGreaterThanUnsigned:
                    SetGreaterThan(ctx, instruction, signed: false);
                    break;

                case IRInstruction.Op.SetLessThanEqSigned:
                    SetLessThanEq(ctx, instruction, signed: true);
                    break;

                case IRInstruction.Op.SetGreaterThanEqSigned:
                    SetGreaterThanEq(ctx, instruction, signed: true);
                    break;

                case IRInstruction.Op.SetLessThanEqUnsigned:
                    SetLessThanEq(ctx, instruction, signed: false);
                    break;

                case IRInstruction.Op.SetGreaterThanEqUnsigned:
                    SetGreaterThanEq(ctx, instruction, signed: false);
                    break;

                case IRInstruction.Op.ZeroExtend:
                    ZeroExtend(ctx, instruction);
                    break;

                case IRInstruction.Op.SignExtend:
                    SignExtend(ctx, instruction);
                    break;

                case IRInstruction.Op.Truncate:
                    Truncate(ctx, instruction);
                    break;

                default:
                    throw new Exception($"TODO: IR [{instruction.Opcode}] -> x64");
            }

            FreeOperands(ctx, instruction);
        }

        private static void PrintNativeCode(Assembler asm)
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
    }
}