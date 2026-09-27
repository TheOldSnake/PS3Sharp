using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator.RegisterAllocator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator {
    public sealed partial class RegisterAllocator {

        // Register pools
		private readonly AssemblerRegister64[] StaticRegisterPool64 = [
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

		private readonly AssemblerRegister64[] StaticNonVolatileRegisterPool64 = [
			rbx,
			rbp,
			rsi,
			rdi,
			r12,
			r13,
			r14,
			r15,
		];

		private readonly AssemblerRegister64[] StaticVolatileRegisterPool64 = [
			rax,
			rcx,
			rdx,
			r8,
			r9,
			r10,
			r11,
		];

		public AssemblerRegister64 PPUStatePointer => rbx;
        public AssemblerRegister64 StackPointer => rsp;
        public AssemblerRegister64 SpillArrayPointer => rbp;
        public AssemblerRegister64 MemoryBasePointer => r8;

        public ReadOnlySpan<AssemblerRegister64> AllRegisters => StaticRegisterPool64;
        public ReadOnlySpan<AssemblerRegister64> AllNonVolatileRegisters => StaticNonVolatileRegisterPool64;
        public ReadOnlySpan<AssemblerRegister64> AllVolatileRegisters => StaticVolatileRegisterPool64;

        private List<AssemblerRegister64> CurrentRegisterPool = new();
        private bool _canAllocate => CurrentRegisterPool.Count > 0;
        private int _spillIndex = 0;

		public bool IsVolatile(AssemblerRegister64 reg) => StaticVolatileRegisterPool64.Contains(reg);
		public bool IsNonVolatile(AssemblerRegister64 reg) => StaticNonVolatileRegisterPool64.Contains(reg);

		private AssemblerRegister64 RequestRegister64() {
            AssemblerRegister64 reg = CurrentRegisterPool[0];
            CurrentRegisterPool.Remove(reg);
            return reg;
        }

        private void FreeRegister64(AssemblerRegister64 reg) {
            CurrentRegisterPool.Add(reg);
        }

        public void ResetAllocatorState() {
            _spillIndex = 0;
            CurrentRegisterPool.Clear();
        }

        private static bool RequiresReservedRaxRdx(IRInstruction instruction) {
            bool isSpecialMulDiv =
                instruction.Opcode == IRInstruction.Op.IMulHD       ||
                instruction.Opcode == IRInstruction.Op.MulHD        ||
                instruction.Opcode == IRInstruction.Op.CheckedIDivD ||
                instruction.Opcode == IRInstruction.Op.CheckedDivD  ||
                instruction.Opcode == IRInstruction.Op.CheckedIDivW ||
                instruction.Opcode == IRInstruction.Op.CheckedDivW;

            return isSpecialMulDiv;
        }

        public List<AssemblerRegister64> CreateRegisterPool(List<IRInstruction> code) {

            // Initial list of all registers
            List<AssemblerRegister64> regs = new List<AssemblerRegister64>(StaticRegisterPool64);

            // Remove registers that are reserved from the pool
            regs.Remove(PPUStatePointer);
            regs.Remove(StackPointer);
            regs.Remove(SpillArrayPointer);

            if (HasMemoryOp(code)) {
                regs.Remove(MemoryBasePointer);
            }

            // RAX and RDX are enabled by default
            // but if the block needs any special multiply / divide instructions,
            // we reserve them by removing them from the pool
            bool shouldReserve = false;

            for (int i = 0; i < code.Count; i++) {
                if (RequiresReservedRaxRdx(code[i])) {
                    shouldReserve = true; 
                    break;
                }
            }

            if (shouldReserve) {
                regs.Remove(rax);
                regs.Remove(rdx);
            }

            return regs;
        }

        public Dictionary<Value, Location> AllocateHostRegisters(List<IRInstruction> code, Dictionary<Value, ulong> knownConstants) {
            // Reset
            
            ResetAllocatorState();
            CurrentRegisterPool = CreateRegisterPool(code);

            List<Liveness> livenessList = BuildLivenessList(code, knownConstants);
            Dictionary<Value, Location> allocatedRegs = new();

            AssignHostRegs(allocatedRegs, livenessList);

            return allocatedRegs;
        }

        private void AssignHostRegs(Dictionary<Value, Location> locations, List<Liveness> intervals) {
            // Values that currently occupy host registers.
            List<Location> currentActiveIntervals = new();

            foreach (Liveness currentInterval in intervals) {

                // Remove intervals whose lifetime has ended
                RemoveExpiredIntervals(currentActiveIntervals, currentInterval);

                // Try to allocate a free register from the pool
                if (TryAllocateRegisterFromPool(locations, currentActiveIntervals, currentInterval)) {
                    continue;
                }

                // If failed, find the active interval that lives the longest.
                Location victim = currentActiveIntervals.OrderByDescending(x => x.Liveness.LastUse).First();

                // We give the register to the value that lives the longest 
                // If the current value dies earlier than the victim, just spill the current value.
                if (currentInterval.LastUse < victim.Liveness.LastUse) {
                    Spill(locations, currentInterval);

                } else {
                    // if the current value lives longer
                    // Evict the victim and give its register to the current value
                    EvictAndSpill(locations, currentActiveIntervals, currentInterval, victim);
                }
            }
        }

        private void RemoveExpiredIntervals(List<Location> active, Liveness currentInterval) {
            // Remove intervals whose lifetime has ended
            for (int i = active.Count - 1; i >= 0; i--) {
                Location location = active[i];

                if (location.Liveness.LastUse < currentInterval.FirstUse) {
                    active.RemoveAt(i);
                    AssemblerRegister64 reg = location.GetRegister();
                    FreeRegister64(reg);
                }
            }
        }

        private bool TryAllocateRegisterFromPool(Dictionary<Value, Location> locations, List<Location> currentActiveIntervals, Liveness currentInterval) {
            if (!_canAllocate) {
                return false;
            }

            Location location = new(currentInterval);
            AssemblerRegister64 reg = RequestRegister64();
            location.AssignRegister(reg);

            locations[currentInterval.Value] = location;
            currentActiveIntervals.Add(location);

            return true;
        }

        private void Spill(Dictionary<Value, Location> locations, Liveness current) {
            Location location = new(current) {
                SpillSlot = _spillIndex++
            };

            locations[current.Value] = location;
        }

        private void EvictAndSpill(Dictionary<Value, Location> locations, List<Location> currentActiveIntervals, Liveness currentInterval, Location victim) {
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

        private List<Liveness> BuildLivenessList(List<IRInstruction> code, Dictionary<Value, ulong> knownConstants) {
            List<Liveness> livenessList = new();

            for (int i = 0; i < code.Count; i++) {
                IRInstruction instruction = code[i];
                AddLiveness(code, instruction.Destination, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand1, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand2, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand3, i, livenessList, knownConstants);
            }

            livenessList = livenessList.OrderBy(x => x.FirstUse).ToList();
            return livenessList;
        }

       private void AddLiveness(List<IRInstruction> code, Value value, int currentIndex, List<Liveness> livenessList, Dictionary<Value, ulong> knownConstants) {
            if (!value.IsValid) {
                return;
            }

            if (knownConstants.ContainsKey(value)) {
                return;
            }

            if (livenessList.Any(x => x.Value == value)) {
                return;
            }

            Liveness liveness = new() {
                Value = value,
                FirstUse = currentIndex
            };

            // Find last use, backwards search

            for (int i = code.Count - 1; i >= 0; i--) {
                IRInstruction instruction = code[i];

                bool found =

                    (instruction.Destination.IsValid && instruction.Destination == liveness.Value) ||
                    (instruction.Operand1.IsValid && instruction.Operand1 == liveness.Value) ||
                    (instruction.Operand2.IsValid && instruction.Operand2 == liveness.Value) ||
                    (instruction.Operand3.IsValid && instruction.Operand3 == liveness.Value);


                if (found) {
                    liveness.LastUse = i;
                    break;
                }

                if (i == currentIndex) {
                    liveness.LastUse = i;
                }
            }

            livenessList.Add(liveness);
        }

        private void PrintRegisterMapping(Dictionary<Value, Location> allocatedRegs) {
            Console.WriteLine($"Mapped Registers:");
            foreach (var item in allocatedRegs) {
                int id = item.Key.Id;
                Location l = item.Value;

                Console.WriteLine($"v{id}: {(l.IsSpilled ? $"SpillSlot({l.SpillSlot})" : $"{l.GetRegister().Value}")}");
            }
        }
    }
}
