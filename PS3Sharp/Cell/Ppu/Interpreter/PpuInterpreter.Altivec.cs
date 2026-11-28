using PS3Sharp.Common;
using static PS3Sharp.Cell.Ppu.PpuThread;

namespace PS3Sharp.Cell.Ppu.Interpreter
{
    internal static unsafe partial class PpuInterpreter
    {
        internal static void PrintAllVectors(PpuThread ppu)
        {
            for (int i = 0; i < 32; i++)
            {
                Console.WriteLine($"VR[{i}] = ({ppu.Vrf[i].Floats[3]},{ppu.Vrf[i].Floats[2]},{ppu.Vrf[i].Floats[1]},{ppu.Vrf[i].Floats[0]})");
            }
        }

        internal static void SetCR(PpuThread ppu, UInt128 result)
        {
            if (result == UInt128.MaxValue)
            {
                //All equal
                ppu.State->CR.WriteCrField(CrNumber.CR6, 0x8);
            }
            else if (result == 0)
            {
                //None equal
                ppu.State->CR.WriteCrField(CrNumber.CR6, 0x2);
            }
            else
            {
                //Some equal
                ppu.State->CR.WriteCrField(CrNumber.CR6, 0x0);
            }
        }

        // Load Instructions
        internal static void Lvx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0xFU;
            ppu.Vrf[instruction.Vd].Value128 = ppu.MemoryManager.Read<BigEndian<UInt128>>(ea);
        }

