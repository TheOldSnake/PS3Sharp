using static PS3Sharp.Cell.Ppu.Interpreter.PpuInterpreter;

namespace PS3Sharp.Cell.Ppu.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal void MTFSF(PpuInstruction instruction)
        {
            // Move To FPSCR
        }

        internal void MTFSFI(PpuInstruction instruction)
        {
            // Move To FPSCR
        }

        internal void MFFS(PpuInstruction instruction)
        {
            // Move from FPSCR
            Value zero = _irContext.Zero; // need to be float/double zero
            _irContext.SetFPR(instruction.Frt, zero);
        }

        internal void FCMPU(PpuInstruction instruction)
        {
            Value a = _irContext.GetFPR(instruction.Fra);
            Value b = _irContext.GetFPR(instruction.Frb);
            uint bf = (instruction.Raw >> 23) & 0x7;

            _irContext.UpdateCR(a, b, (PpuThread.CrNumber)bf); // need to compare floats.. 
        }

        internal void FCMPO(PpuInstruction instruction)
        {
            FCMPU(instruction); // Same thing?
        }

        internal void LFS(PpuInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);

            Value v = Load32(ea); // load float
                                  // convert to long 

            _irContext.SetFPR(instruction.Frt, v); // then write
        }
    }
}
