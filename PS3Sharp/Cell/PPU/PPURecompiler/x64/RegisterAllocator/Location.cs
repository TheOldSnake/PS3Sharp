using Iced.Intel;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator {
    public partial class RegisterAllocator {
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
    }
}
