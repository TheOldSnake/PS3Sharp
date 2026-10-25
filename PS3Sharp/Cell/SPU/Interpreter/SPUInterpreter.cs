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
          RR17(spu, instruction);
        }

        internal static void RR17(SPUThread spu, in SPUInstruction instruction)
        {
            switch (instruction.RR17Op)
            {

                default:
                    RI16(spu, instruction);
                    break;
            }
        }

        internal static void RI16(SPUThread spu, in SPUInstruction instruction)
        {
            switch (instruction.RI16Op)
            {

                default:
                    RI10(spu, instruction);
                    break;
            }
        }

        internal static void RI10(SPUThread spu, in SPUInstruction instruction)
        {
            switch (instruction.RI10Op)
            {

                default:
                    RI18(spu, instruction);
                    break;
            }
        }

        internal static void RI18(SPUThread spu, in SPUInstruction instruction)
        {
            switch (instruction.RI18Op)
            {

                default:
                    RRROp(spu, instruction);
                    break;
            }
        }

        internal static void RRROp(SPUThread spu, in SPUInstruction instruction)
        {
            switch (instruction.RRROp)
            {

                default:
                    throw new NotImplementedException($"[SPUInterpreter][{spu.ThreadName}] Unhandled instruction op = 0x{instruction.RR17Op:X} Raw = 0x{instruction.Raw:X8}");
            }
        }
    }
}
