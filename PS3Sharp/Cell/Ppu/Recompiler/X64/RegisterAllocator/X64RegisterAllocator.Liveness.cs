
using static PS3Sharp.Cell.Ppu.Recompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.Ppu.Recompiler.X64.RegisterAllocator
{
    internal sealed partial class X64RegisterAllocator
    {
        internal sealed class Liveness
        {
            internal Value Value { get; set; }
            internal int FirstUse { get; set; }
            internal int LastUse { get; set; }
        }
    }
}
