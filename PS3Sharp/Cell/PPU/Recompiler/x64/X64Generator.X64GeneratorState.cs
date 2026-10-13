using Iced.Intel;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator.X64RegisterAllocator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64
{
    internal sealed partial class X64Generator
    {
        private sealed class X64GeneratorState
        {
            internal AssemblerRegister64 PPUState { get; }                            // PPU state pointer
            internal AssemblerRegister64 Stack { get; }                               // Stack register
            internal AssemblerRegister64 SpillArray { get; }                          // Spill array pointer
            internal AssemblerRegister64 MemoryBase { get; }                          // Memory base pointer

            internal ImmutableList<AssemblerRegister64> BorrowPool { get; }           // List of all borrowable registers for this block, based on the allocator
            internal ImmutableDictionary<Value, Location> RegisterMap { get; }        // Current register mapping
            internal ImmutableDictionary<Value, ulong> KnownConstants { get; }        // Current constants dictionary                                        
            internal int CurrentStackOffset { get; set; }                             // Current stack offset in bytes
            internal Assembler Asm { get; }                                           // Current assembler

            private readonly List<AssemblerRegister64> _inUseVolatileRegisters = [];  // Currently active volatile registers (across the block)
            private readonly Stack<AssemblerRegister64> _borrowedRegisters = [];      // Currently borrowed registers by the current instruction operands/handler
            private readonly List<AssemblerRegister64> _reservedRegisters = [];       // Currently reserved registers by the current instruction permenant registers mapping

            internal X64GeneratorState(
                AssemblerRegister64 ppuState, 
                AssemblerRegister64 stack, 
                AssemblerRegister64 spillArray, 
                AssemblerRegister64 memoryBase, 
                List<AssemblerRegister64> borrowPool, 
                Dictionary<Value, Location> registerMap, 
                Dictionary<Value, ulong> constants)
            {
                PPUState = ppuState;
                Stack = stack;
                SpillArray = spillArray;
                MemoryBase = memoryBase;
                BorrowPool = borrowPool.ToImmutableList();
                RegisterMap = registerMap.ToImmutableDictionary();
                KnownConstants = constants.ToImmutableDictionary();
                Asm = new(64);
            }

            internal bool IsBorrowedRegister(AssemblerRegister64 reg) => _borrowedRegisters.Contains(reg);

            internal bool IsReservedRegister(AssemblerRegister64 reg) => _reservedRegisters.Contains(reg);

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

            internal void AddBorrowedRegister(AssemblerRegister64 reg)
            {
                if (!_borrowedRegisters.Contains(reg))
                {
                    _borrowedRegisters.Push(reg);
                }
            }

            internal void RemoveBorrowedRegister(AssemblerRegister64 reg)
            {
                // Ensure that the register was borrowed and that is is the most recent borrow
                bool validReturn = _borrowedRegisters.Contains(reg) && _borrowedRegisters.Peek() == reg;

                if (!validReturn)
                {
                    throw new Exception($"[x64 Generator] Invalid return for register: {reg.Value}");
                }

                _borrowedRegisters.Pop();
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
        }
    }
}