        internal static void Lvewx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x3U;
            uint m = (ea >> 2) & 3;          //m=EA[28–29] 
            ppu.Vrf[instruction.Vd].UInts[3 - m] = ppu.MemoryManager.Read<BigEndian<uint>>(ea);
        }

        internal static void Lvehx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x1U;
            uint m = (ea >> 1) & 7;          //m=EA[28–30] 
            ppu.Vrf[instruction.Vd].UShorts[7 - m] = ppu.MemoryManager.Read<BigEndian<ushort>>(ea);
        }

        internal static void Lvebx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint m = ea & 0xF;              //m=EA[28–31] 
            ppu.Vrf[instruction.Vd].Bytes[15 - m] = ppu.MemoryManager.Read<byte>(ea);
        }

        // Store Instructions 
        internal static void Stvx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0xFU;
            BigEndian<UInt128> value = ppu.Vrf[instruction.Vs].Value128;
            ppu.MemoryManager.Write(ea, value);
        }

        internal static void Stvewx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x3U;
            uint m = (ea >> 2) & 3;          //m=EA[28–29] 
            BigEndian<uint> data = ppu.Vrf[instruction.Vs].UInts[3 - m];
            ppu.MemoryManager.Write(ea, data);
        }

        internal static void Stvehx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x1U;
            uint m = (ea >> 1) & 7;         //m=EA[28–30] 
            BigEndian<ushort> data = ppu.Vrf[instruction.Vs].UShorts[7 - m];
            ppu.MemoryManager.Write(ea, data);
        }

        internal static void Stvebx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint m = ea & 0xF;              //m=EA[28–31] 
            byte data = ppu.Vrf[instruction.Vs].Bytes[15 - m];
            ppu.MemoryManager.Write(ea, data);
        }

        internal static void Vadduwm(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            uint* va = vrf[instruction.Va].UInts;
            uint* vb = vrf[instruction.Vb].UInts;
            uint* vd = vrf[instruction.Vd].UInts;

            V128ADD<uint>(va, vb, vd);
        }

        internal static void Vaddfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128ADD<float>(va, vb, vd);
        }

        internal static void Vsubfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128SUB<float>(va, vb, vd);
        }

        internal static void Vand(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128AND(va, vb, vd);
        }

        internal static void Vandc(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128ANDC(va, vb, vd);
        }

        internal static void Vor(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128OR(va, vb, vd);
        }

        internal static void Vnor(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128NOR(va, vb, vd);
        }

        internal static void Vxor(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128XOR(va, vb, vd);
        }

        internal static void Lvsl(PpuInstruction instruction, PpuThread ppu)
        {
            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            int sh = (int)(addr & 0xF);

            ppu.Vrf[instruction.Vd].Value128 = LVSLShiftTable[sh];
        }

        internal static void Lvsr(PpuInstruction instruction, PpuThread ppu)
        {
            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            int sh = (int)(addr & 0xF);

            ppu.Vrf[instruction.Vd].Value128 = LVSRShiftTable[sh];
        }

        internal static void Lvlx(PpuInstruction instruction, PpuThread ppu)
        {
            // Appearently this insturction is only in the CELL processor

            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint b = addr & 0xF;

            ppu.Vrf[instruction.Vd].Value128 = 0;
            for (uint i = 0; i < (16 - b); i++)
            {
                ppu.Vrf[instruction.Vd].Bytes[15 - i] = ppu.MemoryManager.Read<byte>(addr + i);
            }
        }

        internal static void Lvrx(PpuInstruction instruction, PpuThread ppu)
        {
            // Appearently this insturction is only in the CELL processor

            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint b = addr & 0xF;

            ppu.Vrf[instruction.Vd].Value128 = 0;
            for (uint i = (16 - b); i < 16; i++)
            {
                ppu.Vrf[instruction.Vd].Bytes[15 - i] = ppu.MemoryManager.Read<byte>(addr + i - 16);
            }
        }

        internal static void Vprem(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr reg_a = ppu.Vrf[instruction.Va];
            PpuVr reg_b = ppu.Vrf[instruction.Vb];
            PpuVr reg_c = ppu.Vrf[instruction.Vc];

            ReadOnlySpan<byte> a = new(reg_a.Bytes, 16);
            ReadOnlySpan<byte> b = new(reg_b.Bytes, 16);
            ReadOnlySpan<byte> c = new(reg_c.Bytes, 16); //vector C controls the locations
            Span<byte> srcVec = stackalloc byte[32];

            //flip becasue it's big endian
            b.CopyTo(srcVec.Slice(0, 16));
            a.CopyTo(srcVec.Slice(16, 16));

            for (int i = 0; i < 16; i++)
            {
                int location = 31 - (c[i] & 0x1F); //flip becasue it's big endian
                ppu.Vrf[instruction.Vd].Bytes[i] = srcVec[location];
            }

            // Console.WriteLine($"VPERM va = {instruction.Va} val = {ppu.VRF[instruction.Va].Value128:X}");
            // Console.WriteLine($"VPERM vb = {instruction.Vb} val = {ppu.VRF[instruction.Vb].Value128:X}");
            // Console.WriteLine($"VPERM vc = {instruction.Vc} val = {ppu.VRF[instruction.Vc].Value128:X}");
            // Console.WriteLine($"VPERM vd = {instruction.Vd} val = {ppu.VRF[instruction.Vd].Value128:X}");
        }

        internal static void Vspltw(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;
            uint* vb = ppu.Vrf[instruction.Vb].UInts;
            uint* vd = ppu.Vrf[instruction.Vd].UInts;

            uint index = 3 - instruction.Uimm;  //flip becasue it's big endian
            uint word = vb[index];

            V128SPLT_Gen<uint>(vd, word);
        }

        internal static void Vspltisw(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            uint* vd = vrf[instruction.Vd].UInts;
            int signExtended = ((int)(sbyte)(instruction.VecSimm << 3)) >> 3; // sign extend 5 bit operand

            // Same as VSPLTW
            V128SPLT_Gen<uint>(vd, (uint)signExtended);
        }

        internal static void Vspltish(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            ushort* vd = vrf[instruction.Vd].UShorts;
            short signExtended = (short)((sbyte)(instruction.VecSimm << 3) >> 3); // sign extend 5 bit operand

            // Same as VSPLTW
            V128SPLT_Gen<ushort>(vd, (ushort)signExtended);
        }

        internal static void Vmaddfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vc = vrf[instruction.Vc].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128MADD(va, vb, vc, vd);
        }

        internal static void Vnmsubfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vc = vrf[instruction.Vc].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128NMSUB(va, vb, vc, vd);
        }

        internal static void Vmrglw(PpuInstruction instruction, PpuThread ppu)
        {
            //Copy the registers first
            PpuVr a = ppu.Vrf[instruction.Va];
            PpuVr b = ppu.Vrf[instruction.Vb];

            ppu.Vrf[instruction.Vd].UInts[0] = b.UInts[0];
            ppu.Vrf[instruction.Vd].UInts[1] = a.UInts[0];
            ppu.Vrf[instruction.Vd].UInts[2] = b.UInts[1];
            ppu.Vrf[instruction.Vd].UInts[3] = a.UInts[1];
        }

        internal static void Vmrghw(PpuInstruction instruction, PpuThread ppu)
        {
            //Copy the registers first
            PpuVr a = ppu.Vrf[instruction.Va];
            PpuVr b = ppu.Vrf[instruction.Vb];

            ppu.Vrf[instruction.Vd].UInts[0] = b.UInts[2];
            ppu.Vrf[instruction.Vd].UInts[1] = a.UInts[2];
            ppu.Vrf[instruction.Vd].UInts[2] = b.UInts[3];
            ppu.Vrf[instruction.Vd].UInts[3] = a.UInts[3];
        }

        internal static void Vsldoi(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr reg_a = ppu.Vrf[instruction.Va];
            PpuVr reg_b = ppu.Vrf[instruction.Vb];

            ReadOnlySpan<byte> a = new(reg_a.Bytes, 16);
            ReadOnlySpan<byte> b = new(reg_b.Bytes, 16);
            Span<byte> srcVec = stackalloc byte[32];

            //flip becasue it's big endian
            b.CopyTo(srcVec.Slice(0, 16));
            a.CopyTo(srcVec.Slice(16, 16));

            for (uint i = 0; i < 16; i++)
            {
                //flip becasue it's big endian
                int location = (int)(31 - (instruction.Shb + i));
                ppu.Vrf[instruction.Vd].Bytes[15 - i] = srcVec[location];
            }
        }

        internal static void Vrefp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128REFP(vb, vd);
        }

        internal static void Vrsqrtefp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128RSQRTEFP(vb, vd);
        }

        internal static void Vsel(PpuInstruction instruction, PpuThread ppu)
        {
            //if bit[i] of vc is 0 then vd[i] = va[i] 
            //if bit[i] of vc is 1 then vd[i] = vb[i]

            //We can use vc as a mask to extract the needed bits of both va/vb and OR them for the final result
            UInt128 vc = ppu.Vrf[instruction.Vc].Value128;
            UInt128 vb_masked = ppu.Vrf[instruction.Vb].Value128 & vc;
            UInt128 va_masked = ppu.Vrf[instruction.Va].Value128 & (~vc);
            ppu.Vrf[instruction.Vd].Value128 = vb_masked | va_masked;
        }

        internal static void Vslw(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            uint* va = vrf[instruction.Va].UInts;
            uint* vb = vrf[instruction.Vb].UInts;
            uint* vd = vrf[instruction.Vd].UInts;

            vd[0] = va[0] << (int)(vb[0] & 0x1F);
            vd[1] = va[1] << (int)(vb[1] & 0x1F);
            vd[2] = va[2] << (int)(vb[2] & 0x1F);
            vd[3] = va[3] << (int)(vb[3] & 0x1F);
        }

        internal static void Vctsxs(PpuInstruction instruction, PpuThread ppu)
        {
            long factor = 1 << (int)instruction.Uimm; //or Math.Pow(2, instruction.Uimm);

            // Clamp the result to [int.MinValue, int.MaxValue]
            // Important: we need to do multiply longs, not floats!

            ppu.Vrf[instruction.Vd].UInts[0] = (uint)Math.Clamp((long)ppu.Vrf[instruction.Vb].Floats[0] * factor, int.MinValue, int.MaxValue);
            ppu.Vrf[instruction.Vd].UInts[1] = (uint)Math.Clamp((long)ppu.Vrf[instruction.Vb].Floats[1] * factor, int.MinValue, int.MaxValue);
            ppu.Vrf[instruction.Vd].UInts[2] = (uint)Math.Clamp((long)ppu.Vrf[instruction.Vb].Floats[2] * factor, int.MinValue, int.MaxValue);
            ppu.Vrf[instruction.Vd].UInts[3] = (uint)Math.Clamp((long)ppu.Vrf[instruction.Vb].Floats[3] * factor, int.MinValue, int.MaxValue);
        }

        internal static void Vcfsx(PpuInstruction instruction, PpuThread ppu)
        {
            long factor = 1 << (int)instruction.Uimm; //or Math.Pow(2, instruction.Uimm);

            ppu.Vrf[instruction.Vd].Floats[0] = ((float)(int)ppu.Vrf[instruction.Vb].UInts[0]) / factor;
            ppu.Vrf[instruction.Vd].Floats[1] = ((float)(int)ppu.Vrf[instruction.Vb].UInts[1]) / factor;
            ppu.Vrf[instruction.Vd].Floats[2] = ((float)(int)ppu.Vrf[instruction.Vb].UInts[2]) / factor;
            ppu.Vrf[instruction.Vd].Floats[3] = ((float)(int)ppu.Vrf[instruction.Vb].UInts[3]) / factor;
        }

        internal static void Vcmpequw(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            uint* va = vrf[instruction.Va].UInts;
            uint* vb = vrf[instruction.Vb].UInts;
            uint* vd = vrf[instruction.Vd].UInts;

            V128CMPEQUW(va, vb, vd);

            // Handle Rc bit
            if (instruction.VecRc == 1)
            {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        internal static void Vcmpeqfw(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128CMPEQFW(va, vb, vd);

            // Handle Rc bit
            if (instruction.VecRc == 1)
            {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        internal static void Vcmpgtfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128CMPGTFP(va, vb, vd);

            // Handle Rc bit
            if (instruction.VecRc == 1)
            {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        internal static void Vcmpgefp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128CMPGEFP(va, vb, vd);

            // Handle Rc bit
            if (instruction.VecRc == 1)
            {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        internal static void Vmaxfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128MAXFP(va, vb, vd);
        }

        internal static void Vminfp(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128MINFP(va, vb, vd);
        }

        internal static void Vpkswss(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            // Should provide signed ptrs
            int* va = (int*)vrf[instruction.Va].UInts;
            int* vb = (int*)vrf[instruction.Vb].UInts;
            short* vd = (short*)vrf[instruction.Vd].UShorts;

            // vb first
            vd[0] = (short)Math.Clamp(vb[0], short.MinValue, short.MaxValue);
            vd[1] = (short)Math.Clamp(vb[1], short.MinValue, short.MaxValue);
            vd[2] = (short)Math.Clamp(vb[2], short.MinValue, short.MaxValue);
            vd[3] = (short)Math.Clamp(vb[3], short.MinValue, short.MaxValue);

            vd[4] = (short)Math.Clamp(va[0], short.MinValue, short.MaxValue);
            vd[5] = (short)Math.Clamp(va[1], short.MinValue, short.MaxValue);
            vd[6] = (short)Math.Clamp(va[2], short.MinValue, short.MaxValue);
            vd[7] = (short)Math.Clamp(va[3], short.MinValue, short.MaxValue);
        }

        internal static void Vupklsh(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            short* vb = (short*)vrf[instruction.Vb].UShorts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Sign extended
            vd[0] = vb[0];
            vd[1] = vb[1];
            vd[2] = vb[2];
            vd[3] = vb[3];
        }

        internal static void Vupkhsh(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            short* vb = (short*)vrf[instruction.Vb].UShorts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Sign extended
            vd[0] = vb[4];
            vd[1] = vb[5];
            vd[2] = vb[6];
            vd[3] = vb[7];
        }

        internal static void Vadduhm(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            ushort* va = vrf[instruction.Va].UShorts;
            ushort* vb = vrf[instruction.Vb].UShorts;
            ushort* vd = vrf[instruction.Vd].UShorts;

            V128ADD<ushort>(va, vb, vd);
        }

        internal static void Vsubuhm(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            ushort* va = vrf[instruction.Va].UShorts;
            ushort* vb = vrf[instruction.Vb].UShorts;
            ushort* vd = vrf[instruction.Vd].UShorts;

            V128SUB<ushort>(va, vb, vd);
        }

        internal static void Vmulosh(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            short* va = (short*)vrf[instruction.Va].UShorts;
            short* vb = (short*)vrf[instruction.Vb].UShorts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Note: that the manual says odd position,
            // but in our case it's even positions

            // shorts are promoted to int
            vd[0] = va[0] * vb[0];
            vd[1] = va[2] * vb[2];
            vd[2] = va[4] * vb[4];
            vd[3] = va[6] * vb[6];
        }

        internal static void Vmulesh(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            short* va = (short*)vrf[instruction.Va].UShorts;
            short* vb = (short*)vrf[instruction.Vb].UShorts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Note: that the manual says even position,
            // but in our case it's odd positions

            // shorts are promoted to int
            vd[0] = va[1] * vb[1];
            vd[1] = va[3] * vb[3];
            vd[2] = va[5] * vb[5];
            vd[3] = va[7] * vb[7];
        }

        internal static void Vsraw(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            int* va = (int*)vrf[instruction.Va].UInts;
            int* vb = (int*)vrf[instruction.Vb].UInts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Arithmetic shift
            vd[0] = va[0] >> (vb[0] & 0x1F);
            vd[1] = va[1] >> (vb[1] & 0x1F);
            vd[2] = va[2] >> (vb[2] & 0x1F);
            vd[3] = va[3] >> (vb[3] & 0x1F);
        }

        internal static void Vctuxs(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            long factor = 1 << (int)instruction.Uimm;

            float* vb = vrf[instruction.Vb].Floats;
            uint* vd = vrf[instruction.Vd].UInts;

            vd[0] = (uint)Math.Clamp(vb[0] * factor, 0, uint.MaxValue);
            vd[1] = (uint)Math.Clamp(vb[1] * factor, 0, uint.MaxValue);
            vd[2] = (uint)Math.Clamp(vb[2] * factor, 0, uint.MaxValue);
            vd[3] = (uint)Math.Clamp(vb[3] * factor, 0, uint.MaxValue);
        }

        internal static void Vcfux(PpuInstruction instruction, PpuThread ppu)
        {
            PpuVr* vrf = ppu.Vrf;

            long factor = 1 << (int)instruction.Uimm;

            uint* vb = vrf[instruction.Vb].UInts;
            float* vd = vrf[instruction.Vd].Floats;

            vd[0] = (float)vb[0] / factor;
            vd[1] = (float)vb[1] / factor;
            vd[2] = (float)vb[2] / factor;
            vd[3] = (float)vb[3] / factor;
        }
    }
}
