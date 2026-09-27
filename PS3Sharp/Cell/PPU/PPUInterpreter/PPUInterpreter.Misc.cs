using PS3Sharp.Common;
using PS3Sharp.Syscall;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.PPU.PPU;

namespace PS3Sharp.Cell.PPU.PPUInterpreter
{
    public static unsafe partial class PPUInterpreter
    {

        //Syscall
        private static void SC(PPUInstruction instruction, PPU ppu)
        {
            SyscallHandler.HandleSyscall(ppu, instruction.LEV);
        }

        private static void MFSPR(PPUInstruction instruction, PPU ppu)
        {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);

            switch (spr)
            {
                case 0b0000000001: ppu.GPR[instruction.Rt].Value64 = ppu.State->XER.Raw; break;        //XER
                case 0b0000001000: ppu.GPR[instruction.Rt].Value64 = ppu.State->LR; break;             //LR
                case 0b0000001001: ppu.GPR[instruction.Rt].Value64 = ppu.State->CTR; break;            //CTR
                case 0b0100000000: ppu.GPR[instruction.Rt].Value64 = ppu.State->VRSAVE; break;         //VRSAVE
                default: throw new Exception($"Unknown SPR = {spr:B10}");
            }
        }

        private static void MTSPR(PPUInstruction instruction, PPU ppu)
        {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);

            switch (spr)
            {
                case 0b0000000001: ppu.State->XER.Raw = ppu.GPR[instruction.Rs].Value64; break;        //XER
                case 0b0000001000: ppu.State->LR = ppu.GPR[instruction.Rs].Value32; break;             //LR
                case 0b0000001001: ppu.State->CTR = ppu.GPR[instruction.Rs].Value64; break;            //CTR
                case 0b0100000000: ppu.State->VRSAVE = ppu.GPR[instruction.Rs].Value32; break;         //VRSAVE
                default: throw new Exception($"Unknown SPR = {spr:B10}");
            }
        }

        private static void MFCR(PPUInstruction instruction, PPU ppu)
        {
            /*bool isOneField = ((instruction.Raw >> 20) & 1) == 1;
            if (isOneField) {
                //mfocrf
                //how does it work?
                throw new Exception($"Unimplemented mfocrf");
            }*/

            //mfcr
            ppu.GPR[instruction.Rt].Value64 = ppu.State->CR.Raw;
        }


        private static void MTCRF(PPUInstruction instruction, PPU ppu)
        {
            bool isOneField = ((instruction.Raw >> 20) & 1) == 1;
            uint fxm = instruction.Fxm;
            uint rs = ppu.GPR[instruction.Rs].Value32;

            if (isOneField && uint.PopCount(fxm) > 1)
            {
                throw new Exception($"mtocrf with fxm containing more than one field");
            }

            for (int i = 0; i < 8; i++)
            {
                if (((fxm >> i) & 1) == 1)
                {
                    PPU.CRNumber cr = (PPU.CRNumber)(7 - i);
                    uint value = (rs >> (i * 4)) & 0xF;
                    ppu.State->CR.WriteCRField(cr, value);
                }
            }
        }

        private static void MCRF(PPUInstruction instruction, PPU ppu)
        {
            //CR_BF = CR_BFA
            uint bfa = ppu.State->CR.GetCRField((CRNumber)instruction.BFA);
            ppu.State->CR.WriteCRField((CRNumber)instruction.BF, bfa);
        }

        private static void MFTB(PPUInstruction instruction, PPU ppu)
        {
            uint spr = ((instruction.SPR & 0x1F) << 5) | ((instruction.SPR >> 5) & 0x1F);
            //ulong value = Utility.GetCurrentTickSince1970() * 80;
            //ulong value = ppu.TB.Value64;

            ulong value = Utility.ReadTime_Microseconds() * 80;

            switch (spr)
            {
                case 0x10C:
                    ppu.GPR[instruction.Rt].Value64 = value;
                    break;

                case 0x10D:
                    ppu.GPR[instruction.Rt].Value64 = value >> 32;
                    break;

                default:
                    throw new Exception($"[PPU] MFTB instruction unknown spr = {spr:X}");
            }
        }
    }
}
