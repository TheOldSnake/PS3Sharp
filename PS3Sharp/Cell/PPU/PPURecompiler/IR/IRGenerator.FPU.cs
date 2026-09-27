using System.Runtime.CompilerServices;
using static PS3Sharp.Cell.PPU.PPUInterpreter.PPUInterpreter;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR
{
    public sealed partial class IRGenerator
    {
        public void MTFSF(PPUInstruction instruction)
        {
            // Move To FPSCR
        }

        public void MTFSFI(PPUInstruction instruction)
        {
            // Move To FPSCR
        }

        public void MFFS(PPUInstruction instruction)
        {
            // Move from FPSCR
            Value zero = _irContext.Zero; // need to be float/double zero
            _irContext.SetFPR(instruction.Frt, zero);
        }

        public void FCMPU(PPUInstruction instruction)
        {
            Value a = _irContext.GetFPR(instruction.Fra);
            Value b = _irContext.GetFPR(instruction.Frb);
            uint bf = (instruction.Raw >> 23) & 0x7;

            _irContext.UpdateCR(a, b, (PPU.CRNumber)bf); // need to compare floats.. 
        }

        public void FCMPO(PPUInstruction instruction)
        {
            FCMPU(instruction); // Same thing?
        }

        public void LFS(PPUInstruction instruction)
        {
            Value ea = GetEa(instruction, MemAccessForm.DForm, true);

            Value v = Load32(ea); // load float
                                  // convert to long 

            _irContext.SetFPR(instruction.Frt, v); // then write
        }
    }
}
