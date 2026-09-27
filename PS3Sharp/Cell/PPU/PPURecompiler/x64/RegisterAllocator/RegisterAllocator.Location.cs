using Iced.Intel;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64.RegisterAllocator
{
    internal sealed partial class RegisterAllocator
    {
        internal sealed class Location
        {
            internal Liveness Liveness { get; }
            private AssemblerRegister64? Register { get; set; }
            internal int SpillSlot { get; set; } = -1;

            internal bool IsSpilled => SpillSlot >= 0;
            internal Value Value => Liveness.Value;
            internal Location(Liveness liveness)
            {
                Liveness = liveness;
            }

            internal AssemblerRegister64 GetRegister()
            {
                if (Register != null)
                {
                    return Register.Value;
                }
                else
                {
                    throw new Exception("No register was assigned");
                }
            }

            internal void AssignRegister(AssemblerRegister64 register)
            {
                Register = register;
            }

            internal void UnassignRegister()
            {
                Register = null;
            }
        }
    }
}
