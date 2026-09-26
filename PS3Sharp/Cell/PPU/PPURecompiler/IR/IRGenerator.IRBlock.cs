namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public partial class IRGenerator {
        public class IRBlock {
            public uint VirtualAddressStart;
            public uint VirtualAddressEnd;      // address of last ppc instruction
            public List<IRInstruction> IRCode;
            public Dictionary<Value, ulong> Constants;
            public int IsSyscallExit = 0;

            public IRBlock(uint startAddress, uint endAddress, List<IRInstruction> code, Dictionary<Value, ulong> constants, int isSyscallExit) {
                VirtualAddressStart = startAddress;
                VirtualAddressEnd = endAddress;
                IRCode = code;
                Constants = constants;
                IsSyscallExit = isSyscallExit;
            }
        }
    }
}

