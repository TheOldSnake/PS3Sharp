namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRInstructions {
        public struct IRInstruction {
            public Op Op;

            public ulong Immediate1;
            public ulong Immediate2;
            public ulong Immediate3;

            public Value Operand1;
            public Value Operand2;
            public Value Operand3;
            public Value Destination;

            public static IRInstruction Create(Op op, Value destination, Value a = default, Value b = default, Value c = default, ulong constant1 = 0, ulong constant2 = 0, ulong constant3 = 0) {
                return new IRInstruction {
                    Op = op,
                    Destination = destination,
                    Operand1 = a,
                    Operand2 = b,
                    Operand3 = c,
                    Immediate1 = constant1,
                    Immediate2 = constant2,
                    Immediate3 = constant3,
                };
            }

            public static IRInstruction Create(Op op, Value destination, ulong constant1 = 0, ulong constant2 = 0, ulong constant3 = 0) {
                return new IRInstruction {
                    Op = op,
                    Destination = destination,
                    Immediate1 = constant1,
                    Immediate2 = constant2,
                    Immediate3 = constant3,
                };
            }
        }
    }
}
