using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.PPU
{
    internal unsafe sealed partial class PPUThread
    {
        // Unmanaged State
        [StructLayout(LayoutKind.Sequential)]
        internal struct PPUState
        {
            internal uint PC;                     // Program Counter
            internal uint LR;                     // Link Register      
            internal uint TB;                     // Time Base Register -- is this global, or per thread ?
            internal uint VRSAVE;                 // Vector Save/Restore Register, default value = 0xFFFFFFFF
            internal ConditionRegister CR;        // Condition Register
            internal ulong CTR;                   // Count Register
            internal XERRegister XER;             // XER Register
            internal InlineData.InlineArray32<PPUGPR> GPR;
            internal InlineData.InlineArray32<PPUFPR> FPR;
            internal InlineData.InlineArray32<PPUVR> VRF;
            internal void* Context;
        }
    }
}
