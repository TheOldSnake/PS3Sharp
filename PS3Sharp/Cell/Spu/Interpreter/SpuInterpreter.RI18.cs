using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        // RI18: 7-bit opcode group
        private static void Ila(SpuThread spu, SpuInstruction instruction)
        {
            // Immediate Load Address
            int rt = instruction.RtLo;
            int i18 = instruction.I18;

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> rtLanes = state->GPR[rt].Ints;
                Vector128<int> vi18 = Vector128.Create(i18);
                vi18.CopyTo(rtLanes);
            }
        }
    }
}
