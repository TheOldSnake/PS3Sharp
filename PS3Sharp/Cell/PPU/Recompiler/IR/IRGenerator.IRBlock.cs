namespace PS3Sharp.Cell.PPU.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal sealed class IRBlock
        {
            internal uint VirtualAddressStart { get; }
            internal uint VirtualAddressEnd { get; }      // address of last ppc instruction
            internal List<IRInstruction> IRCode { get; }
            internal Dictionary<Value, ulong> Constants { get; }
            internal int IsSyscallExit { get; set; }

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