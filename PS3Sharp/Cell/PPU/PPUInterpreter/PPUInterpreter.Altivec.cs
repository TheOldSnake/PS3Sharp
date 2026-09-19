using PS3Sharp.Memory_Manager;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static PS3Sharp.Cell.PPU.PPU;

namespace PS3Sharp.Cell.PPU.PPUInterpreter {
    public static unsafe partial class PPUInterpreter {
        private static void Altivec(PPUInstruction instruction, PPU ppu) {
            //Console.WriteLine($"[{ppu.PC:X}] Altivec: {instruction.Group04Op:X} -- {(instruction.Group04Op & 0x3F):X}");

            switch ((G04)((uint)instruction.G04 & 0x3F)) {
                //Check for instructions with 6 bits opcode first
                case G04.VSEL: VSEL(instruction, ppu); break;
                case G04.VPERM: VPERM(instruction, ppu); break;
                case G04.VSLDOI: VSLDOI(instruction, ppu); break;
                case G04.VMADDFP: VMADDFP(instruction, ppu); break;
                case G04.VNMSUBFP: VNMSUBFP(instruction, ppu); break;

                default:
                    //Then the rest
                    switch (instruction.G04) {
                        case G04.VADDFP: VADDFP(instruction, ppu); break;
                        case G04.VADDUWM: VADDUWM(instruction, ppu); break;
                        case G04.VCMPEQUW: VCMPEQUW(instruction, ppu); break;
                        case G04.VMRGHW: VMRGHW(instruction, ppu); break;
                        case G04.VADDUHM: VADDUHM(instruction, ppu); break;
                        case G04.VSUBFP: VSUBFP(instruction, ppu); break;

                        case G04.VCMPEQFW: VCMPEQFW(instruction, ppu); break;
                        case G04.VCMPEQFW_RC: VCMPEQFW(instruction, ppu); break;

                        case G04.VREFP: VREFP(instruction, ppu); break;
                        case G04.VMULOSH: VMULOSH(instruction, ppu); break;
                        case G04.VRSQRTEFP: VRSQRTEFP(instruction, ppu); break;
                        case G04.VSLW: VSLW(instruction, ppu); break;
                        case G04.VMRGLW: VMRGLW(instruction, ppu); break;
                        case G04.VPKSWSS: VPKSWSS(instruction, ppu); break;

                        case G04.VSPLTW: VSPLTW(instruction, ppu); break;

                        case G04.VCMPGEFP: VCMPGEFP(instruction, ppu); break;
                        case G04.VCMPGTFP: VCMPGTFP(instruction, ppu); break;

                        case G04.VCMPGEFP_RC: VCMPGEFP(instruction, ppu); break;
                        case G04.VCMPGTFP_RC: VCMPGTFP(instruction, ppu); break;

                        case G04.VUPKHSH: VUPKHSH(instruction, ppu); break;
                        case G04.VUPKLSH: VUPKLSH(instruction, ppu); break;

                        case G04.VCFUX: VCFUX(instruction, ppu); break;
                        case G04.VCFSX: VCFSX(instruction, ppu); break;
                        case G04.VMULESH: VMULESH(instruction, ppu); break;
                        case G04.VSPLTISH: VSPLTISH(instruction, ppu); break;
                        case G04.VSRAW: VSRAW(instruction, ppu); break;
                        case G04.VCTUXS: VCTUXS(instruction, ppu); break;
                        case G04.VSPLTISW: VSPLTISW(instruction, ppu); break;
                        case G04.VCTSXS: VCTSXS(instruction, ppu); break;

                        case G04.VAND: VAND(instruction, ppu); break;
                        case G04.VMAXFP: VMAXFP(instruction, ppu); break;
                        case G04.VSUBUHM: VSUBUHM(instruction, ppu); break;
                        case G04.VANDC: VANDC(instruction, ppu); break;
                        case G04.VMINFP: VMINFP(instruction, ppu); break;
                        case G04.VOR: VOR(instruction, ppu); break;
                        case G04.VXOR: VXOR(instruction, ppu); break;
                        case G04.VNOR: VNOR(instruction, ppu); break;
                            default: throw new Exception($"Unhandled PPU Altivec OP: 0x{(uint)instruction.G04:X} = {instruction.G04} -- raw = 0x{instruction.Raw:X8}");
                    }

                    break;
            }         
        }

