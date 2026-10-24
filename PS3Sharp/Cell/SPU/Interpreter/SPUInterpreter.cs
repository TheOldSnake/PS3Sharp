namespace PS3Sharp.Cell.SPU.Interpreter
{
    internal class SPUInterpreter
    {
        internal static void Step(SPUThread spu)
        {
            unsafe
            {
                SPUInstruction instruction = new()
                {
                    Raw = spu.MemoryManager.ReadSPUInstruction(spu.State->PC)
                };

                spu.State->PC += 4;
                HandleInstruction(spu, instruction);
            }
        }

        internal static void HandleInstruction(SPUThread spu, in SPUInstruction instruction)
        {
            switch (instruction.Op)
            {
                default: 
                    throw new NotImplementedException($"[SPUInterpreter][{spu.ThreadName}] Unhandled instruction op = 0x{instruction.Op:X} Raw = 0x{instruction.Raw:X8}");
            }
        }
    }
}
