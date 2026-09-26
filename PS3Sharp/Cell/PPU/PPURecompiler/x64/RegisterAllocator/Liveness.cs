using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator {
    public partial class RegisterAllocator {
        public class Liveness {
            public Value Value;
            public int FirstUse;
            public int LastUse;
        }
    }
}
