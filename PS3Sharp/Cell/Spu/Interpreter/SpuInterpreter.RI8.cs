using System.Numerics;
using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        private static void Cflts(SpuThread spu, SpuInstruction instruction)
        {
            // Convert Floating to Signed Integer
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            float scale = 1 << (173 - instruction.I8);

            unsafe
            {
                SpuState* state = spu.State;
                Span<float> raLanes = state->GPR[ra].Floats;
                Span<int> rtLanes = state->GPR[rt].Ints;

                // Clamp to [-0x80000000, 0x7FFFFFFF]
                long value0 = long.Clamp((long)(raLanes[0] * scale), int.MinValue, int.MaxValue);
                long value1 = long.Clamp((long)(raLanes[1] * scale), int.MinValue, int.MaxValue);
                long value2 = long.Clamp((long)(raLanes[2] * scale), int.MinValue, int.MaxValue);
                long value3 = long.Clamp((long)(raLanes[3] * scale), int.MinValue, int.MaxValue);

                rtLanes[0] = (int)value0;
                rtLanes[1] = (int)value1;
                rtLanes[2] = (int)value2;
                rtLanes[3] = (int)value3;
            }
        }

        private static void Cfltu(SpuThread spu, SpuInstruction instruction)
        {
            // Convert Floating to Unsigned Integer
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            float scale = 1 << (173 - instruction.I8);

            unsafe
            {
                SpuState* state = spu.State;
                Span<float> raLanes = state->GPR[ra].Floats;
                Span<uint> rtLanes = state->GPR[rt].UInts;

                // Clamp to [0x00000000, 0xFFFFFFFF]
                long value0 = long.Clamp((long)(raLanes[0] * scale), uint.MinValue, 0);
                long value1 = long.Clamp((long)(raLanes[1] * scale), uint.MinValue, 0);
                long value2 = long.Clamp((long)(raLanes[2] * scale), uint.MinValue, 0);
                long value3 = long.Clamp((long)(raLanes[3] * scale), uint.MinValue, 0);

                rtLanes[0] = (uint)value0;
                rtLanes[1] = (uint)value1;
                rtLanes[2] = (uint)value2;
                rtLanes[3] = (uint)value3;
            }
        }

        private static void Csflt(SpuThread spu, SpuInstruction instruction)
        {
            // Convert Signed Integer to Floating
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int scale = 1 << (155 - instruction.I8);

            unsafe
            {
                SpuState* state = spu.State;
                Span<int> raLanes = state->GPR[ra].Ints;
                Span<float> rtLanes = state->GPR[rt].Floats;

                rtLanes[0] = (float)raLanes[0] / scale;
                rtLanes[1] = (float)raLanes[1] / scale;
                rtLanes[2] = (float)raLanes[2] / scale;
                rtLanes[3] = (float)raLanes[3] / scale;
            }
        }

        private static void Cuflt(SpuThread spu, SpuInstruction instruction)
        {
            // Convert Unsigned Integer to Floating
            int ra = instruction.Ra;
            int rt = instruction.RtLo;
            int scale = 1 << (155 - instruction.I8);

            unsafe
            {
                SpuState* state = spu.State;
                Span<uint> raLanes = state->GPR[ra].UInts;
                Span<float> rtLanes = state->GPR[rt].Floats;

                rtLanes[0] = (float)raLanes[0] / scale;
                rtLanes[1] = (float)raLanes[1] / scale;
                rtLanes[2] = (float)raLanes[2] / scale;
                rtLanes[3] = (float)raLanes[3] / scale;
            }
        }
    }
}
