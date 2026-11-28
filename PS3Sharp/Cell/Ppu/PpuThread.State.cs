using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.Ppu
{
    internal sealed unsafe partial class PpuThread
    {
        // Unmanaged State
        [StructLayout(LayoutKind.Sequential)]
        internal struct PpuState
        {
            internal uint PC;                     // Program Counter
            internal uint LR;                     // Link Register      
            internal uint TB;                     // Time Base Register -- is this global, or per thread ?
            internal uint VrSave;                 // Vector Save/Restore Register, default value = 0xFFFFFFFF
            internal ConditionRegister CR;        // Condition Register
            internal ulong Ctr;                   // Count Register
            internal XerRegister Xer;             // XER Register
            internal InlineData.InlineArray32<PpuGpr> Gpr;
            internal InlineData.InlineArray32<PpuFpr> Fpr;
            internal InlineData.InlineArray32<PpuVr> Vrf;
            internal void* Context;
        }
    }
}
