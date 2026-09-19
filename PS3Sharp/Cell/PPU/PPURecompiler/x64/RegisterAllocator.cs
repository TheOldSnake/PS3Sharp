using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;
using static PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64 {
    public static class RegisterAllocator {
        public class Liveness {
            public Value Value;
            public int FirstUse;
            public int LastUse;
        }

        public class Location {
            public Liveness Liveness { get; }
            private AssemblerRegister64? Register { get; set; }
            public int SpillSlot { get; set; } = -1;

            public bool IsSpilled => SpillSlot >= 0;
            public Value Value => Liveness.Value;
            public Location(Liveness liveness) {
                Liveness = liveness;
            }

            public AssemblerRegister64 GetRegister() {
                if (Register != null) {
                    return Register.Value;
                } else {
                    throw new Exception("No register was assigned");
                }
            }

            public void AssignRegister(AssemblerRegister64 register) { 
                Register = register;
            }

            public void UnassignRegister() {
                Register = null;
            }
        }

        public static AssemblerRegister64 MemoryBaseRegister => r8;

        public static readonly AssemblerRegister64[] StaticRegisterPool64 = [   
           rax,
           rcx,
           rdx,
        //  rbx,  // PPU State pointer
        //  rsp,  // Current Stack pointer
        //  rbp,  // Spill Array pointer
           rsi,
           rdi,
        //  r8,   // Guest memory base pointer 
           r9,
           r10,
           r11,
           r12,
           r13,
           r14,
           r15,
        ];

        public static readonly AssemblerRegister64[] StaticNonVolatileRegisterPool64 = [
            rsi,
            rdi,
            r12,
            r13,
            r14,
            r15,
        ];

        public static readonly AssemblerRegister64[] StaticVolatileRegisterPool64 = [
            rax,
            rcx,
            rdx,
           // r8, // Guest memory base pointer 
            r9,
            r10,
            r11,
        ];

        private static List<AssemblerRegister64> CurrentRegisterPool = new();
        private static bool _canAllocate => CurrentRegisterPool.Count > 0;
        private static int _spillIndex = 0;

        private static AssemblerRegister64 RequestRegister64() {
            AssemblerRegister64 reg = CurrentRegisterPool[0];
            CurrentRegisterPool.Remove(reg);
            return reg;
        }

        private static void FreeRegister64(AssemblerRegister64 reg) {
            CurrentRegisterPool.Add(reg);
        }

        public static void ResetAllocatorState() {
            _spillIndex = 0;
            CurrentRegisterPool.Clear();

            for (int i = 0; i < StaticRegisterPool64.Length; i++) {
                CurrentRegisterPool.Add(StaticRegisterPool64[i]);
            }
        }

        public static Dictionary<Value, Location> AllocateHostRegisters(List<IRInstruction> code, Dictionary<Value, ulong> knownConstants) {
            // Reset
            ResetAllocatorState();

            List<Liveness> livenessList = BuildLivenessDictionary(code, knownConstants);
            Dictionary<Value, Location> allocatedRegs = new();

            AssignHostRegs(allocatedRegs, livenessList);

            //Console.WriteLine($"Mapped Registers:");

            foreach (var item in allocatedRegs) {
                int id = item.Key.Id;
                Location l = item.Value;

                Console.WriteLine($"v{id}: {(l.IsSpilled ? $"SpillSlot({l.SpillSlot})" : $"{l.GetRegister().Value}")}");
            }

            return allocatedRegs;
        }


        private static void AssignHostRegs(Dictionary<Value, Location> locations, List<Liveness> intervals) {

            // Values that currently occupy host registers.
            List<Location> active = new();

            foreach (Liveness current in intervals) {

                // Remove intervals whose lifetime has ended
                for (int i = active.Count - 1; i >= 0; i--) {
                    Location location = active[i];

                    if (location.Liveness.LastUse < current.FirstUse) {
                        active.RemoveAt(i);
                        FreeRegister64(location.GetRegister());
                    }
                }

                // Try to allocate a free register
                if (_canAllocate) {
                    Location location = new(current);
                    location.AssignRegister(RequestRegister64());

                    locations[current.Value] = location;
                    active.Add(location);

                    continue;
                }

                // If failed, find the active interval that lives the longest.
                Location victim = active.OrderByDescending(x => x.Liveness.LastUse).First();

                // If the current value dies earlier than the victim, just spill the current value.
                if (current.LastUse < victim.Liveness.LastUse) {
                    Location location = new(current);
                    location.SpillSlot = _spillIndex++;

                    locations[current.Value] = location;

                } else {
                    // if the current value lives longer
                    // Evict the victim and give its register to current.

                    AssemblerRegister64 victimRegister = victim.GetRegister();

                    // Spill victim
                    victim.SpillSlot = _spillIndex++;
                    victim.UnassignRegister();

                    // Give its register to current
                    Location location = new(current);
                    location.AssignRegister(victimRegister);

                    locations[current.Value] = location;

                    // Replace victim with current in active set
                    active.Remove(victim);
                    active.Add(location);
                }
            }
        }

        private static List<Liveness> BuildLivenessDictionary(List<IRInstruction> code, Dictionary<Value, ulong> knownConstants) {
            List<Liveness> livenessList = new();

            for (int i = 0; i < code.Count; i++) {
                IRInstruction instruction = code[i];
                AddLiveness(code, instruction.Destination, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand1, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand2, i, livenessList, knownConstants);
                AddLiveness(code, instruction.Operand3, i, livenessList, knownConstants);
            }

            livenessList = livenessList.OrderBy(x => x.FirstUse).ToList();
            //Console.WriteLine($"Liveness report:");

            //foreach (var l in livenessList) {
            //    Console.WriteLine($"v{l.Value.Id} first use: {l.FirstUse} last use: {l.LastUse}");
            //}

            return livenessList;
        }

       private static void AddLiveness(List<IRInstruction> code, Value value, int currentIndex, List<Liveness> livenessList, Dictionary<Value, ulong> knownConstants) {
            if (!value.IsValid) {
                return;
            }

            if (knownConstants.ContainsKey(value)) {
                return;
            }

            if (livenessList.Any(x => x.Value == value)) {
                return;
            }
            
            Liveness liveness = new();
            liveness.Value = value;
            liveness.FirstUse = currentIndex;

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

    }
}
