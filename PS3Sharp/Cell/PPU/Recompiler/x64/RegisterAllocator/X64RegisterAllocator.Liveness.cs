
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.Recompiler.x64.RegisterAllocator
{
    internal sealed partial class X64RegisterAllocator
    {
        internal sealed class Liveness
        {
            internal Value Value;
            internal int FirstUse;
            internal int LastUse;
        }
    }
}
