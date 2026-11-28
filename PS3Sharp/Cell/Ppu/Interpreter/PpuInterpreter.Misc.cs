using PS3Sharp.Common;
using static PS3Sharp.Cell.Ppu.PpuThread;

namespace PS3Sharp.Cell.Ppu.Interpreter
{
    internal static unsafe partial class PpuInterpreter
    {

        //Syscall
        private static void Sc(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.SyscallDispatcher.DispatchSyscall(ppu, instruction.Lev);
        }

        private static void Mfspr(PpuInstruction instruction, PpuThread ppu)
        {
            uint spr = ((instruction.Spr & 0x1F) << 5) | ((instruction.Spr >> 5) & 0x1F);

            switch (spr)
            {
                case 0b0000000001: ppu.Gpr[instruction.Rt].Value64 = ppu.State->Xer.Raw; break;        //XER
                case 0b0000001000: ppu.Gpr[instruction.Rt].Value64 = ppu.State->LR; break;             //LR
                case 0b0000001001: ppu.Gpr[instruction.Rt].Value64 = ppu.State->Ctr; break;            //CTR
                case 0b0100000000: ppu.Gpr[instruction.Rt].Value64 = ppu.State->VrSave; break;         //VRSAVE
                default: throw new Exception($"Unknown SPR = {spr:B10}");
            }
        }

        private static void Mtspr(PpuInstruction instruction, PpuThread ppu)
        {
            uint spr = ((instruction.Spr & 0x1F) << 5) | ((instruction.Spr >> 5) & 0x1F);

            switch (spr)
            {
                case 0b0000000001: ppu.State->Xer.Raw = ppu.Gpr[instruction.Rs].Value64; break;        //XER
                case 0b0000001000: ppu.State->LR = ppu.Gpr[instruction.Rs].Value32; break;             //LR
                case 0b0000001001: ppu.State->Ctr = ppu.Gpr[instruction.Rs].Value64; break;            //CTR
                case 0b0100000000: ppu.State->VrSave = ppu.Gpr[instruction.Rs].Value32; break;         //VRSAVE
                default: throw new Exception($"Unknown SPR = {spr:B10}");
            }
        }

        private static void Mfcr(PpuInstruction instruction, PpuThread ppu)
        {
            /*bool isOneField = ((instruction.Raw >> 20) & 1) == 1;
            if (isOneField) {
                //mfocrf
                //how does it work?
                throw new Exception($"Unimplemented mfocrf");
            }*/

            //mfcr
            ppu.Gpr[instruction.Rt].Value64 = ppu.State->CR.Raw;
        }


        private static void Mtcrf(PpuInstruction instruction, PpuThread ppu)
        {
            bool isOneField = ((instruction.Raw >> 20) & 1) == 1;
            uint fxm = instruction.Fxm;
            uint rs = ppu.Gpr[instruction.Rs].Value32;

            if (isOneField && uint.PopCount(fxm) > 1)
            {
                throw new Exception($"mtocrf with fxm containing more than one field");
            }

            for (int i = 0; i < 8; i++)
            {
                if (((fxm >> i) & 1) == 1)
                {
                    PpuThread.CrNumber cr = (PpuThread.CrNumber)(7 - i);
                    uint value = (rs >> (i * 4)) & 0xF;
                    ppu.State->CR.WriteCrField(cr, value);
                }
            }
        }

        private static void Mcrf(PpuInstruction instruction, PpuThread ppu)
        {
            //CR_BF = CR_BFA
            uint bfa = ppu.State->CR.GetCrField((CrNumber)instruction.BFA);
            ppu.State->CR.WriteCrField((CrNumber)instruction.Bf, bfa);
        }

        private static void Mftb(PpuInstruction instruction, PpuThread ppu)
        {
            uint spr = ((instruction.Spr & 0x1F) << 5) | ((instruction.Spr >> 5) & 0x1F);
            //ulong value = Utility.GetCurrentTickSince1970() * 80;
            //ulong value = ppu.TB.Value64;

            ulong value = Utility.ReadTime_Microseconds() * 80;

            switch (spr)
            {
                case 0x10C:
                    ppu.Gpr[instruction.Rt].Value64 = value;
                    break;

                case 0x10D:
                    ppu.Gpr[instruction.Rt].Value64 = value >> 32;
                    break;

                default:
                    throw new Exception($"[PPU] MFTB instruction unknown spr = {spr:X}");
            }
        }
    }
}
