using PS3Sharp.Common;
using System.Runtime.CompilerServices;

namespace PS3Sharp.Cell.Ppu.Interpreter
{
    internal static unsafe partial class PpuInterpreter
    {
        internal static void PrintAllFPRs(PpuThread ppu)
        {
            for (int i = 0; i < 32; i++)
            {
                Console.WriteLine($"FPR[{i}] = {ppu.Fpr[i].Float64}");
            }
        }

        internal static void Mtfsf(PpuInstruction instruction, PpuThread ppu)
        {
            // Move To FPSCR
        }

        internal static void Mtfsfi(PpuInstruction instruction, PpuThread ppu)
        {
            // Move To FPSCR
        }

        internal static void Mffs(PpuInstruction instruction, PpuThread ppu)
        {
            // Move from FPSCR
            ppu.Fpr[instruction.Frt].Value64 = 0;
        }

        internal static void Fcmpu(PpuInstruction instruction, PpuThread ppu)
        {
            double a = ppu.Fpr[instruction.Fra].Float64;
            double b = ppu.Fpr[instruction.Frb].Float64;
            uint bf = (instruction.Raw >> 23) & 0x7;
            UpdateCRRegister(a, b, (PpuThread.CrNumber)bf, ppu);
            //Console.WriteLine($"FCMPU [{instruction.Fra}] vs [{instruction.Frb}] = {a} vs {b} -- Cr: {bf} -- pc: {ppu.PC:X8}");
        }

        internal static void Fcmpo(PpuInstruction instruction, PpuThread ppu)
        {
            Fcmpu(instruction, ppu); // Same thing?
        }

        internal static void Lfs(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            uint v = ppu.MemoryManager.Read<BigEndian<uint>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
        }

        internal static void Lfsu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            uint v = ppu.MemoryManager.Read<BigEndian<uint>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        internal static void Lfsx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint v = ppu.MemoryManager.Read<BigEndian<uint>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
        }

        internal static void Lfsux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            uint v = ppu.MemoryManager.Read<BigEndian<uint>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<uint, float>(v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        //in store float instructions we need to cast double -> float
        //(this does no equal taking first 32-bit as float!!)
        internal static void Stfs(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            float frs = (float)ppu.Fpr[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, v);
        }

        internal static void Stfsx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            float frs = (float)ppu.Fpr[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, v);
        }

