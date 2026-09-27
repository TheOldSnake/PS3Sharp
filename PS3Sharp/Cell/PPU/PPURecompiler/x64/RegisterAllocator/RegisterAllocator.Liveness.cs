
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator
{
    public sealed partial class RegisterAllocator
    {
        public sealed class Liveness
        {
            public Value Value;
            public int FirstUse;
            public int LastUse;
        }
    }
}
