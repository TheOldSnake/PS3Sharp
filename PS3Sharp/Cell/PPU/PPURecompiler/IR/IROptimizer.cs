using System.Numerics;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR
{
    public sealed class IROptimizer
    {
        public Dictionary<Value, ulong> ConstantFold(IRContext ctx)
        {
            Dictionary<Value, ulong> constants = new();
            List<IRInstruction> code = ctx.GetIRCode();
            List<IRInstruction> foldedCode = new();
            bool removed;

            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                Value dest = instruction.Destination;

                removed = false;

                // Skip special loads
                if (!IsSpecialRegisterLoad(instruction))
                {

                    if (instruction.Opcode == IRInstruction.Op.Constant)
                    {
                        // Add any destination of "Constant" instruction, the instruction can be removed
                        constants[dest] = instruction.Immediate1;
                        removed = true;

                    }
                    else
                    {
                        // Try to compute the destination as a constant, if succeeds the instruction can be removed
                        removed = TryComputeConstant(instruction, constants);
                    }

                }

                if (!removed)
                {
                    foldedCode.Add(instruction);
                }
            }

            ctx.SetIRCode(foldedCode);
            return constants;
        }

        public bool TryComputeConstant(IRInstruction instruction, Dictionary<Value, ulong> constants)
        {
            // Skip special loads
            if (IsSpecialRegisterLoad(instruction))
            {
                return false;
            }

            // Ensure that the instruction is a math/logic instruciton on 2 operands, with a valid destination
            bool isUsingTwoOperands = GetNumberOfValidOperands(instruction) == 2;
            if (!isUsingTwoOperands)
            {
                return false;
            }

            if (!instruction.Destination.IsValid)
            {
                return false;
            }

            // Check if both operands are constants
            Value op1 = instruction.Operand1;
            Value op2 = instruction.Operand2;
            bool operandsAreConstants = constants.ContainsKey(op1) && constants.ContainsKey(op2);

            if (!operandsAreConstants)
            {
                return false;
            }

            ulong result;
            switch (instruction.Opcode)
            {
                case IRInstruction.Op.Add:
                    result = constants[op1] + constants[op2];
                    break;

                case IRInstruction.Op.Sub:
                    result = constants[op1] - constants[op2];
                    break;

                case IRInstruction.Op.And:
                    result = constants[op1] & constants[op2];
                    break;

                case IRInstruction.Op.Or:
                    result = constants[op1] | constants[op2];
                    break;

                case IRInstruction.Op.SetEq:
                    result = constants[op1] == constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetNotEq:
                    result = constants[op1] != constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetLessThanEqSigned:
                    result = (long)constants[op1] <= (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetLessThanEqUnsigned:
                    result = constants[op1] <= constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetLessThanSigned:
                    result = (long)constants[op1] < (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetLessThanUnsigned:
                    result = constants[op1] < constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetGreaterThanEqSigned:
                    result = (long)constants[op1] >= (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetGreaterThanEqUnsigned:
                    result = constants[op1] >= constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetGreaterThanSigned:
                    result = (long)constants[op1] > (long)constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.SetGreaterThanUnsigned:
                    result = constants[op1] > constants[op2] ? 1U : 0U;
                    break;

                case IRInstruction.Op.RotateLeft:
                    result = BitOperations.RotateLeft(constants[op1], (int)constants[op2]);
                    break;

                default:
                    throw new Exception($"[IR Optimizer] ComputeConstant todo: {instruction.Opcode}");
            }

            // Add the constant 
            constants[instruction.Destination] = result;
            return true;
        }

        public void CopyPropagation(IRContext ctx, Dictionary<Value, ulong> knownConstants)
        {
            // Copy Propagation on any case where we have ADD value2, value1, 0 or ADD value2, 0, Value1
            // todo: generalize for more opcodes than ADD

            List<IRInstruction> code = ctx.GetIRCode();
            List<IRInstruction> propagatedCode = new();
            Dictionary<Value, Value> propagatedValues = new();

            // First pass
            FilterPropagation(code, propagatedCode, propagatedValues, knownConstants);

            // Second pass
            PropagateToInputs(propagatedCode, propagatedValues);

            ctx.SetIRCode(propagatedCode);
        }

        private void FilterPropagation(List<IRInstruction> inputCode, List<IRInstruction> outputCode, Dictionary<Value, Value> propagatedValues, Dictionary<Value, ulong> knownConstants)
        {
            // Remove instructions where propagation is possible, and add the info to the dictionary
            for (int i = 0; i < inputCode.Count; i++)
            {
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


                if (operandsCount == 2 && op1Const && op2Const)
                {
                    // this should not happen here, the constant folding phase should have eliminated this
                    throw new Exception($"[IR Optimizer] Unexpected instruction with 2 constant operands");
                }

                // TODO: Handle cleanly


                // Select is special case, we can optimize if we know the condition
                if (instruction.Opcode == IRInstruction.Op.Select && op1Const)
                {
                    // known condition => known propagation
                    propagatedValues[dest] = knownConstants[op1] != 0 ? op2 : op3;
                    removed = true;
                }

                if (instruction.Opcode == IRInstruction.Op.Add)
                {
                    if (op1Const && knownConstants[op1] == 0)
                    {
                        // op1 == 0, propagate op2 to dest, the instruction can be removed
                        propagatedValues[dest] = op2;
                        removed = true;

                    }
                    else if (op2Const && knownConstants[op2] == 0)
                    {
                        // op2 == 0, propagate op1 to dest, the instruction can be removed
                        propagatedValues[dest] = op1;
                        removed = true;
                    }
                }

                if (instruction.Opcode == IRInstruction.Op.ShiftLeft ||
                    instruction.Opcode == IRInstruction.Op.ShiftRight ||
                    instruction.Opcode == IRInstruction.Op.RotateLeft ||
                    instruction.Opcode == IRInstruction.Op.RotateRight)
                {

                    if (op2Const && knownConstants[op2] == 0)
                    {
                        // shift/rotate by 0, propagate op1 to dest, the instruction can be removed
                        propagatedValues[dest] = op1;
                        removed = true;
                    }
                }

                if (!removed)
                {
                    outputCode.Add(instruction);
                }
            }
        }

        private void PropagateToInputs(List<IRInstruction> code, Dictionary<Value, Value> propagatedValues)
        {
            // Replace inputs with the propagated values
            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];

                Value dest = instruction.Destination;
                Value op1 = instruction.Operand1;
                Value op2 = instruction.Operand2;

                bool destIsPropagated = propagatedValues.ContainsKey(dest);
                bool op1IsPropagated = propagatedValues.ContainsKey(op1) && op1.IsValid;
                bool op2IsPropagated = propagatedValues.ContainsKey(op2) && op2.IsValid;

                // This case is only valid for memory store, where the "destination" is an input 
                if (destIsPropagated)
                {
                    if (IsRegularStore(instruction) || IsSpecialRegisterStore(instruction))
                    {
                        instruction.Destination = propagatedValues[dest];
                    }
                }

                if (op1IsPropagated)
                {
                    instruction.Operand1 = propagatedValues[op1];
                }

                if (op2IsPropagated)
                {
                    instruction.Operand2 = propagatedValues[op2];
                }

                code[i] = instruction;
            }
        }

    }
}
