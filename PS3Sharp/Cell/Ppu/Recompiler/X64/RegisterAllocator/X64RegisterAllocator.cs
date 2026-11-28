using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.Ppu.Recompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.Ppu.Recompiler.X64.RegisterAllocator
{
    internal sealed partial class X64RegisterAllocator
    {
        // Register pools
        private static readonly AssemblerRegister64[] _registerPool64 =
        [
           rax,
           rcx,
           rdx,
           rbx,
           rsp,
           rbp,
           rsi,
           rdi,
           r8,
           r9,
           r10,
           r11,
           r12,
           r13,
           r14,
           r15,
        ];

        private static readonly AssemblerRegister64[] _nonVolatileRegisterPool64 =
        [
            rbx,
            rbp,
            rsi,
            rdi,
            r12,
            r13,
            r14,
            r15,
        ];

        private static readonly AssemblerRegister64[] _volatileRegisterPool64 =
        [
            rax,
            rcx,
            rdx,
            r8,
            r9,
            r10,
            r11,
        ];

        internal static ReadOnlySpan<AssemblerRegister64> AllRegisters => _registerPool64;
        internal static ReadOnlySpan<AssemblerRegister64> AllNonVolatileRegisters => _nonVolatileRegisterPool64;
        internal static ReadOnlySpan<AssemblerRegister64> AllVolatileRegisters => _volatileRegisterPool64;

        // Currently these assignments are permanent for all blocks
        internal static AssemblerRegister64 PPUStatePointer => rbx;
        internal static AssemblerRegister64 StackPointer => rsp;
        internal static AssemblerRegister64 SpillArrayPointer => rbp;
        internal static AssemblerRegister64 MemoryBasePointer => r8;

        private int _spillIndex = 0;
        private List<AssemblerRegister64> _currentRegisterPool = [];
        private bool CanAllocate => _currentRegisterPool.Count > 0;

        internal static bool IsVolatile(AssemblerRegister64 reg) => _volatileRegisterPool64.Contains(reg);
        internal static bool IsNonVolatile(AssemblerRegister64 reg) => _nonVolatileRegisterPool64.Contains(reg);

        internal void ResetAllocatorState()
        {
            _spillIndex = 0;
            _currentRegisterPool.Clear();
        }

        internal Dictionary<Value, Location> AllocateHostRegisters(List<IRInstruction> code, Dictionary<Value, ulong> knownConstants)
        {
            // Reset
            ResetAllocatorState();
            _currentRegisterPool = CreateRegisterPool(code);

            List<Liveness> livenessList = BuildLivenessList(code, knownConstants);
            Dictionary<Value, Location> allocatedRegs = AssignHostRegs(livenessList);

            return allocatedRegs;
        }

        internal List<AssemblerRegister64> CreateRegisterPool(List<IRInstruction> code)
        {

            // Initial list of all registers
            List<AssemblerRegister64> regs = new(_registerPool64);

            // Remove registers that are reserved from the pool
            regs.Remove(PPUStatePointer);
            regs.Remove(StackPointer);
            regs.Remove(SpillArrayPointer);

            if (HasMemoryOp(code))
            {
                regs.Remove(MemoryBasePointer);
            }

            // RAX and RDX are enabled by default
            // but if the block needs any special multiply / divide instructions,
            // we reserve them by removing them from the pool
            bool shouldReserve = false;

            for (int i = 0; i < code.Count; i++)
            {
                if (RequiresReservedRaxRdx(code[i]))
                {
                    shouldReserve = true;
                    break;
                }
            }

            if (shouldReserve)
            {
                regs.Remove(rax);
                regs.Remove(rdx);
            }

            return regs;
        }

        private AssemblerRegister64 RequestRegister64()
        {
            AssemblerRegister64 reg = _currentRegisterPool[0];
            _currentRegisterPool.Remove(reg);
            return reg;
        }

        private void FreeRegister64(AssemblerRegister64 reg)
        {
            _currentRegisterPool.Add(reg);
        }

        private static bool RequiresReservedRaxRdx(IRInstruction instruction)
        {
            bool isSpecialMulDiv =
                instruction.Opcode == IRInstruction.Op.IMulHD ||
                instruction.Opcode == IRInstruction.Op.MulHD ||
                instruction.Opcode == IRInstruction.Op.CheckedIDivD ||
                instruction.Opcode == IRInstruction.Op.CheckedDivD ||
                instruction.Opcode == IRInstruction.Op.CheckedIDivW ||
                instruction.Opcode == IRInstruction.Op.CheckedDivW;

            return isSpecialMulDiv;
        }

        private Dictionary<Value, Location> AssignHostRegs(List<Liveness> intervals)
        {
            // Final locations
            Dictionary<Value, Location> locations = [];

            // Values that currently occupy host registers.
            List<Location> currentActiveIntervals = [];

            foreach (Liveness currentInterval in intervals)
            {

                // Remove intervals whose lifetime has ended
                RemoveExpiredIntervals(currentActiveIntervals, currentInterval);

                // Try to allocate a free register from the pool
                if (TryAllocateRegisterFromPool(locations, currentActiveIntervals, currentInterval))
                {
                    continue;
                }

                // If failed, find the active interval that lives the longest.
                Location victim = currentActiveIntervals.OrderByDescending(x => x.Liveness.LastUse).First();

                // We give the register to the value that lives the longest 
                // If the current value dies earlier than the victim, just spill the current value.
                if (currentInterval.LastUse < victim.Liveness.LastUse)
                {
                    Spill(locations, currentInterval);
                }
                else
                {
                    // if the current value lives longer
                    // Evict the victim and give its register to the current value
                    EvictAndSpill(locations, currentActiveIntervals, currentInterval, victim);
                }
            }

            return locations;
        }

        private void RemoveExpiredIntervals(List<Location> active, Liveness currentInterval)
        {
            // Remove intervals whose lifetime has ended
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Location location = active[i];

                if (location.Liveness.LastUse < currentInterval.FirstUse)
                {
                    active.RemoveAt(i);
                    AssemblerRegister64 reg = location.GetRegister();
                    FreeRegister64(reg);
                }
            }
        }

        private bool TryAllocateRegisterFromPool(Dictionary<Value, Location> locations, List<Location> currentActiveIntervals, Liveness currentInterval)
        {
            if (!CanAllocate)
            {
                return false;
            }

            Location location = new(currentInterval);
            AssemblerRegister64 reg = RequestRegister64();
            location.AssignRegister(reg);

            locations[currentInterval.Value] = location;
            currentActiveIntervals.Add(location);

            return true;
        }

        private void Spill(Dictionary<Value, Location> locations, Liveness current)
        {
            Location location = new(current)
            {
                SpillSlot = _spillIndex++
            };

            locations[current.Value] = location;
        }

        private void EvictAndSpill(Dictionary<Value, Location> locations, List<Location> currentActiveIntervals, Liveness currentInterval, Location victim)
        {
            AssemblerRegister64 victimRegister = victim.GetRegister();

            // Spill victim
            victim.SpillSlot = _spillIndex++;
            victim.UnassignRegister();

            // Give its register to current
            Location location = new(currentInterval);
            location.AssignRegister(victimRegister);

            locations[currentInterval.Value] = location;

            // Replace victim with current in active set
            currentActiveIntervals.Remove(victim);
            currentActiveIntervals.Add(location);
        }

        private List<Liveness> BuildLivenessList(List<IRInstruction> code, Dictionary<Value, ulong> knownConstants)
        {
            List<Liveness> livenessList = new();

            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                AddLiveness(code, instruction.Destination, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand1, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand2, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand3, i, livenessList, knownConstants);
            }

            livenessList = livenessList.OrderBy(x => x.FirstUse).ToList();
            return livenessList;
        }

        private void AddLiveness(List<IRInstruction> code, Value value, int currentIndex, List<Liveness> livenessList, Dictionary<Value, ulong> knownConstants)
        {
            if (!value.IsValid)
            {
                return;
            }

            if (knownConstants.ContainsKey(value))
            {
                return;
            }

            if (livenessList.Any(x => x.Value == value))
            {
                return;
            }

            Liveness liveness = new()
            {
                Value = value,
                FirstUse = currentIndex
            };

            // Find last use, backwards search

            for (int i = code.Count - 1; i >= 0; i--)
            {
                IRInstruction instruction = code[i];

                bool found =

                    (instruction.Destination.IsValid && instruction.Destination == liveness.Value) ||
                    (instruction.Operand1.IsValid && instruction.Operand1 == liveness.Value) ||
                    (instruction.Operand2.IsValid && instruction.Operand2 == liveness.Value) ||
                    (instruction.Operand3.IsValid && instruction.Operand3 == liveness.Value);


                if (found)
                {
                    liveness.LastUse = i;
                    break;
                }

                if (i == currentIndex)
                {
                    liveness.LastUse = i;
                }
            }

            livenessList.Add(liveness);
        }

        private void PrintRegisterMapping(Dictionary<Value, Location> allocatedRegs)
        {
            Console.WriteLine($"Mapped Registers:");
            foreach (KeyValuePair<Value, Location> item in allocatedRegs)
            {
                int id = item.Key.Id;
                Location l = item.Value;

                Console.WriteLine($"v{id}: {(l.IsSpilled ? $"SpillSlot({l.SpillSlot})" : $"{l.GetRegister().Value}")}");
            }
        }
    }
}
