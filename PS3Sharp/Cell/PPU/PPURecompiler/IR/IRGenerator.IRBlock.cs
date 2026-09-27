namespace PS3Sharp.Cell.PPU.PPURecompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal sealed class IRBlock
        {
            internal uint VirtualAddressStart;
            internal uint VirtualAddressEnd;      // address of last ppc instruction
            internal List<IRInstruction> IRCode;
            internal Dictionary<Value, ulong> Constants;
            internal int IsSyscallExit = 0;

            internal IRBlock(uint startAddress, uint endAddress, List<IRInstruction> code, Dictionary<Value, ulong> constants, int isSyscallExit)
            {
                VirtualAddressStart = startAddress;
                VirtualAddressEnd = endAddress;
                IRCode = code;
                Constants = constants;
                IsSyscallExit = isSyscallExit;
            }
        }
    }
}

