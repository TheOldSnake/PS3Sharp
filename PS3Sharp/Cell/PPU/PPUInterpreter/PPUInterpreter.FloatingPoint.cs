using PS3Sharp.Memory_Manager;
using System.Runtime.CompilerServices;

namespace PS3Sharp.Cell.PPU.PPUInterpreter
{
    internal static unsafe partial class PPUInterpreter
    {
        private static void Group3B(PPUInstruction instruction, PPU ppu)
        {
            //Single float instructions
            //Console.WriteLine($"FPU S: {instruction.Group3BOp:X}");

            switch (instruction.G3B)
            {
                case G3B.FDIVS: FDIVS(instruction, ppu); break;
                case G3B.FSUBS: FSUBS(instruction, ppu); break;
                case G3B.FADDS: FADDS(instruction, ppu); break;
                case G3B.FSQRTS: FSQRTS(instruction, ppu); break;
                case G3B.FRES: FRES(instruction, ppu); break;
                case G3B.FMULS: FMULS(instruction, ppu); break;
                case G3B.FRSQRTES: FRSQRTES(instruction, ppu); break;
                case G3B.FMSUBS: FMSUBS(instruction, ppu); break;
                case G3B.FMADDS: FMADDS(instruction, ppu); break;
                case G3B.FNMSUBS: FNMSUBS(instruction, ppu); break;
                case G3B.FNMADDS: FNMADDS(instruction, ppu); break;

                default: throw new Exception($"Unimplemented Opcode 0x3B Field = 0x{instruction.G3B:X} = {instruction.G3B} -- raw = 0x{instruction.Raw:X8}");
            }
        }

        internal static void Group3F(PPUInstruction instruction, PPU ppu)
        {
            //Double float instructions
            //Console.WriteLine($"FPU D : {instruction.Group3FOp:X}");

            // First check for instructions with FRC field (where opcode is 5 bits only)

            switch ((G3F)((uint)instruction.G3F & 0x1F))
            {
                case G3F.FSEL: FSEL(instruction, ppu); break;
                case G3F.FMUL: FMUL(instruction, ppu); break;
                case G3F.FRE: FRE(instruction, ppu); break;
                case G3F.FRSQRTE: FRSQRTE(instruction, ppu); break;
                case G3F.FMSUB: FMSUB(instruction, ppu); break;
                case G3F.FMADD: FMADD(instruction, ppu); break;
                case G3F.FNMSUB: FNMSUB(instruction, ppu); break;
                case G3F.FNMADD: FNMADD(instruction, ppu); break;

                default:

                    // Then check the rest
                    switch (instruction.G3F)
                    {
                        case G3F.FCMPU: FCMPU(instruction, ppu); break;
                        case G3F.FRSP: FRSP(instruction, ppu); break;
                        case G3F.FCTIW: FCTIW(instruction, ppu); break;
                        case G3F.FCTIWZ: FCTIWZ(instruction, ppu); break;

                        case G3F.FDIV: FDIV(instruction, ppu); break;
                        case G3F.FSUB: FSUB(instruction, ppu); break;
                        case G3F.FADD: FADD(instruction, ppu); break;
                        case G3F.FSQRT: FSQRT(instruction, ppu); break;
                        case G3F.FCMPO: FCMPO(instruction, ppu); break;
                        case G3F.FNEG: FNEG(instruction, ppu); break;

                        case G3F.MTFSFI: MTFSFI(instruction, ppu); break;
                        case G3F.FMR: FMR(instruction, ppu); break;
                        case G3F.FNABS: FNABS(instruction, ppu); break;
                        case G3F.FABS: FABS(instruction, ppu); break;
                        case G3F.MFFS: MFFS(instruction, ppu); break;
                        case G3F.MTFSF: MTFSF(instruction, ppu); break;

                        case G3F.FCTID: FCTID(instruction, ppu); break;
                        case G3F.FCTIDZ: FCTIDZ(instruction, ppu); break;
                        case G3F.FCFID: FCFID(instruction, ppu); break;

                        default: throw new Exception($"Unimplemented Opcode 0x3F Field = 0x{instruction.G3F:X} = {instruction.G3F} raw = {instruction.Raw:X}");
                    }

                    break;
            }
        }

        internal static void PrintAllFPRs(PPU ppu)
        {
            for (int i = 0; i < 32; i++)
            {
                Console.WriteLine($"FPR[{i}] = {ppu.FPR[i].Float64}");
            }
        }

        internal static void MTFSF(PPUInstruction instruction, PPU ppu)
        {
            // Move To FPSCR
        }

        internal static void MTFSFI(PPUInstruction instruction, PPU ppu)
        {
            // Move To FPSCR
        }

        internal static void MFFS(PPUInstruction instruction, PPU ppu)
        {
            // Move from FPSCR
            ppu.FPR[instruction.Frt].Value64 = 0;
        }

