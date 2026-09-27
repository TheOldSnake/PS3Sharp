using System.Numerics;
using static PS3Sharp.Cell.PPU.PPURotationMasks;

namespace PS3Sharp.Cell.PPU.PPUInterpreter
{
    internal static unsafe partial class PPUInterpreter
    {

        // Add with carry out helpers
        internal static ulong CkdAdd(ulong a, ulong b, out bool carry)
        {
            ulong result = a + b;
            carry = result < a;
            return result;
        }

        internal static uint CkdAdd(uint a, uint b, out bool carry)
        {
            uint result = a + b;
            carry = result < a;
            return result;
        }

        // ADD instructions
        private static void ADDI(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Rt].Value64 = (instruction.Ra == 0 ? 0 : ppu.GPR[instruction.Ra].Value64) + (ulong)instruction.SI_64;
        }

        private static void ADDIS(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Rt].SValue64 = (instruction.Ra == 0 ? 0 : ppu.GPR[instruction.Ra].SValue64) + (long)(int)(instruction.SI << 16);
        }

        private static void ADDIC(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong b = (ulong)instruction.SI_64;
            ulong result = CkdAdd(ra, b, out bool carry);   // Add with carry check

            ppu.GPR[instruction.Rt].Value64 = result;
            ppu.State->XER.SetCA(carry ? 1U : 0U);
        }

        private static void ADDIC_Record(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong b = (ulong)instruction.SI_64;
            ulong result = CkdAdd(ra, b, out bool carry);

            ppu.GPR[instruction.Rt].Value64 = result;
            ppu.State->XER.SetCA(carry ? 1U : 0U);

            // this instructions always updates CR0
            UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
        }

        private static void ADDE(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong rb = ppu.GPR[instruction.Rb].Value64;
            ulong ca = ppu.State->XER.CA;

            ulong sum1 = CkdAdd(ra, rb, out bool carry1);
            ulong sum2 = CkdAdd(sum1, ca, out bool carry2);

            ppu.GPR[instruction.Rt].Value64 = sum2;
            ppu.State->XER.SetCA((carry1 || carry2) ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }


        private static void ADD(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Rt].Value64 = ppu.GPR[instruction.Rb].Value64 + ppu.GPR[instruction.Ra].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        private static void ADDC(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong rb = ppu.GPR[instruction.Rb].Value64;
            ulong sum = CkdAdd(ra, rb, out bool carry);

            ppu.GPR[instruction.Rt].Value64 = sum;
            ppu.State->XER.SetCA(carry ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void ADDZE(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong ca = ppu.State->XER.CA;
            ulong sum = CkdAdd(ra, ca, out bool carry);

            ppu.GPR[instruction.Rt].Value64 = sum;
            ppu.State->XER.SetCA(carry ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void ADDME(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong ca = ppu.State->XER.CA;
            ulong minusOne = ulong.MaxValue;

            ulong sum1 = CkdAdd(ra, ca, out bool carry1);
            ulong sum2 = CkdAdd(sum1, minusOne, out bool carry2);

            ppu.GPR[instruction.Rt].Value64 = sum2;
            ppu.State->XER.SetCA(carry1 || carry2 ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        // Subtract instructions
        private static void SUBF(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Rt].Value64 = ppu.GPR[instruction.Rb].Value64 - ppu.GPR[instruction.Ra].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        private static void SUBFC(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong rb = ppu.GPR[instruction.Rb].Value64;

            ulong sum1 = CkdAdd(~ra, rb, out bool carry1);
            ulong sum2 = CkdAdd(sum1, 1, out bool carry2);

            ppu.State->XER.SetCA((carry1 || carry2) ? 1U : 0U);
            ppu.GPR[instruction.Rt].Value64 = sum2;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        private static void SUBFIC(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong b = (ulong)instruction.SI_64;

            ulong sum1 = CkdAdd(~ra, b, out bool carry1);
            ulong sum2 = CkdAdd(sum1, 1, out bool carry2);

            ppu.State->XER.SetCA((carry1 || carry2) ? 1U : 0U);
            ppu.GPR[instruction.Rt].Value64 = sum2;
        }


        private static void SUBFE(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong rb = ppu.GPR[instruction.Rb].Value64;
            ulong ca = ppu.State->XER.CA;

            ulong sum1 = CkdAdd(~ra, rb, out bool carry1);
            ulong sum2 = CkdAdd(sum1, ca, out bool carry2);

            ppu.GPR[instruction.Rt].Value64 = sum2;
            ppu.State->XER.SetCA((carry1 || carry2) ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SUBFZE(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong ca = ppu.State->XER.CA;
            ulong sum = CkdAdd(~ra, ca, out bool carry);

            ppu.GPR[instruction.Rt].Value64 = sum;
            ppu.State->XER.SetCA(carry ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SUBFME(PPUInstruction instruction, PPU ppu)
        {
            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ulong ca = ppu.State->XER.CA;
            ulong minusOne = ulong.MaxValue;

            ulong result1 = CkdAdd(~ra, ca, out bool carry1);
            ulong result2 = CkdAdd(result1, minusOne, out bool carry2);

            ppu.GPR[instruction.Rt].Value64 = result2;
            ppu.State->XER.SetCA(carry1 || carry2 ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        // General logic instructions
        internal static void ANDI(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 & instruction.UI;
            UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
        }

        internal static void ANDIS(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 & ((ulong)instruction.UI << 16);
            UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
        }

        private static void ORI(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 | instruction.UI;
            // Does not affect CR
        }

        private static void ORIS(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 | ((ulong)instruction.UI << 16);
            // Does not affect CR
        }

        private static void XORI(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 ^ instruction.UI;
            // Does not affect CR
        }

        private static void XORIS(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 ^ ((ulong)instruction.UI << 16);
            // Does not affect CR
        }

        internal static void AND(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 & ppu.GPR[instruction.Rb].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void OR(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 | ppu.GPR[instruction.Rb].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void XOR(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 ^ ppu.GPR[instruction.Rb].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void NAND(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ~(ppu.GPR[instruction.Rs].Value64 & ppu.GPR[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void NOR(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ~(ppu.GPR[instruction.Rs].Value64 | ppu.GPR[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void EQV(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ~(ppu.GPR[instruction.Rs].Value64 ^ ppu.GPR[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void ANDC(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 & (~ppu.GPR[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void ORC(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].Value64 = ppu.GPR[instruction.Rs].Value64 | (~ppu.GPR[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void NEG(PPUInstruction instruction, PPU ppu)
        {
            //RT = ¬(RA) + 1
            ppu.GPR[instruction.Rt].SValue64 = -ppu.GPR[instruction.Ra].SValue64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        // Condition register bit logic opcodes
        internal static (uint ba, uint bb) GetBitsAB(PPUInstruction instruction, PPU ppu)
        {
            //Helper for condition register bit opcodes
            uint ba = (ppu.State->CR.Raw >> (int)(31 - instruction.Ba)) & 1;
            uint bb = (ppu.State->CR.Raw >> (int)(31 - instruction.Bb)) & 1;
            return (ba, bb);
        }

        internal static void CROR(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba | bb; //OR
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CRORC(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba | (~bb); //ORC
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CRAND(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba & bb; //AND
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CRANDC(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba & (~bb); //ANDC
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CRXOR(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba ^ bb; //XOR
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CRNAND(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ~(ba & bb); //NAND
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CRNOR(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ~(ba | bb); //NOR
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }

        internal static void CREQV(PPUInstruction instruction, PPU ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ~(ba ^ bb); //EQV (~XOR)
            ppu.State->CR.WriteCRBit(instruction.Bt, bt_value & 1);
        }


        // Multiply instructions
        internal static void MULLI(PPUInstruction instruction, PPU ppu)
        { //Multiply low immediate
            ppu.GPR[instruction.Rt].SValue64 = ppu.GPR[instruction.Ra].SValue64 * instruction.SI_64;
            //No special registers affected
        }

        internal static void MULLW(PPUInstruction instruction, PPU ppu)
        { //Multiply word, write 64-bit result (signed)
            ppu.GPR[instruction.Rt].SValue64 = (long)ppu.GPR[instruction.Ra].SValue32 * (long)ppu.GPR[instruction.Rb].SValue32;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void MULHW(PPUInstruction instruction, PPU ppu)
        { //Multiply word, write high 32-bit (signed)
            long result = ((long)ppu.GPR[instruction.Ra].SValue32) * ((long)ppu.GPR[instruction.Rb].SValue32);
            ppu.GPR[instruction.Rt].SValue64 = (int)(result >> 32);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void MULHWU(PPUInstruction instruction, PPU ppu)
        { //Multiply word, write high 32-bit (unsigned)
            ulong result = ((ulong)ppu.GPR[instruction.Ra].Value32) * ((ulong)ppu.GPR[instruction.Rb].Value32);
            ppu.GPR[instruction.Rt].Value64 = (uint)(result >> 32);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void MULLD(PPUInstruction instruction, PPU ppu)
        { //Multiply double word (signed)
            ppu.GPR[instruction.Rt].SValue64 = ppu.GPR[instruction.Ra].SValue64 * ppu.GPR[instruction.Rb].SValue64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void MULHD(PPUInstruction instruction, PPU ppu)
        { //Multiply double word, write high 64-bit (signed)
            Int128 result = ((Int128)ppu.GPR[instruction.Ra].SValue64) * ((Int128)ppu.GPR[instruction.Rb].SValue64);
            ppu.GPR[instruction.Rt].SValue64 = (long)(result >> 64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void MULHDU(PPUInstruction instruction, PPU ppu)
        { //Multiply double word, write high 64-bit (unsigned)
            UInt128 result = ((UInt128)ppu.GPR[instruction.Ra].Value64) * ((UInt128)ppu.GPR[instruction.Rb].Value64);
            ppu.GPR[instruction.Rt].Value64 = (ulong)(result >> 64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        // Divide instructions
        internal static void DIVD(PPUInstruction instruction, PPU ppu)
        {
            long a = ppu.GPR[instruction.Ra].SValue64;
            long b = ppu.GPR[instruction.Rb].SValue64;

            if (b == 0 || (((ulong)a == 0x8000_0000_0000_0000) && b == -1))
            {
                ppu.GPR[instruction.Rt].SValue64 = 0;
            }
            else
            {
                ppu.GPR[instruction.Rt].SValue64 = a / b;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void DIVDU(PPUInstruction instruction, PPU ppu)
        {
            ulong a = ppu.GPR[instruction.Ra].Value64;
            ulong b = ppu.GPR[instruction.Rb].Value64;

            if (b == 0)
            {
                ppu.GPR[instruction.Rt].Value64 = 0;
            }
            else
            {
                ppu.GPR[instruction.Rt].Value64 = a / b;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void DIVW(PPUInstruction instruction, PPU ppu)
        {
            int a = ppu.GPR[instruction.Ra].SValue32;
            int b = ppu.GPR[instruction.Rb].SValue32;

            if (b == 0 || (((uint)a == 0x8000_0000) && b == -1))
            {
                ppu.GPR[instruction.Rt].Value64 = 0;
            }
            else
            {
                // no sign extension to 64-bits, apparently 
                uint result = (uint)(a / b);
                ppu.GPR[instruction.Rt].Value64 = result;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void DIVWU(PPUInstruction instruction, PPU ppu)
        {
            uint a = ppu.GPR[instruction.Ra].Value32;
            uint b = ppu.GPR[instruction.Rb].Value32;

            if (b == 0)
            {
                ppu.GPR[instruction.Rt].Value64 = 0;
            }
            else
            {
                ppu.GPR[instruction.Rt].Value64 = a / b;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Rt].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        // Shift instructions
        internal static void SLD(PPUInstruction instruction, PPU ppu)
        {
            int shift = (int)(ppu.GPR[instruction.Rb].Value64 & 0x7F);
            ulong result = shift >= 64 ? 0 : ppu.GPR[instruction.Rs].Value64 << shift;
            ppu.GPR[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SLW(PPUInstruction instruction, PPU ppu)
        {
            int shift = (int)(ppu.GPR[instruction.Rb].Value64 & 0x3F);
            ulong result = shift >= 32 ? 0 : ppu.GPR[instruction.Rs].Value32 << shift;
            ppu.GPR[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SRAD(PPUInstruction instruction, PPU ppu)
        {
            int shift = (int)(ppu.GPR[instruction.Rb].Value32 & 0x7F);
            int mask = (1 << shift) - 1;
            bool hasOnes = (ppu.GPR[instruction.Rs].SValue64 & mask) > 0;

            if (shift > 63)
            {
                uint sign = ppu.GPR[instruction.Rs].SValue64 < 0 ? 1U : 0U;
                ppu.GPR[instruction.Ra].SValue64 = sign == 1 ? -1 : 0;
                ppu.State->XER.SetCA(sign);

            }
            else
            {

                //Arithmetic shift
                ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue64 >> shift;

                if (shift > 0)
                {
                    if (ppu.GPR[instruction.Rs].SValue64 < 0 && hasOnes)
                    {
                        ppu.State->XER.SetCA(1);
                    }
                    else
                    {
                        ppu.State->XER.SetCA(0);
                    }
                }
                else
                {
                    ppu.State->XER.SetCA(0);
                }
            }


            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SRADI(PPUInstruction instruction, PPU ppu)
        {
            int mask = (1 << instruction.SH) - 1;
            bool hasOnes = (ppu.GPR[instruction.Rs].SValue64 & mask) > 0;

            //Arithmetic shift
            ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue64 >> instruction.SH;

            if (instruction.SH > 0)
            {
                if (ppu.GPR[instruction.Rs].SValue64 < 0 && hasOnes)
                {
                    ppu.State->XER.SetCA(1);
                }
                else
                {
                    ppu.State->XER.SetCA(0);
                }
            }
            else
            {
                ppu.State->XER.SetCA(0);
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SRAW(PPUInstruction instruction, PPU ppu)
        {
            int shift = (int)(ppu.GPR[instruction.Rb].Value32 & 0x3F);
            int mask = (1 << shift) - 1;
            bool hasOnes = (ppu.GPR[instruction.Rs].SValue32 & mask) > 0;

            if (shift > 31)
            {
                uint sign = ppu.GPR[instruction.Rs].SValue32 < 0 ? 1U : 0U;
                ppu.GPR[instruction.Ra].SValue64 = sign == 1 ? -1 : 0;
                ppu.State->XER.SetCA(sign);

            }
            else
            {

                //Arithmetic shift
                ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue32 >> shift;

                if (shift > 0)
                {
                    if (ppu.GPR[instruction.Rs].SValue32 < 0 && hasOnes)
                    {
                        ppu.State->XER.SetCA(1);
                    }
                    else
                    {
                        ppu.State->XER.SetCA(0);
                    }
                }
                else
                {
                    ppu.State->XER.SetCA(0);
                }
            }


            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SRAWI(PPUInstruction instruction, PPU ppu)
        {
            int shift = instruction.SH & 0x1F; //5 bits
            int mask = (1 << (shift)) - 1;
            bool hasOnes = (ppu.GPR[instruction.Rs].SValue32 & mask) > 0;

            //Arithmetic shift
            ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue32 >> shift;

            if (shift > 0)
            {
                if (ppu.GPR[instruction.Rs].SValue32 < 0 && hasOnes)
                {
                    ppu.State->XER.SetCA(1);
                }
                else
                {
                    ppu.State->XER.SetCA(0);
                }

            }
            else
            {
                ppu.State->XER.SetCA(0);
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SRD(PPUInstruction instruction, PPU ppu)
        {
            int shift = (int)(ppu.GPR[instruction.Rb].Value64 & 0x7F);
            ulong result = shift >= 64 ? 0 : ppu.GPR[instruction.Rs].Value64 >> shift;
            ppu.GPR[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void SRW(PPUInstruction instruction, PPU ppu)
        {
            int shift = (int)(ppu.GPR[instruction.Rb].Value64 & 0x3F);
            ulong result = shift >= 32 ? 0 : ppu.GPR[instruction.Rs].Value32 >> shift;
            ppu.GPR[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        // Rotate instructions
        private static void RotateLeft(PPUInstruction instruction, PPU ppu)
        {
            uint field = (instruction.Raw >> 1) & 0xF; // bits 27 - 30 in BE

            switch (field)
            {
                case 0:
                case 1:
                    RLDICL(instruction, ppu);
                    break;

                case 2:
                case 3:
                    RLDICR(instruction, ppu);
                    break;

                case 4:
                case 5:
                    RLDIC(instruction, ppu);
                    break;

                case 6:
                case 7:
                    RLDIMI(instruction, ppu);
                    break;

                case 8:
                    RLDCL(instruction, ppu);
                    break;

                case 9:
                    RLDCR(instruction, ppu);
                    break;

                default:
                    throw new Exception($"Unknown RotateLeft PPU Instruction: {instruction.Raw:X8}");
            }

            /*switch ((instruction.Raw >> 1) & 0xF) {
                case 8: RLDCL(instruction, ppu); return;
                case 9: RLDCR(instruction, ppu); return;
            }

            switch ((instruction.Raw >> 2) & 0x7) {
                case 0: RLDICL(instruction, ppu); return;
                case 1: RLDICR(instruction, ppu); return;
                case 2: RLDIC(instruction, ppu); return;
                case 3: RLDIMI(instruction, ppu); return;
            }*/

            //throw new Exception($"Unknown RotateLeft PPU Instruction: {instruction.Raw:X8}");
        }

        //private static ulong GenerateMask64(int start, int end) {
        //    (start, end) = FlipStartEnd(start, end); //Because Big Endian

        //    int bitCount = end - start + 1;
        //    ulong mask = ((1UL << (end - start + 1)) - 1UL) << start;

        //    // Edge case when start == 0 and end == 63
        //    return bitCount == 64 ? ulong.MaxValue : mask;
        //}

        internal static (int, int) FlipStartEnd(int start, int end)
        {
            int actualStart = 63 - end;
            int actualEnd = 63 - start;
            return (actualStart, actualEnd);
        }

        internal static void RLDCL(PPUInstruction instruction, PPU ppu)
        {
            int amount = (int)(ppu.GPR[instruction.Rb].Value32 & 0x3F); //6-bits
            ulong rotated = BitOperations.RotateLeft(ppu.GPR[instruction.Rs].Value64, amount);
            ulong mask = GetRotationMask(instruction.MB_6, 63);

            ppu.GPR[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLDCR(PPUInstruction instruction, PPU ppu)
        {
            int amount = (int)(ppu.GPR[instruction.Rb].Value32 & 0x3F); //6-bits
            ulong rotated = BitOperations.RotateLeft(ppu.GPR[instruction.Rs].Value64, amount);
            ulong mask = GetRotationMask(0, instruction.ME_6);

            ppu.GPR[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLDIC(PPUInstruction instruction, PPU ppu)
        {
            ulong mask = GetRotationMask(instruction.MB_6, 63 - instruction.SH);
            ulong rotated = BitOperations.RotateLeft(ppu.GPR[instruction.Rs].Value64, instruction.SH);
            ppu.GPR[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLDICL(PPUInstruction instruction, PPU ppu)
        {
            ulong rotated = BitOperations.RotateLeft(ppu.GPR[instruction.Rs].Value64, instruction.SH);
            ulong mask = GetRotationMask(instruction.MB_6, 63);

            ppu.GPR[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLDICR(PPUInstruction instruction, PPU ppu)
        {
            ulong rotated = BitOperations.RotateLeft(ppu.GPR[instruction.Rs].Value64, instruction.SH);
            ulong mask = GetRotationMask(0, instruction.ME_6);

            ppu.GPR[instruction.Ra].Value64 = rotated & mask;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLDIMI(PPUInstruction instruction, PPU ppu)
        {
            ulong mask = GetRotationMask(instruction.MB_6, 63 - instruction.SH);
            ulong rotated = BitOperations.RotateLeft(ppu.GPR[instruction.Rs].Value64, instruction.SH);

            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ppu.GPR[instruction.Ra].Value64 = (rotated & mask) | (ra & (~mask));

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLWIMI(PPUInstruction instruction, PPU ppu)
        {
            int n = instruction.SH & 0x1F; //Rotate 32 takes only 5 bits of SH

            //The 64-bit value rotated is the lower 32 bit (2 copies)
            ulong value = ppu.GPR[instruction.Rs].Value32 | (((ulong)ppu.GPR[instruction.Rs].Value32) << 32);
            ulong rotated = BitOperations.RotateLeft(value, n);

            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            ulong ra = ppu.GPR[instruction.Ra].Value64;
            ppu.GPR[instruction.Ra].Value64 = (rotated & mask) | (ra & (~mask));

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLWINM(PPUInstruction instruction, PPU ppu)
        {
            int n = instruction.SH & 0x1F; //Rotate 32 takes only 5 bits of SH

            //The 64-bit value rotated is the lower 32 bit (2 copies)
            ulong value = ppu.GPR[instruction.Rs].Value32 | (((ulong)ppu.GPR[instruction.Rs].Value32) << 32);
            ulong rotated = BitOperations.RotateLeft(value, n);
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            ppu.GPR[instruction.Ra].Value64 = rotated & mask;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        internal static void RLWNM(PPUInstruction instruction, PPU ppu)
        {
            int n = (int)(ppu.GPR[instruction.Rb].Value32 & 0x1F); //Rotate 32 takes only 5 bits of RB

            //The 64-bit value rotated is the lower 32 bit (2 copies)
            ulong value = ppu.GPR[instruction.Rs].Value32 | (((ulong)ppu.GPR[instruction.Rs].Value32) << 32);
            ulong rotated = BitOperations.RotateLeft(value, n);
            ulong mask = GetRotationMask(instruction.MB_5 + 32, instruction.ME_5 + 32);

            ppu.GPR[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }
    }
}
