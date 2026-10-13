using Iced.Intel;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal sealed partial class X64Generator
    {
        private sealed class X64Context
        {
            internal Assembler Asm { get; }                                           // Current assembler
            internal AssemblerRegister64 PPUState { get; }                            // PPU state pointer
            internal AssemblerRegister64 Stack { get; }                               // Stack register
            internal AssemblerRegister64 SpillArray { get; }                          // Spill array pointer
            internal AssemblerRegister64 MemoryBase { get; }                          // Memory base pointer

            private readonly ImmutableList<AssemblerRegister64> _borrowPool;          // List of all borrowable registers for this block, based on the allocator
            private readonly ImmutableDictionary<Value, Location> _registerMap;       // Current register mapping
            private readonly ImmutableDictionary<Value, ulong> _knownConstants;       // Current constants dictionary                                        

            private readonly List<AssemblerRegister64> _inUseVolatileRegisters = [];  // Currently active volatile registers (across the block)
            private readonly Stack<AssemblerRegister64> _borrowedRegisters = [];      // Currently borrowed registers by the current instruction operands/handler
            private readonly List<AssemblerRegister64> _reservedRegisters = [];       // Currently reserved registers by the current instruction permenant registers mapping
            private int _currentStackOffset = 8;                                      // Current stack offset in bytes -  starts offset by 8 bytes for the return address

            internal X64Context(
                List<AssemblerRegister64> borrowPool, 
                Dictionary<Value, Location> registerMap, 
                Dictionary<Value, ulong> constants,
                AssemblerRegister64 ppuState,
                AssemblerRegister64 stack,
                AssemblerRegister64 spillArray,
                AssemblerRegister64 memoryBase)
            {
                _borrowPool = borrowPool.ToImmutableList();
                _registerMap = registerMap.ToImmutableDictionary();
                _knownConstants = constants.ToImmutableDictionary();
                PPUState = ppuState;
                Stack = stack;
                SpillArray = spillArray;
                MemoryBase = memoryBase;
                Asm = new(64);
            }

            internal bool IsBorrowedRegister(AssemblerRegister64 reg) => _borrowedRegisters.Contains(reg);

            internal bool IsReservedRegister(AssemblerRegister64 reg) => _reservedRegisters.Contains(reg);

            internal bool IsConst(Value value) => _knownConstants.ContainsKey(value);

            internal bool TryGetConst(Value value, out ulong result) => _knownConstants.TryGetValue(value, out result);

            internal Location GetLocation(Value value) => _registerMap[value];

            internal void StackPush(AssemblerRegister64 reg)
            {
                Asm.push(reg);
                _currentStackOffset += 8;
            }

            internal void StackPop(AssemblerRegister64 reg)
            {
                Asm.pop(reg);
                _currentStackOffset -= 8;
            }

            internal void StackAlloc(int size)
            {
                Asm.sub(Stack, size);
                _currentStackOffset -= size;
            }

            internal int GetStackSlotsCount()
            {
                int lastIndex = -1;
                foreach (KeyValuePair<Value, Location> item in _registerMap)
                {
                    Location location = item.Value;
                    if (location.IsSpilled)
                    {
                        lastIndex = int.Max(lastIndex, location.SpillSlot);
                    }
                }

                return lastIndex + 1;
            }

            internal List<AssemblerRegister64> GetNonVolatileMappedRegisters()
            {
                List<AssemblerRegister64> regs = new();

                foreach (KeyValuePair<Value, Location> item in _registerMap)
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

            internal ReadOnlySpan<AssemblerRegister64> GetInUseVolatileRegisters()
            {
                return CollectionsMarshal.AsSpan(_inUseVolatileRegisters);
            }

            internal void AddInUseVolatileRegister(AssemblerRegister64 reg)
            {
                if (!_inUseVolatileRegisters.Contains(reg))
                {
                    _inUseVolatileRegisters.Add(reg);
                }
            }

            internal void RemoveInUseVolatileRegister(AssemblerRegister64 reg)
            {
                _inUseVolatileRegisters.Remove(reg);
            }

            internal void AddReservedRegister(AssemblerRegister64 reg)
            {
                if (!_reservedRegisters.Contains(reg))
                {
                    _reservedRegisters.Add(reg);
                }
            }

            internal void RemoveReservedRegister(AssemblerRegister64 reg)
            {
                _reservedRegisters.Remove(reg);
            }

            internal void ClearReservedRegister()
            {
                _reservedRegisters.Clear();
            }

            internal AssemblerRegister64 BorrowRegister()
            {
                // Try to find a borrowable register
                foreach (AssemblerRegister64 reg in _borrowPool)
                {
                    if (TryBorrowRegister(reg))
                    {
                        return reg;
                    }
                }

                // If we fail this means all registers are corrently borrowed,
                // something must gone very wrong
                throw new Exception($"[x64 Generator] Too many borrows");
            }

            internal void ReturnBorrowedRegister(AssemblerRegister64 reg)
            {
                // Ensure that the register was borrowed and that is is the most recent borrow
                bool validReturn = _borrowedRegisters.Contains(reg) && _borrowedRegisters.Peek() == reg;

                if (!validReturn)
                {
                    throw new Exception($"[x64 Generator] Invalid return for register: {reg.Value}");
                }

                StackPop(reg);
                _borrowedRegisters.Pop();
            }

            private bool TryBorrowRegister(AssemblerRegister64 reg)
            {
                if (!IsBorrowedRegister(reg) && !IsReservedRegister(reg))
                {
                    StackPush(reg);
                    _borrowedRegisters.Push(reg);
                    return true;
                }

                return false;
            }
        }
    }
}