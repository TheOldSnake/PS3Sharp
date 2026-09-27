
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator
{
    internal sealed partial class RegisterAllocator
    {
        internal sealed class Liveness
        {
            internal Value Value;
            internal int FirstUse;
            internal int LastUse;
        }
    }
}
