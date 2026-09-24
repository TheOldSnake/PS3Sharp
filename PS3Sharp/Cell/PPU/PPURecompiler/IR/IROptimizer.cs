using OpenTK.Graphics.OpenGL;
using System.Numerics;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static class IROptimizer {
        public static bool IsRegularStore(IRInstruction instruction) {
            bool isStore = 
                instruction.Op == IRInstructions.Op.Store8      ||
                instruction.Op == IRInstructions.Op.Store16     ||
                instruction.Op == IRInstructions.Op.Store16Rev  ||
                instruction.Op == IRInstructions.Op.Store32     ||
                instruction.Op == IRInstructions.Op.Store32Rev  ||
                instruction.Op == IRInstructions.Op.Store32CX   ||
                instruction.Op == IRInstructions.Op.Store64     ||
                instruction.Op == IRInstructions.Op.Store64CX   || 
                instruction.Op == IRInstructions.Op.Store128;

            return isStore;
        }

        public static bool IsSpecialRegisterStore(IRInstruction instruction) {
            bool isStore =
                instruction.Op == IRInstructions.Op.GPRStore    ||
                instruction.Op == IRInstructions.Op.FPRStore    ||
                instruction.Op == IRInstructions.Op.CRStore     ||
                instruction.Op == IRInstructions.Op.CTRStore    ||
                instruction.Op == IRInstructions.Op.XERStore    ||
                instruction.Op == IRInstructions.Op.LRStore     ||
                instruction.Op == IRInstructions.Op.VRSaveStore ||
                instruction.Op == IRInstructions.Op.PCStore;


            return isStore;
        }

        public static bool IsSpecialRegisterLoad(IRInstruction instruction) {
            bool isStore =
                instruction.Op == IRInstructions.Op.GPRLoad ||
                instruction.Op == IRInstructions.Op.FPRLoad ||
                instruction.Op == IRInstructions.Op.CRLoad  ||
                instruction.Op == IRInstructions.Op.CTRLoad ||
                instruction.Op == IRInstructions.Op.XERLoad ||
                instruction.Op == IRInstructions.Op.LRLoad  ||
                instruction.Op == IRInstructions.Op.VRSaveLoad;


            return isStore;
        }

        public static int GetNumberOfValidOperands(IRInstruction instruction) {
            int operands = 0;

            if (instruction.Operand1.IsValid) {
                operands++;
            }

            if (instruction.Operand2.IsValid) {
                operands++;
            }

            if (instruction.Operand3.IsValid) {
                operands++;
            }

            return operands;
        }

        public static Dictionary<Value, ulong> ConstantFold(IRContext ctx) {
            Dictionary<Value, ulong> constants = new Dictionary<Value, ulong>();
            List<IRInstruction> code = ctx.GetIRCode();
            List<IRInstruction> foldedCode = new();
            bool removed;

            for (int i = 0; i < code.Count; i++) {
                IRInstruction instruction = code[i];
                Value dest = instruction.Destination;

                removed = false;

                // Skip special loads
                if (!IsSpecialRegisterLoad(instruction)) {

                    if (instruction.Op == IRInstructions.Op.Constant) {
                        // Add any destination of "Constant" instruction, the instruction can be removed
                        constants[dest] = instruction.Immediate1;
                        removed = true;

                    } else {
                        // Try to compute the destination as a constant, if succeeds the instruction can be removed
                        removed = TryComputeConstant(instruction, constants);
                    }

                }

                if (!removed) {
                    foldedCode.Add(instruction);
                }
            }

            ctx.SetIRCode(foldedCode);
            return constants;
        }

        public static bool TryComputeConstant(IRInstruction instruction, Dictionary<Value, ulong> constants) {
            // Skip special loads
            if (IsSpecialRegisterLoad(instruction)) {
                return false;
            }

            // Ensure that the instruction is a math/logic instruciton on 2 operands, with a valid destination
            bool isUsingTwoOperands = GetNumberOfValidOperands(instruction) == 2;
            if (!isUsingTwoOperands) {
                return false;
            }

            if (!instruction.Destination.IsValid) {
                return false;
            }

            // Check if both operands are constants
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;
            bool operandsAreConstants = constants.ContainsKey(op1) && constants.ContainsKey(op2);

            if (!operandsAreConstants){
                return false;
            }

            ulong result;
            switch (instruction.Op) {
                case IRInstructions.Op.Add:
                    result = constants[op1] + constants[op2];
                    break;

                case IRInstructions.Op.Sub:
                    result = constants[op1] - constants[op2];
                    break;

                case IRInstructions.Op.And:
                    result = constants[op1] & constants[op2];
                    break;

                case IRInstructions.Op.Or:
                    result = constants[op1] | constants[op2];
                    break;

                case IRInstructions.Op.SetEq:
                    result = constants[op1] == constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetNotEq:
                    result = constants[op1] != constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetLessThanEqSigned:
                    result = (long)constants[op1] <= (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetLessThanEqUnsigned:
                    result = constants[op1] <= constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetLessThanSigned:
                    result = (long)constants[op1] < (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetLessThanUnsigned:
                    result = constants[op1] < constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetGreaterThanEqSigned:
                    result = (long)constants[op1] >= (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetGreaterThanEqUnsigned:
                    result = constants[op1] >= constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetGreaterThanSigned:
                    result = (long)constants[op1] > (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.SetGreaterThanUnsigned:
                    result = constants[op1] > constants[op2] ? 1U : 0U;
                    break;

                case IRInstructions.Op.RotateLeft:
                    result = BitOperations.RotateLeft(constants[op1], (int)constants[op2]);
                    break;

                default:
                    throw new Exception($"[IR Optimizer] ComputeConstant todo: {instruction.Op}");
            }

            // Add the constant 
            constants[instruction.Destination] = result;
            return true;
        }

        public static void CopyPropagation(IRContext ctx, Dictionary<Value, ulong> knownConstants) {
            // Copy Propagation on any case where we have ADD value2, value1, 0 or ADD value2, 0, Value1
            // todo: generalize for more opcodes than ADD

            List<IRInstruction> code = ctx.GetIRCode();
            List<IRInstruction> propagatedCode = new();
            Dictionary<Value, Value> propagatedValues = new Dictionary<Value, Value>();

            // First pass
            FilterPropagation(code, propagatedCode, propagatedValues, knownConstants);

            // Second pass
            PropagateToInputs(propagatedCode, propagatedValues);

            ctx.SetIRCode(propagatedCode);
        }

        private static void FilterPropagation(List<IRInstruction> inputCode, List<IRInstruction> outputCode, Dictionary<Value, Value> propagatedValues, Dictionary<Value, ulong> knownConstants) {
            // Remove instructions where propagation is possible, and add the info to the dictionary
            for (int i = 0; i < inputCode.Count; i++) {
                IRInstruction instruction = inputCode[i];
                bool removed = false;
                int operandsCount = GetNumberOfValidOperands(instruction);
                Value dest = instruction.Destination;
                Value op1 = instruction.Operand1;
                Value op2 = instruction.Operand2;
                Value op3 = instruction.Operand3;

                bool op1Const = knownConstants.ContainsKey(op1);
                bool op2Const = knownConstants.ContainsKey(op2);
                bool op3Const = knownConstants.ContainsKey(op3);


                if (op1Const && op2Const) {
                    // this should not happen here, the constant folding phase should have eliminated this
                    throw new Exception($"[IR Optimizer] Unexpected instruction with 2 constant operands");
                }

                // TODO: Handle cleanly


                // Select is special case, we can optimize if we know the condition
                if (instruction.Op == IRInstructions.Op.Select && op1Const) {
                    // known condition => known propagation
                    propagatedValues[dest] = knownConstants[op1] != 0 ? op2 : op3;
                    removed = true;
                }

                if (instruction.Op == IRInstructions.Op.Add) {     
                    if (op1Const && knownConstants[op1] == 0) {
                        // op1 == 0, propagate op2 to dest, the instruction can be removed
                        propagatedValues[dest] = op2;
                        removed = true;

                    } else if (op2Const && knownConstants[op2] == 0) {
                        // op2 == 0, propagate op1 to dest, the instruction can be removed
                        propagatedValues[dest] = op1;
                        removed = true;
                    }
                }

                if (instruction.Op == IRInstructions.Op.ShiftLeft || 
                    instruction.Op == IRInstructions.Op.ShiftRight ||
                    instruction.Op == IRInstructions.Op.RotateLeft ||
                    instruction.Op == IRInstructions.Op.RotateRight ) {

                    if (op2Const && knownConstants[op2] == 0) {
                        // shift/rotate by 0, propagate op1 to dest, the instruction can be removed
                        propagatedValues[dest] = op1;
                        removed = true;
                    }
                }

                if (!removed) {
                    outputCode.Add(instruction);
                }
            }
        }

        private static void PropagateToInputs(List<IRInstruction> code, Dictionary<Value, Value> propagatedValues) {
            // Replace inputs with the propagated values
            for (int i = 0; i < code.Count; i++) {
                IRInstruction instruction = code[i];

                Value dest = instruction.Destination;
                Value op1 = instruction.Operand1;
                Value op2 = instruction.Operand2;

                bool destIsPropagated = propagatedValues.ContainsKey(dest);
                bool op1IsPropagated = propagatedValues.ContainsKey(op1) && op1.IsValid;
                bool op2IsPropagated = propagatedValues.ContainsKey(op2) && op2.IsValid;

                // This case is only valid for memory store, where the "destination" is an input 
                if (destIsPropagated) {
                    if (IsRegularStore(instruction) || IsSpecialRegisterStore(instruction)) {
                        instruction.Destination = propagatedValues[dest];
                    }
                }

                if (op1IsPropagated) {
                    instruction.Operand1 = propagatedValues[op1];
                }

                if (op2IsPropagated) {
                    instruction.Operand2 = propagatedValues[op2];
                }

                code[i] = instruction;
            }
        }

    }
}