        internal static void Stfsu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            float frs = (float)ppu.Fpr[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        internal static void Stfsux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            float frs = (float)ppu.Fpr[instruction.Frs].Float64;
            uint v = Unsafe.BitCast<float, uint>(frs);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        internal static void Stfiwx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint frs = (uint)(ppu.Fpr[instruction.Frs].Value64 & 0xFFFFFFFF);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, frs);
        }

        internal static void Lfd(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ulong v = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
        }

        internal static void Lfdx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ulong v = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
        }

        internal static void Lfdu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ulong v = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        internal static void Lfdux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ulong v = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
            ppu.Fpr[instruction.Frt].Float64 = Unsafe.BitCast<ulong, double>(v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        //in store float instructions we need to cast double -> float
        //(this does no equal taking first 32-bit as float!!)
        internal static void Stfd(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            double frs = (double)ppu.Fpr[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, v);
        }

        internal static void Stfdx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            double frs = (double)ppu.Fpr[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, v);
        }

        internal static void Stfdu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            double frs = (double)ppu.Fpr[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        internal static void Stfdux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            double frs = (double)ppu.Fpr[instruction.Frs].Float64;
            ulong v = Unsafe.BitCast<double, ulong>(frs);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, v);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        internal static void Fmr(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Value64 = ppu.Fpr[instruction.Frb].Value64;
            //Console.WriteLine($"FMR [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fadd(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = ppu.Fpr[instruction.Fra].Float64 + ppu.Fpr[instruction.Frb].Float64;
            //Console.WriteLine($"FADD [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fadds(PpuInstruction instruction, PpuThread ppu)
        {
            float result = (float)ppu.Fpr[instruction.Fra].Float64 + (float)ppu.Fpr[instruction.Frb].Float64;
            ppu.Fpr[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FADDS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fsub(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = ppu.Fpr[instruction.Fra].Float64 - ppu.Fpr[instruction.Frb].Float64;
            //Console.WriteLine($"FSUB [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fsubs(PpuInstruction instruction, PpuThread ppu)
        {
            float result = (float)ppu.Fpr[instruction.Fra].Float64 - (float)ppu.Fpr[instruction.Frb].Float64;
            ppu.Fpr[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FSUBS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fmul(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = ppu.Fpr[instruction.Fra].Float64 * ppu.Fpr[instruction.Frc].Float64;
            //Console.WriteLine($"FMUL [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fmuls(PpuInstruction instruction, PpuThread ppu)
        {
            float result = (float)ppu.Fpr[instruction.Fra].Float64 * (float)ppu.Fpr[instruction.Frc].Float64;
            ppu.Fpr[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FMULS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fdiv(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = ppu.Fpr[instruction.Fra].Float64 / ppu.Fpr[instruction.Frb].Float64;
            //Console.WriteLine($"FDIV [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fdivs(PpuInstruction instruction, PpuThread ppu)
        {
            float result = (float)ppu.Fpr[instruction.Fra].Float64 / (float)ppu.Fpr[instruction.Frb].Float64;
            ppu.Fpr[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FDIVS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fmadd(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = double.FusedMultiplyAdd(ppu.Fpr[instruction.Fra].Float64, ppu.Fpr[instruction.Frc].Float64, ppu.Fpr[instruction.Frb].Float64);

            //ppu.FPR[instruction.Frt].Float64 = (ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) + ppu.FPR[instruction.Frb].Float64;        
            //Console.WriteLine($"FMADD [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fmadds(PpuInstruction instruction, PpuThread ppu)
        {
            float result = float.FusedMultiplyAdd((float)ppu.Fpr[instruction.Fra].Float64, (float)ppu.Fpr[instruction.Frc].Float64, (float)ppu.Fpr[instruction.Frb].Float64);

            //float result = ((float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64) + (float)ppu.FPR[instruction.Frb].Float64;

            ppu.Fpr[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FMADDS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fmsub(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = double.FusedMultiplyAdd(ppu.Fpr[instruction.Fra].Float64, ppu.Fpr[instruction.Frc].Float64, -ppu.Fpr[instruction.Frb].Float64);

            //ppu.FPR[instruction.Frt].Float64 = (ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) - ppu.FPR[instruction.Frb].Float64;          
            //Console.WriteLine($"FMADD [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fmsubs(PpuInstruction instruction, PpuThread ppu)
        {
            float result = float.FusedMultiplyAdd((float)ppu.Fpr[instruction.Fra].Float64, (float)ppu.Fpr[instruction.Frc].Float64, -(float)ppu.Fpr[instruction.Frb].Float64);

            // float result = ((float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64) - (float)ppu.FPR[instruction.Frb].Float64;

            ppu.Fpr[instruction.Frt].Float64 = result;
            //Console.WriteLine($"FMADDS [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fnmadd(PpuInstruction instruction, PpuThread ppu)
        {
            //ppu.FPR[instruction.Frt].Float64 = -((ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) + ppu.FPR[instruction.Frb].Float64);
            ppu.Fpr[instruction.Frt].Float64 = -double.FusedMultiplyAdd(ppu.Fpr[instruction.Fra].Float64, ppu.Fpr[instruction.Frc].Float64, ppu.Fpr[instruction.Frb].Float64); ;
        }

        internal static void Fnmadds(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = -float.FusedMultiplyAdd((float)ppu.Fpr[instruction.Fra].Float64, (float)ppu.Fpr[instruction.Frc].Float64, (float)ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Fnmsub(PpuInstruction instruction, PpuThread ppu)
        {
            // Frt = - (Fra * Frc - Frb) 
            // Exapnds to Frt = Frb + (-Fra * Frc)

            ppu.Fpr[instruction.Frt].Float64 = double.FusedMultiplyAdd(-ppu.Fpr[instruction.Fra].Float64, ppu.Fpr[instruction.Frc].Float64, ppu.Fpr[instruction.Frb].Float64);
            //ppu.FPR[instruction.Frt].Float64 = -((ppu.FPR[instruction.Fra].Float64 * ppu.FPR[instruction.Frc].Float64) - ppu.FPR[instruction.Frb].Float64);
        }

        internal static void Fnmsubs(PpuInstruction instruction, PpuThread ppu)
        {
            // Frt = - (Fra * Frc - Frb) 
            // Exapnds to Frt = Frb + (-Fra * Frc)

            ppu.Fpr[instruction.Frt].Float64 = float.FusedMultiplyAdd(-(float)ppu.Fpr[instruction.Fra].Float64, (float)ppu.Fpr[instruction.Frc].Float64, (float)ppu.Fpr[instruction.Frb].Float64);
            //ppu.FPR[instruction.Frt].Float64 = -(((float)ppu.FPR[instruction.Fra].Float64 * (float)ppu.FPR[instruction.Frc].Float64) - (float)ppu.FPR[instruction.Frb].Float64);
        }

        internal static void Fsqrt(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = double.Sqrt(ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Fsqrts(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = float.Sqrt((float)ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Frsqrte(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = double.ReciprocalSqrtEstimate(ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Frsqrtes(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = float.ReciprocalSqrtEstimate((float)ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Fre(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = double.ReciprocalEstimate(ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Fres(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = float.ReciprocalEstimate((float)ppu.Fpr[instruction.Frb].Float64);
        }

        internal static void Fneg(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = -(ppu.Fpr[instruction.Frb].Float64);
            //Console.WriteLine($"FNEG [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fabs(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = double.Abs(ppu.Fpr[instruction.Frb].Float64);
            //Console.WriteLine($"FNEG [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fnabs(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = -(double.Abs(ppu.Fpr[instruction.Frb].Float64));
            //Console.WriteLine($"FNEG [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Frsp(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Fpr[instruction.Frt].Float64 = (float)ppu.Fpr[instruction.Frb].Float64;
            //Console.WriteLine($"FRSP [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fcfid(PpuInstruction instruction, PpuThread ppu)
        {
            //long -> double
            ppu.Fpr[instruction.Frt].Float64 = (double)(ppu.Fpr[instruction.Frb].SValue64);
            //Console.WriteLine($"FCFID [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fctid(PpuInstruction instruction, PpuThread ppu)
        {
            Fctidz(instruction, ppu); //We only support rounding toward zero..
        }

        internal static void Fctidz(PpuInstruction instruction, PpuThread ppu)
        {
            //double -> long
            ppu.Fpr[instruction.Frt].SValue64 = (long)(ppu.Fpr[instruction.Frb].Float64);
            //Console.WriteLine($"FCFID [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Float64}");
        }

        internal static void Fctiw(PpuInstruction instruction, PpuThread ppu)
        {
            //We only support rounding toward zero..
            Fctiwz(instruction, ppu);
        }

        internal static void Fctiwz(PpuInstruction instruction, PpuThread ppu)
        {
            long frb = (long)ppu.Fpr[instruction.Frb].Float64;
            frb = Math.Clamp(frb, int.MinValue, int.MaxValue);

            //ppu.FPR[instruction.Frt].Value64 = (uint)frb;

            ppu.Fpr[instruction.Frt].Value64 = (ulong)frb | 0xfff8000000000000;

            //Console.WriteLine($"FCTIWZ [{instruction.Frt}] = {ppu.FPR[instruction.Frt].Value64}");
        }

        internal static void Fsel(PpuInstruction instruction, PpuThread ppu)
        {
            double fra = ppu.Fpr[instruction.Fra].Float64;
            double frb = ppu.Fpr[instruction.Frb].Float64;
            double frc = ppu.Fpr[instruction.Frc].Float64;
            double result = fra >= 0.0 ? frc : frb;
            ppu.Fpr[instruction.Frt].Float64 = result;
        }
    }
}