        internal static void FCMPU(PPUInstruction instruction, PPU ppu)
        {
            double a = ppu.FPR[instruction.Fra].Float64;
            double b = ppu.FPR[instruction.Frb].Float64;
            uint bf = (instruction.Raw >> 23) & 0x7;
            UpdateCRRegister(a, b, (PPU.CRNumber)bf, ppu);
            //Console.WriteLine($"FCMPU [{instruction.Fra}] vs [{instruction.Frb}] = {a} vs {b} -- Cr: {bf} -- pc: {ppu.PC:X8}");
        }

        internal static void FCMPO(PPUInstruction instruction, PPU ppu)
        {
            FCMPU(instruction, ppu); // Same thing?
        }

        internal static void LFS(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            uint v = MemoryManager.Read<uint>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
        }

        internal static void LFSU(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            uint v = MemoryManager.Read<uint>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        internal static void LFSX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint v = MemoryManager.Read<uint>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
        }

        internal static void LFSUX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            uint v = MemoryManager.Read<uint>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        //in store float instructions we need to cast double -> float
        //(this does no equal taking first 32-bit as float!!)
        internal static void STFS(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            float frs = (float)ppu.FPR[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            MemoryManager.Write<uint>(ea, v);
        }

        internal static void STFSX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            float frs = (float)ppu.FPR[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            MemoryManager.Write<uint>(ea, v);
        }

        internal static void STFSU(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            float frs = (float)ppu.FPR[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            MemoryManager.Write<uint>(ea, v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        internal static void STFSUX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            float frs = (float)ppu.FPR[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            MemoryManager.Write<uint>(ea, v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        internal static void STFIWX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint frs = (uint)(ppu.FPR[instruction.Frs].Value64 & 0xFFFFFFFF);
            MemoryManager.Write<uint>(ea, frs);
        }

        internal static void LFD(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ulong v = MemoryManager.Read<ulong>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
        }

        internal static void LFDX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ulong v = MemoryManager.Read<ulong>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
        }

        internal static void LFDU(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ulong v = MemoryManager.Read<ulong>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        internal static void LFDUX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ulong v = MemoryManager.Read<ulong>(ea);
            ppu.FPR[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        //in store float instructions we need to cast double -> float
        //(this does no equal taking first 32-bit as float!!)
        internal static void STFD(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            double frs = (double)ppu.FPR[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            MemoryManager.Write<ulong>(ea, v);
        }

        internal static void STFDX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            double frs = (double)ppu.FPR[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            MemoryManager.Write<ulong>(ea, v);
        }

        internal static void STFDU(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            double frs = (double)ppu.FPR[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            MemoryManager.Write<ulong>(ea, v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        internal static void STFDUX(PPUInstruction instruction, PPU ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            double frs = (double)ppu.FPR[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            MemoryManager.Write<ulong>(ea, v);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        internal static void FMR(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Value64 = ppu.FPR[instruction.Frb].Value64;
            //Console.WriteLine($"FMR [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FADD(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = ppu.FPR[instruction.Fra].Float64 + ppu.FPR[instruction.Frb].Float64;
            //Console.WriteLine($"FADD [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FADDS(PPUInstruction instruction, PPU ppu)
        {
            float result = (float)ppu.FPR[instruction.Fra].Float64 + (float)ppu.FPR[instruction.Frb].Float64;
            ppu.FPR[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FADDS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FSUB(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = ppu.FPR[instruction.Fra].Float64 - ppu.FPR[instruction.Frb].Float64;
            //Console.WriteLine($"FSUB [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FSUBS(PPUInstruction instruction, PPU ppu)
        {
            float result = (float)ppu.FPR[instruction.Fra].Float64 - (float)ppu.FPR[instruction.Frb].Float64;
            ppu.FPR[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FSUBS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FMUL(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64;
            //Console.WriteLine($"FMUL [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FMULS(PPUInstruction instruction, PPU ppu)
        {
            float result = (float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64;
            ppu.FPR[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FMULS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FDIV(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = ppu.FPR[instruction.Fra].Float64 / ppu.FPR[instruction.Frb].Float64;
            //Console.WriteLine($"FDIV [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FDIVS(PPUInstruction instruction, PPU ppu)
        {
            float result = (float)ppu.FPR[instruction.Fra].Float64 / (float)ppu.FPR[instruction.Frb].Float64;
            ppu.FPR[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FDIVS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FMADD(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = double.FusedMultiplyAdd(ppu.FPR[instruction.Fra].Float64, ppu.FPR[instruction.Frc].Float64, ppu.FPR[instruction.Frb].Float64);

            //ppu.FPR[instruction.Frt].Float64 = (ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) + ppu.FPR[instruction.Frb].Float64;        
            //Console.WriteLine($"FMADD [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FMADDS(PPUInstruction instruction, PPU ppu)
        {
            float result = float.FusedMultiplyAdd((float)ppu.FPR[instruction.Fra].Float64, (float)ppu.FPR[instruction.Frc].Float64, (float)ppu.FPR[instruction.Frb].Float64);

            //float result = ((float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64) + (float)ppu.FPR[instruction.Frb].Float64;

            ppu.FPR[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FMADDS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FMSUB(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = double.FusedMultiplyAdd(ppu.FPR[instruction.Fra].Float64, ppu.FPR[instruction.Frc].Float64, -ppu.FPR[instruction.Frb].Float64);

            //ppu.FPR[instruction.Frt].Float64 = (ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) - ppu.FPR[instruction.Frb].Float64;          
            //Console.WriteLine($"FMADD [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FMSUBS(PPUInstruction instruction, PPU ppu)
        {
            float result = float.FusedMultiplyAdd((float)ppu.FPR[instruction.Fra].Float64, (float)ppu.FPR[instruction.Frc].Float64, -(float)ppu.FPR[instruction.Frb].Float64);

            // float result = ((float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64) - (float)ppu.FPR[instruction.Frb].Float64;

            ppu.FPR[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FMADDS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FNMADD(PPUInstruction instruction, PPU ppu)
        {
            //ppu.FPR[instruction.Frt].Float64 = -((ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) + ppu.FPR[instruction.Frb].Float64);
            ppu.FPR[instruction.Frt].Float64 = -double.FusedMultiplyAdd(ppu.FPR[instruction.Fra].Float64, ppu.FPR[instruction.Frc].Float64, ppu.FPR[instruction.Frb].Float64); ;
        }

        internal static void FNMADDS(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = -float.FusedMultiplyAdd((float)ppu.FPR[instruction.Fra].Float64, (float)ppu.FPR[instruction.Frc].Float64, (float)ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FNMSUB(PPUInstruction instruction, PPU ppu)
        {
            // Frt = - (Fra * Frc - Frb) 
            // Exapnds to Frt = Frb + (-Fra * Frc)

            ppu.FPR[instruction.Frt].Float64 = double.FusedMultiplyAdd(-ppu.FPR[instruction.Fra].Float64, ppu.FPR[instruction.Frc].Float64, ppu.FPR[instruction.Frb].Float64);
            //ppu.FPR[instruction.Frt].Float64 = -((ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) - ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FNMSUBS(PPUInstruction instruction, PPU ppu)
        {
            // Frt = - (Fra * Frc - Frb) 
            // Exapnds to Frt = Frb + (-Fra * Frc)

            ppu.FPR[instruction.Frt].Float64 = float.FusedMultiplyAdd(-(float)ppu.FPR[instruction.Fra].Float64, (float)ppu.FPR[instruction.Frc].Float64, (float)ppu.FPR[instruction.Frb].Float64);
            //ppu.FPR[instruction.Frt].Float64 = -(((float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64) - (float)ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FSQRT(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = double.Sqrt(ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FSQRTS(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = float.Sqrt((float)ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FRSQRTE(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = double.ReciprocalSqrtEstimate(ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FRSQRTES(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = float.ReciprocalSqrtEstimate((float)ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FRE(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = double.ReciprocalEstimate(ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FRES(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = float.ReciprocalEstimate((float)ppu.FPR[instruction.Frb].Float64);
        }

        internal static void FNEG(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = -(ppu.FPR[instruction.Frb].Float64);
            //Console.WriteLine($"FNEG [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FABS(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = double.Abs(ppu.FPR[instruction.Frb].Float64);
            //Console.WriteLine($"FNEG [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FNABS(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = -(double.Abs(ppu.FPR[instruction.Frb].Float64));
            //Console.WriteLine($"FNEG [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FRSP(PPUInstruction instruction, PPU ppu)
        {
            ppu.FPR[instruction.Frt].Float64 = (float)ppu.FPR[instruction.Frb].Float64;
            //Console.WriteLine($"FRSP [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FCFID(PPUInstruction instruction, PPU ppu)
        {
            //long -> double
            ppu.FPR[instruction.Frt].Float64 = (double)(ppu.FPR[instruction.Frb].SValue64);
            //Console.WriteLine($"FCFID [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FCTID(PPUInstruction instruction, PPU ppu)
        {
            FCTIDZ(instruction, ppu); //We only support rounding toward zero..
        }

        internal static void FCTIDZ(PPUInstruction instruction, PPU ppu)
        {
            //double -> long
            ppu.FPR[instruction.Frt].SValue64 = (long)(ppu.FPR[instruction.Frb].Float64);
            //Console.WriteLine($"FCFID [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void FCTIW(PPUInstruction instruction, PPU ppu)
        {
            //We only support rounding toward zero..
            FCTIWZ(instruction, ppu);
        }

        internal static void FCTIWZ(PPUInstruction instruction, PPU ppu)
        {
            long frb = (long)ppu.FPR[instruction.Frb].Float64;
            frb = Math.Clamp(frb, int.MinValue, int.MaxValue);

            //ppu.FPR[instruction.Frt].Value64 = (uint)frb;

            ppu.FPR[instruction.Frt].Value64 = (ulong)frb | 0xfff8000000000000;

            //Console.WriteLine($"FCTIWZ [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Value64}");
        }

        internal static void FSEL(PPUInstruction instruction, PPU ppu)
        {
            double fra = ppu.FPR[instruction.Fra].Float64;
            double frb = ppu.FPR[instruction.Frb].Float64;
            double frc = ppu.FPR[instruction.Frc].Float64;
            double result = fra >= 0.0 ? frc : frb;
            ppu.FPR[instruction.Frt].Float64 = result;
        }
    }
}