using System.Runtime.Intrinsics;
using static PS3Sharp.Cell.Spu.SpuThread;

namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal static partial class SpuInterpreter
    {
        private static void Selb(SpuThread spu, SpuInstruction instruction)
        {
            // Select Bits
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rc = instruction.Rc;
            int rt = instruction.RtHi;

            unsafe
            {
                SpuState* state = spu.State;
                Span<byte> raLanes = state->GPR[ra].Bytes;
                Span<byte> rbLanes = state->GPR[rb].Bytes;
                Span<byte> rcLanes = state->GPR[rc].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                Vector128<byte> va = Vector128.Create(raLanes);
                Vector128<byte> vb = Vector128.Create(rbLanes);
                Vector128<byte> vc = Vector128.Create(rcLanes);

                // The required operations is:
                // If the bit in register RC is ‘0’, then select the bit from register RA; otherwise, select the bit from register RB.

                Vector128<byte> result = Vector128.ConditionalSelect(vc, vb, va);

                result.CopyTo(rtLanes);
            }
        }

        private static void ShufB(SpuThread spu, SpuInstruction instruction)
        {
            // Shuffle Bytes
            int ra = instruction.Ra;
            int rb = instruction.Rb;
            int rc = instruction.Rc;
            int rt = instruction.RtHi;

            unsafe
            {
                SpuState* state = spu.State;

                // Concatenate RA and RB
                Span<byte> ab = stackalloc byte[32];
                state->GPR[rb].Bytes.CopyTo(ab);
                state->GPR[ra].Bytes.CopyTo(ab.Slice(16));

                // Shuffle RT based on rc
                Span<byte> rcLanes = state->GPR[rc].Bytes;
                Span<byte> rtLanes = state->GPR[rt].Bytes;

                for (int i = 0; i < rcLanes.Length; i++)
                {
                    byte selector = rcLanes[i];
                    byte result;

                    // Special cases
                    if ((selector >> 6) == 0x2)
                    {
                        result = 0x00;
                    }
                    else if ((selector >> 5) == 0x6)
                    {
                        result = 0xFF;
                    }
                    else if ((selector >> 5) == 0x7)
                    {
                        result = 0x80;
                    }
                    else
                    {
                        // Select from AB
                        int idx = 31 - (selector & 0x1F); // Big endian indexing
                        result = ab[idx];
                    }

                    rtLanes[i] = result;
                }
            }
        }
    }
}