        public static void PrintAllVectors(PPU ppu) {
            for (int i = 0; i < 32; i++) {
                Console.WriteLine($"VR[{i}] = ({ppu.VRF[i].Floats[3]},{ppu.VRF[i].Floats[2]},{ppu.VRF[i].Floats[1]},{ppu.VRF[i].Floats[0]})");
            }
        }

        public static void SetCR(PPU ppu, UInt128 result) {
            if (result == UInt128.MaxValue) {
                //All equal
                ppu.State->CR.WriteCRField(CRNumber.CR6, 0x8);
            } else if (result == 0) {
                //None equal
                ppu.State->CR.WriteCRField(CRNumber.CR6, 0x2);
            } else {
                //Some equal
                ppu.State->CR.WriteCRField(CRNumber.CR6, 0x0);
            }
        }

        // Load Instructions
        public static void LVX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0xFU;
            ppu.VRF[instruction.Vd].Value128 = MemoryManager.Read<UInt128>(ea);
        }

        public static void LVEWX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x3U;
            uint m = (ea >> 2) & 3;          //m=EA[28–29] 
            ppu.VRF[instruction.Vd].UInts[3 - m] = MemoryManager.Read<uint>(ea);
        }

        public static void LVEHX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x1U;
            uint m = (ea >> 1) & 7;          //m=EA[28–30] 
            ppu.VRF[instruction.Vd].UShorts[7 - m] = MemoryManager.Read<ushort>(ea);
        }

        public static void LVEBX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint m = ea & 0xF;              //m=EA[28–31] 
            ppu.VRF[instruction.Vd].Bytes[15 - m] = MemoryManager.Read<byte>(ea);
        }

        // Store Instructions 
        public static void STVX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0xFU;
            MemoryManager.Write<UInt128>(ea, ppu.VRF[instruction.Vs].Value128);
        }

        public static void STVEWX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x3U;
            uint m = (ea >> 2) & 3;          //m=EA[28–29] 
            uint data = ppu.VRF[instruction.Vs].UInts[3 - m];
            MemoryManager.Write<uint>(ea, data);
        }

        public static void STVEHX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true) & ~0x1U;
            uint m = (ea >> 1) & 7;         //m=EA[28–30] 
            ushort data = ppu.VRF[instruction.Vs].UShorts[7 - m];
            MemoryManager.Write<ushort>(ea, data);
        }

        public static void STVEBX(PPUInstruction instruction, PPU ppu) {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint m = ea & 0xF;              //m=EA[28–31] 
            byte data = ppu.VRF[instruction.Vs].Bytes[15 - m];
            MemoryManager.Write<byte>(ea, data);
        }

        public static void VADDUWM(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            uint* va = vrf[instruction.Va].UInts;
            uint* vb = vrf[instruction.Vb].UInts;
            uint* vd = vrf[instruction.Vd].UInts;

            V128ADD<uint>(va, vb, vd);
        }

        public static void VADDFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128ADD<float>(va, vb, vd);
        }

        public static void VSUBFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128SUB<float>(va, vb, vd);
        }

        public static void VAND(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128AND(va, vb, vd);
        }

        public static void VANDC(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128ANDC(va, vb, vd);
        }

        public static void VOR(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128OR(va, vb, vd);
        }

        public static void VNOR(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128NOR(va, vb, vd);
        }

        public static void VXOR(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            byte* va = vrf[instruction.Va].Bytes;
            byte* vb = vrf[instruction.Vb].Bytes;
            byte* vd = vrf[instruction.Vd].Bytes;

            V128XOR(va, vb, vd);
        }

        public static void LVSL(PPUInstruction instruction, PPU ppu) {
            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint sh = addr & 0xF;

            ppu.VRF[instruction.Vd].Value128 = GenerateShift(sh);

            static UInt128 GenerateShift(uint sh) { //or use a lookup table
                UInt128 value = 0;

                for (int i = 0; i < 16; i++) {
                    value = (value << 8) | (byte)(sh + i);
                }

                //Console.WriteLine($"LVSL sh: {sh} -> {value:X32}");
                return value;
            }

            //Console.WriteLine($"LVSL vd = {instruction.Vd} val = {ppu.VRF[instruction.Vd].Value128:X}");
        }

        public static void LVLX(PPUInstruction instruction, PPU ppu) {
            // Appearently this insturction is only in the CELL processor

            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint b = addr & 0xF;

            ppu.VRF[instruction.Vd].Value128 = 0;
            for (uint i = 0; i < (16 - b); i++) {
                ppu.VRF[instruction.Vd].Bytes[15 - i] = MemoryManager.Read<byte>(addr + i);
            }
        }

        public static void LVRX(PPUInstruction instruction, PPU ppu) {
            // Appearently this insturction is only in the CELL processor

            uint addr = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint b = addr & 0xF;

            ppu.VRF[instruction.Vd].Value128 = 0;
            for (uint i = (16 - b); i < 16; i++) {
                ppu.VRF[instruction.Vd].Bytes[15 - i] = MemoryManager.Read<byte>(addr + i - 16);
            }
        }

        public static void VPERM(PPUInstruction instruction, PPU ppu) {
            PPUVR reg_a = ppu.VRF[instruction.Va];
            PPUVR reg_b = ppu.VRF[instruction.Vb];
            PPUVR reg_c = ppu.VRF[instruction.Vc];

            ReadOnlySpan<byte> a = new ReadOnlySpan<byte>(reg_a.Bytes, 16);
            ReadOnlySpan<byte> b = new ReadOnlySpan<byte>(reg_b.Bytes, 16);
            ReadOnlySpan<byte> c = new ReadOnlySpan<byte>(reg_c.Bytes, 16); //vector C controls the locations
            Span<byte> srcVec = stackalloc byte[32];

            //flip becasue it's big endian
            b.CopyTo(srcVec.Slice(0, 16));
            a.CopyTo(srcVec.Slice(16, 16));

            for (int i = 0; i < 16; i++) {
                int location = 31 - (c[i] & 0x1F); //flip becasue it's big endian
                ppu.VRF[instruction.Vd].Bytes[i] = srcVec[location]; 
            }

           // Console.WriteLine($"VPERM va = {instruction.Va} val = {ppu.VRF[instruction.Va].Value128:X}");
           // Console.WriteLine($"VPERM vb = {instruction.Vb} val = {ppu.VRF[instruction.Vb].Value128:X}");
           // Console.WriteLine($"VPERM vc = {instruction.Vc} val = {ppu.VRF[instruction.Vc].Value128:X}");
           // Console.WriteLine($"VPERM vd = {instruction.Vd} val = {ppu.VRF[instruction.Vd].Value128:X}");
        }

        public static void VSPLTW(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;
            uint* vb = ppu.VRF[instruction.Vb].UInts;
            uint* vd = ppu.VRF[instruction.Vd].UInts;

            uint index = 3 - instruction.Uimm;  //flip becasue it's big endian
            uint word = vb[index];

            V128SPLT_Gen<uint>(vd, word);
        }

        public static void VSPLTISW(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            uint* vd = vrf[instruction.Vd].UInts;
            int signExtended = ((int)(sbyte)(instruction.Vec_SIMM << 3)) >> 3; // sign extend 5 bit operand

            // Same as VSPLTW
            V128SPLT_Gen<uint>(vd, (uint)signExtended);
        }

        public static void VSPLTISH(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            ushort* vd = vrf[instruction.Vd].UShorts;
            short signExtended = (short)((sbyte)(instruction.Vec_SIMM << 3) >> 3); // sign extend 5 bit operand

            // Same as VSPLTW
            V128SPLT_Gen<ushort>(vd, (ushort)signExtended);
        }

        public static void VMADDFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vc = vrf[instruction.Vc].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128MADD(va, vb, vc, vd);
        }

        public static void VNMSUBFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vc = vrf[instruction.Vc].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128NMSUB(va, vb, vc, vd);
        }

        public static void VMRGLW(PPUInstruction instruction, PPU ppu) {
            //Copy the registers first
            PPUVR a = ppu.VRF[instruction.Va]; 
            PPUVR b = ppu.VRF[instruction.Vb]; 

            ppu.VRF[instruction.Vd].UInts[0] = b.UInts[0];
            ppu.VRF[instruction.Vd].UInts[1] = a.UInts[0];
            ppu.VRF[instruction.Vd].UInts[2] = b.UInts[1];
            ppu.VRF[instruction.Vd].UInts[3] = a.UInts[1];
        }

        public static void VMRGHW(PPUInstruction instruction, PPU ppu) {
            //Copy the registers first
            PPUVR a = ppu.VRF[instruction.Va];
            PPUVR b = ppu.VRF[instruction.Vb];

            ppu.VRF[instruction.Vd].UInts[0] = b.UInts[2];
            ppu.VRF[instruction.Vd].UInts[1] = a.UInts[2];
            ppu.VRF[instruction.Vd].UInts[2] = b.UInts[3];
            ppu.VRF[instruction.Vd].UInts[3] = a.UInts[3];
        }

        public static void VSLDOI(PPUInstruction instruction, PPU ppu) {
            PPUVR reg_a = ppu.VRF[instruction.Va];
            PPUVR reg_b = ppu.VRF[instruction.Vb];

            ReadOnlySpan<byte> a = new ReadOnlySpan<byte>(reg_a.Bytes, 16);
            ReadOnlySpan<byte> b = new ReadOnlySpan<byte>(reg_b.Bytes, 16);
            Span<byte> srcVec = stackalloc byte[32];

            //flip becasue it's big endian
            b.CopyTo(srcVec.Slice(0, 16));
            a.CopyTo(srcVec.Slice(16, 16));

            for (uint i = 0; i < 16; i++) {
                //flip becasue it's big endian
                int location = (int)(31 - (instruction.SHB + i));
                ppu.VRF[instruction.Vd].Bytes[15 - i] = srcVec[location];
            }
        }

        public static void VREFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128REFP(vb, vd);
        }

        public static void VRSQRTEFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128RSQRTEFP(vb, vd);
        }

        public static void VSEL(PPUInstruction instruction, PPU ppu) {
            //if bit[i] of vc is 0 then vd[i] = va[i] 
            //if bit[i] of vc is 1 then vd[i] = vb[i]
            
            //We can use vc as a mask to extract the needed bits of both va/vb and OR them for the final result
            UInt128 vc = ppu.VRF[instruction.Vc].Value128; 
            UInt128 vb_masked = ppu.VRF[instruction.Vb].Value128 & vc;
            UInt128 va_masked = ppu.VRF[instruction.Va].Value128 & (~vc);
            ppu.VRF[instruction.Vd].Value128 = vb_masked | va_masked;
        }

        public static void VSLW(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            uint* va = vrf[instruction.Va].UInts;
            uint* vb = vrf[instruction.Vb].UInts;
            uint* vd = vrf[instruction.Vd].UInts;

            vd[0] = va[0] << (int)(vb[0] & 0x1F);
            vd[1] = va[1] << (int)(vb[1] & 0x1F);
            vd[2] = va[2] << (int)(vb[2] & 0x1F);
            vd[3] = va[3] << (int)(vb[3] & 0x1F);
        }

        public static void VCTSXS(PPUInstruction instruction, PPU ppu) {
            long factor = 1 << (int)instruction.Uimm; //or Math.Pow(2, instruction.Uimm);

            // Clamp the result to [int.MinValue, int.MaxValue]
            // Important: we need to do multiply longs, not floats!

            ppu.VRF[instruction.Vd].UInts[0] = (uint)Math.Clamp((long)ppu.VRF[instruction.Vb].Floats[0] * factor, int.MinValue, int.MaxValue);
            ppu.VRF[instruction.Vd].UInts[1] = (uint)Math.Clamp((long)ppu.VRF[instruction.Vb].Floats[1] * factor, int.MinValue, int.MaxValue);
            ppu.VRF[instruction.Vd].UInts[2] = (uint)Math.Clamp((long)ppu.VRF[instruction.Vb].Floats[2] * factor, int.MinValue, int.MaxValue);
            ppu.VRF[instruction.Vd].UInts[3] = (uint)Math.Clamp((long)ppu.VRF[instruction.Vb].Floats[3] * factor, int.MinValue, int.MaxValue);
        }

        public static void VCFSX(PPUInstruction instruction, PPU ppu) {
            long factor = 1 << (int)instruction.Uimm; //or Math.Pow(2, instruction.Uimm);

            ppu.VRF[instruction.Vd].Floats[0] = ((float)(int)ppu.VRF[instruction.Vb].UInts[0]) / factor;
            ppu.VRF[instruction.Vd].Floats[1] = ((float)(int)ppu.VRF[instruction.Vb].UInts[1]) / factor;
            ppu.VRF[instruction.Vd].Floats[2] = ((float)(int)ppu.VRF[instruction.Vb].UInts[2]) / factor;
            ppu.VRF[instruction.Vd].Floats[3] = ((float)(int)ppu.VRF[instruction.Vb].UInts[3]) / factor;
        }

        public static void VCMPEQUW(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            uint* va = vrf[instruction.Va].UInts;
            uint* vb = vrf[instruction.Vb].UInts;
            uint* vd = vrf[instruction.Vd].UInts;

            V128CMPEQUW(va, vb, vd);

            // Handle Rc bit
            if (instruction.Vec_Rc == 1) {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        public static void VCMPEQFW(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;
            
            V128CMPEQFW(va, vb, vd);

            // Handle Rc bit
            if (instruction.Vec_Rc == 1) {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        public static void VCMPGTFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128CMPGTFP(va, vb, vd);

            // Handle Rc bit
            if (instruction.Vec_Rc == 1) {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }

        public static void VCMPGEFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128CMPGEFP(va, vb, vd);

            // Handle Rc bit
            if (instruction.Vec_Rc == 1) {
                SetCR(ppu, vrf[instruction.Vd].Value128);
            }
        }
   
        public static void VMAXFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128MAXFP(va, vb, vd);
        }

        public static void VMINFP(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            float* va = vrf[instruction.Va].Floats;
            float* vb = vrf[instruction.Vb].Floats;
            float* vd = vrf[instruction.Vd].Floats;

            V128MINFP(va, vb, vd);
        }

        public static void VPKSWSS(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

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

        public static void VUPKLSH(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            short* vb = (short*)vrf[instruction.Vb].UShorts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Sign extended
            vd[0] = vb[0];
            vd[1] = vb[1];
            vd[2] = vb[2];
            vd[3] = vb[3];
        }

        public static void VUPKHSH(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            short* vb = (short*)vrf[instruction.Vb].UShorts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Sign extended
            vd[0] = vb[4];
            vd[1] = vb[5];
            vd[2] = vb[6];
            vd[3] = vb[7];
        }

        public static void VADDUHM(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            ushort* va = vrf[instruction.Va].UShorts;
            ushort* vb = vrf[instruction.Vb].UShorts;
            ushort* vd = vrf[instruction.Vd].UShorts;

            V128ADD<ushort>(va, vb, vd);
        }

        public static void VSUBUHM(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            ushort* va = vrf[instruction.Va].UShorts;
            ushort* vb = vrf[instruction.Vb].UShorts;
            ushort* vd = vrf[instruction.Vd].UShorts;

            V128SUB<ushort>(va, vb, vd);
        }

        public static void VMULOSH(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

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

        public static void VMULESH(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

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

        public static void VSRAW(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            int* va = (int*)vrf[instruction.Va].UInts;
            int* vb = (int*)vrf[instruction.Vb].UInts;
            int* vd = (int*)vrf[instruction.Vd].UInts;

            // Arithmetic shift
            vd[0] = va[0] >> (vb[0] & 0x1F);
            vd[1] = va[1] >> (vb[1] & 0x1F);
            vd[2] = va[2] >> (vb[2] & 0x1F);
            vd[3] = va[3] >> (vb[3] & 0x1F);
        }
        
        public static void VCTUXS(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

            long factor = 1 << (int)instruction.Uimm;

            float* vb = vrf[instruction.Vb].Floats;
            uint* vd = vrf[instruction.Vd].UInts;

            vd[0] = (uint)Math.Clamp(vb[0] * factor, 0, uint.MaxValue);
            vd[1] = (uint)Math.Clamp(vb[1] * factor, 0, uint.MaxValue);
            vd[2] = (uint)Math.Clamp(vb[2] * factor, 0, uint.MaxValue);
            vd[3] = (uint)Math.Clamp(vb[3] * factor, 0, uint.MaxValue);
        }

        public static void VCFUX(PPUInstruction instruction, PPU ppu) {
            PPUVR* vrf = ppu.VRF;

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
