using System.Numerics;
using static PS3Sharp.Cell.Ppu.PpuRotationMasks;

namespace PS3Sharp.Cell.Ppu.Interpreter
{
    internal static unsafe partial class PpuInterpreter
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
        private static void Addi(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Rt].Value64 = (instruction.Ra == 0 ? 0 : ppu.Gpr[instruction.Ra].Value64) + (ulong)instruction.Si64;
        }

        private static void Addis(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Rt].SValue64 = (instruction.Ra == 0 ? 0 : ppu.Gpr[instruction.Ra].SValue64) + (long)(int)(instruction.Si << 16);
        }

        private static void Addic(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong b = (ulong)instruction.Si64;
            ulong result = CkdAdd(ra, b, out bool carry);   // Add with carry check

            ppu.Gpr[instruction.Rt].Value64 = result;
            ppu.State->Xer.SetCA(carry ? 1U : 0U);
        }

        private static void AddicRecord(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong b = (ulong)instruction.Si64;
            ulong result = CkdAdd(ra, b, out bool carry);

            ppu.Gpr[instruction.Rt].Value64 = result;
            ppu.State->Xer.SetCA(carry ? 1U : 0U);

            // this instructions always updates CR0
            UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
        }

        private static void Adde(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong rb = ppu.Gpr[instruction.Rb].Value64;
            ulong ca = ppu.State->Xer.CA;

            ulong sum1 = CkdAdd(ra, rb, out bool carry1);
            ulong sum2 = CkdAdd(sum1, ca, out bool carry2);

            ppu.Gpr[instruction.Rt].Value64 = sum2;
            ppu.State->Xer.SetCA((carry1 || carry2) ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }


        private static void Add(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Rt].Value64 = ppu.Gpr[instruction.Rb].Value64 + ppu.Gpr[instruction.Ra].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        private static void Addc(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong rb = ppu.Gpr[instruction.Rb].Value64;
            ulong sum = CkdAdd(ra, rb, out bool carry);

            ppu.Gpr[instruction.Rt].Value64 = sum;
            ppu.State->Xer.SetCA(carry ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Addze(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong ca = ppu.State->Xer.CA;
            ulong sum = CkdAdd(ra, ca, out bool carry);

            ppu.Gpr[instruction.Rt].Value64 = sum;
            ppu.State->Xer.SetCA(carry ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Addme(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong ca = ppu.State->Xer.CA;
            ulong minusOne = ulong.MaxValue;

            ulong sum1 = CkdAdd(ra, ca, out bool carry1);
            ulong sum2 = CkdAdd(sum1, minusOne, out bool carry2);

            ppu.Gpr[instruction.Rt].Value64 = sum2;
            ppu.State->Xer.SetCA(carry1 || carry2 ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        // Subtract instructions
        private static void Subf(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Rt].Value64 = ppu.Gpr[instruction.Rb].Value64 - ppu.Gpr[instruction.Ra].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        private static void Subfc(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong rb = ppu.Gpr[instruction.Rb].Value64;

            ulong sum1 = CkdAdd(~ra, rb, out bool carry1);
            ulong sum2 = CkdAdd(sum1, 1, out bool carry2);

            ppu.State->Xer.SetCA((carry1 || carry2) ? 1U : 0U);
            ppu.Gpr[instruction.Rt].Value64 = sum2;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        private static void Subfic(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong b = (ulong)instruction.Si64;

            ulong sum1 = CkdAdd(~ra, b, out bool carry1);
            ulong sum2 = CkdAdd(sum1, 1, out bool carry2);

            ppu.State->Xer.SetCA((carry1 || carry2) ? 1U : 0U);
            ppu.Gpr[instruction.Rt].Value64 = sum2;
        }


        private static void Subfe(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong rb = ppu.Gpr[instruction.Rb].Value64;
            ulong ca = ppu.State->Xer.CA;

            ulong sum1 = CkdAdd(~ra, rb, out bool carry1);
            ulong sum2 = CkdAdd(sum1, ca, out bool carry2);

            ppu.Gpr[instruction.Rt].Value64 = sum2;
            ppu.State->Xer.SetCA((carry1 || carry2) ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Subfze(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong ca = ppu.State->Xer.CA;
            ulong sum = CkdAdd(~ra, ca, out bool carry);

            ppu.Gpr[instruction.Rt].Value64 = sum;
            ppu.State->Xer.SetCA(carry ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Subfme(PpuInstruction instruction, PpuThread ppu)
        {
            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ulong ca = ppu.State->Xer.CA;
            ulong minusOne = ulong.MaxValue;

            ulong result1 = CkdAdd(~ra, ca, out bool carry1);
            ulong result2 = CkdAdd(result1, minusOne, out bool carry2);

            ppu.Gpr[instruction.Rt].Value64 = result2;
            ppu.State->Xer.SetCA(carry1 || carry2 ? 1U : 0U);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        // General logic instructions
        internal static void Andi(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 & instruction.Ui;
            UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
        }

        internal static void Andis(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 & ((ulong)instruction.Ui << 16);
            UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
        }

        private static void Ori(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 | instruction.Ui;
            // Does not affect CR
        }

        private static void Oris(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 | ((ulong)instruction.Ui << 16);
            // Does not affect CR
        }

        private static void Xori(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 ^ instruction.Ui;
            // Does not affect CR
        }

        private static void Xoris(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 ^ ((ulong)instruction.Ui << 16);
            // Does not affect CR
        }

        internal static void And(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 & ppu.Gpr[instruction.Rb].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Or(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 | ppu.Gpr[instruction.Rb].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Xor(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 ^ ppu.Gpr[instruction.Rb].Value64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Nand(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ~(ppu.Gpr[instruction.Rs].Value64 & ppu.Gpr[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Nor(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ~(ppu.Gpr[instruction.Rs].Value64 | ppu.Gpr[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Eqv(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ~(ppu.Gpr[instruction.Rs].Value64 ^ ppu.Gpr[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Andc(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 & (~ppu.Gpr[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Orc(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].Value64 = ppu.Gpr[instruction.Rs].Value64 | (~ppu.Gpr[instruction.Rb].Value64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Neg(PpuInstruction instruction, PpuThread ppu)
        {
            //RT = ¬(RA) + 1
            ppu.Gpr[instruction.Rt].SValue64 = -ppu.Gpr[instruction.Ra].SValue64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        // Condition register bit logic opcodes
        internal static (uint ba, uint bb) GetBitsAB(PpuInstruction instruction, PpuThread ppu)
        {
            //Helper for condition register bit opcodes
            uint ba = (ppu.State->CR.Raw >> (int)(31 - instruction.Ba)) & 1;
            uint bb = (ppu.State->CR.Raw >> (int)(31 - instruction.Bb)) & 1;
            return (ba, bb);
        }

        internal static void Cror(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba | bb; //OR
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Crorc(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba | (~bb); //ORC
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Crand(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba & bb; //AND
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Crandc(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba & (~bb); //ANDC
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Crxor(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ba ^ bb; //XOR
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Crnand(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ~(ba & bb); //NAND
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Crnor(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ~(ba | bb); //NOR
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }

        internal static void Creqv(PpuInstruction instruction, PpuThread ppu)
        {
            (uint ba, uint bb) = GetBitsAB(instruction, ppu);
            uint bt_value = ~(ba ^ bb); //EQV (~XOR)
            ppu.State->CR.WriteCrBit(instruction.Bt, bt_value & 1);
        }


        // Multiply instructions
        internal static void Mulli(PpuInstruction instruction, PpuThread ppu)
        { //Multiply low immediate
            ppu.Gpr[instruction.Rt].SValue64 = ppu.Gpr[instruction.Ra].SValue64 * instruction.Si64;
            //No special registers affected
        }

        internal static void Mullw(PpuInstruction instruction, PpuThread ppu)
        { //Multiply word, write 64-bit result (signed)
            ppu.Gpr[instruction.Rt].SValue64 = (long)ppu.Gpr[instruction.Ra].SValue32 * (long)ppu.Gpr[instruction.Rb].SValue32;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Mulhw(PpuInstruction instruction, PpuThread ppu)
        { //Multiply word, write high 32-bit (signed)
            long result = ((long)ppu.Gpr[instruction.Ra].SValue32) * ((long)ppu.Gpr[instruction.Rb].SValue32);
            ppu.Gpr[instruction.Rt].SValue64 = (int)(result >> 32);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Mulhwu(PpuInstruction instruction, PpuThread ppu)
        { //Multiply word, write high 32-bit (unsigned)
            ulong result = ((ulong)ppu.Gpr[instruction.Ra].Value32) * ((ulong)ppu.Gpr[instruction.Rb].Value32);
            ppu.Gpr[instruction.Rt].Value64 = (uint)(result >> 32);

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Mulld(PpuInstruction instruction, PpuThread ppu)
        { //Multiply double word (signed)
            ppu.Gpr[instruction.Rt].SValue64 = ppu.Gpr[instruction.Ra].SValue64 * ppu.Gpr[instruction.Rb].SValue64;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Mulhd(PpuInstruction instruction, PpuThread ppu)
        { //Multiply double word, write high 64-bit (signed)
            Int128 result = ((Int128)ppu.Gpr[instruction.Ra].SValue64) * ((Int128)ppu.Gpr[instruction.Rb].SValue64);
            ppu.Gpr[instruction.Rt].SValue64 = (long)(result >> 64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Mulhdu(PpuInstruction instruction, PpuThread ppu)
        { //Multiply double word, write high 64-bit (unsigned)
            UInt128 result = ((UInt128)ppu.Gpr[instruction.Ra].Value64) * ((UInt128)ppu.Gpr[instruction.Rb].Value64);
            ppu.Gpr[instruction.Rt].Value64 = (ulong)(result >> 64);
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        // Divide instructions
        internal static void Divd(PpuInstruction instruction, PpuThread ppu)
        {
            long a = ppu.Gpr[instruction.Ra].SValue64;
            long b = ppu.Gpr[instruction.Rb].SValue64;

            if (b == 0 || (((ulong)a == 0x8000_0000_0000_0000) && b == -1))
            {
                ppu.Gpr[instruction.Rt].SValue64 = 0;
            }
            else
            {
                ppu.Gpr[instruction.Rt].SValue64 = a / b;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Divdu(PpuInstruction instruction, PpuThread ppu)
        {
            ulong a = ppu.Gpr[instruction.Ra].Value64;
            ulong b = ppu.Gpr[instruction.Rb].Value64;

            if (b == 0)
            {
                ppu.Gpr[instruction.Rt].Value64 = 0;
            }
            else
            {
                ppu.Gpr[instruction.Rt].Value64 = a / b;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Divw(PpuInstruction instruction, PpuThread ppu)
        {
            int a = ppu.Gpr[instruction.Ra].SValue32;
            int b = ppu.Gpr[instruction.Rb].SValue32;

            if (b == 0 || (((uint)a == 0x8000_0000) && b == -1))
            {
                ppu.Gpr[instruction.Rt].Value64 = 0;
            }
            else
            {
                // no sign extension to 64-bits, apparently 
                uint result = (uint)(a / b);
                ppu.Gpr[instruction.Rt].Value64 = result;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        internal static void Divwu(PpuInstruction instruction, PpuThread ppu)
        {
            uint a = ppu.Gpr[instruction.Ra].Value32;
            uint b = ppu.Gpr[instruction.Rb].Value32;

            if (b == 0)
            {
                ppu.Gpr[instruction.Rt].Value64 = 0;
            }
            else
            {
                ppu.Gpr[instruction.Rt].Value64 = a / b;
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Rt].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }

            if (instruction.Oe == 1)
            {
                Console.WriteLine($"[PPU] Ignored OE bit!");
            }
        }

        // Shift instructions
        internal static void Sld(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = (int)(ppu.Gpr[instruction.Rb].Value64 & 0x7F);
            ulong result = shift >= 64 ? 0 : ppu.Gpr[instruction.Rs].Value64 << shift;
            ppu.Gpr[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Slw(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = (int)(ppu.Gpr[instruction.Rb].Value64 & 0x3F);
            ulong result = shift >= 32 ? 0 : ppu.Gpr[instruction.Rs].Value32 << shift;
            ppu.Gpr[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Srad(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = (int)(ppu.Gpr[instruction.Rb].Value32 & 0x7F);
            int mask = (1 << shift) - 1;
            bool hasOnes = (ppu.Gpr[instruction.Rs].SValue64 & mask) > 0;

            if (shift > 63)
            {
                uint sign = ppu.Gpr[instruction.Rs].SValue64 < 0 ? 1U : 0U;
                ppu.Gpr[instruction.Ra].SValue64 = sign == 1 ? -1 : 0;
                ppu.State->Xer.SetCA(sign);

            }
            else
            {

                //Arithmetic shift
                ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue64 >> shift;

                if (shift > 0)
                {
                    if (ppu.Gpr[instruction.Rs].SValue64 < 0 && hasOnes)
                    {
                        ppu.State->Xer.SetCA(1);
                    }
                    else
                    {
                        ppu.State->Xer.SetCA(0);
                    }
                }
                else
                {
                    ppu.State->Xer.SetCA(0);
                }
            }


            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Sradi(PpuInstruction instruction, PpuThread ppu)
        {
            int mask = (1 << instruction.Sh) - 1;
            bool hasOnes = (ppu.Gpr[instruction.Rs].SValue64 & mask) > 0;

            //Arithmetic shift
            ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue64 >> instruction.Sh;

            if (instruction.Sh > 0)
            {
                if (ppu.Gpr[instruction.Rs].SValue64 < 0 && hasOnes)
                {
                    ppu.State->Xer.SetCA(1);
                }
                else
                {
                    ppu.State->Xer.SetCA(0);
                }
            }
            else
            {
                ppu.State->Xer.SetCA(0);
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Sraw(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = (int)(ppu.Gpr[instruction.Rb].Value32 & 0x3F);
            int mask = (1 << shift) - 1;
            bool hasOnes = (ppu.Gpr[instruction.Rs].SValue32 & mask) > 0;

            if (shift > 31)
            {
                uint sign = ppu.Gpr[instruction.Rs].SValue32 < 0 ? 1U : 0U;
                ppu.Gpr[instruction.Ra].SValue64 = sign == 1 ? -1 : 0;
                ppu.State->Xer.SetCA(sign);

            }
            else
            {

                //Arithmetic shift
                ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue32 >> shift;

                if (shift > 0)
                {
                    if (ppu.Gpr[instruction.Rs].SValue32 < 0 && hasOnes)
                    {
                        ppu.State->Xer.SetCA(1);
                    }
                    else
                    {
                        ppu.State->Xer.SetCA(0);
                    }
                }
                else
                {
                    ppu.State->Xer.SetCA(0);
                }
            }


            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Srawi(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = instruction.Sh & 0x1F; //5 bits
            int mask = (1 << (shift)) - 1;
            bool hasOnes = (ppu.Gpr[instruction.Rs].SValue32 & mask) > 0;

            //Arithmetic shift
            ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue32 >> shift;

            if (shift > 0)
            {
                if (ppu.Gpr[instruction.Rs].SValue32 < 0 && hasOnes)
                {
                    ppu.State->Xer.SetCA(1);
                }
                else
                {
                    ppu.State->Xer.SetCA(0);
                }

            }
            else
            {
                ppu.State->Xer.SetCA(0);
            }

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Srd(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = (int)(ppu.Gpr[instruction.Rb].Value64 & 0x7F);
            ulong result = shift >= 64 ? 0 : ppu.Gpr[instruction.Rs].Value64 >> shift;
            ppu.Gpr[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Srw(PpuInstruction instruction, PpuThread ppu)
        {
            int shift = (int)(ppu.Gpr[instruction.Rb].Value64 & 0x3F);
            ulong result = shift >= 32 ? 0 : ppu.Gpr[instruction.Rs].Value32 >> shift;
            ppu.Gpr[instruction.Ra].Value64 = result;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        // Rotate instructions
        private static void RotateLeft(PpuInstruction instruction, PpuThread ppu)
        {
            uint field = (instruction.Raw >> 1) & 0xF; // bits 27 - 30 in BE

            switch (field)
            {
                case 0:
                case 1:
                    Rldicl(instruction, ppu);
                    break;

                case 2:
                case 3:
                    Rldicr(instruction, ppu);
                    break;

                case 4:
                case 5:
                    Rldic(instruction, ppu);
                    break;

                case 6:
                case 7:
                    Rldimi(instruction, ppu);
                    break;

                case 8:
                    Rldcl(instruction, ppu);
                    break;

                case 9:
                    Rldcr(instruction, ppu);
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

        internal static void Rldcl(PpuInstruction instruction, PpuThread ppu)
        {
            int amount = (int)(ppu.Gpr[instruction.Rb].Value32 & 0x3F); //6-bits
            ulong rotated = BitOperations.RotateLeft(ppu.Gpr[instruction.Rs].Value64, amount);
            ulong mask = GetRotationMask(instruction.Mb6, 63);

            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rldcr(PpuInstruction instruction, PpuThread ppu)
        {
            int amount = (int)(ppu.Gpr[instruction.Rb].Value32 & 0x3F); //6-bits
            ulong rotated = BitOperations.RotateLeft(ppu.Gpr[instruction.Rs].Value64, amount);
            ulong mask = GetRotationMask(0, instruction.Me6);

            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rldic(PpuInstruction instruction, PpuThread ppu)
        {
            ulong mask = GetRotationMask(instruction.Mb6, 63 - instruction.Sh);
            ulong rotated = BitOperations.RotateLeft(ppu.Gpr[instruction.Rs].Value64, instruction.Sh);
            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rldicl(PpuInstruction instruction, PpuThread ppu)
        {
            ulong rotated = BitOperations.RotateLeft(ppu.Gpr[instruction.Rs].Value64, instruction.Sh);
            ulong mask = GetRotationMask(instruction.Mb6, 63);

            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rldicr(PpuInstruction instruction, PpuThread ppu)
        {
            ulong rotated = BitOperations.RotateLeft(ppu.Gpr[instruction.Rs].Value64, instruction.Sh);
            ulong mask = GetRotationMask(0, instruction.Me6);

            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rldimi(PpuInstruction instruction, PpuThread ppu)
        {
            ulong mask = GetRotationMask(instruction.Mb6, 63 - instruction.Sh);
            ulong rotated = BitOperations.RotateLeft(ppu.Gpr[instruction.Rs].Value64, instruction.Sh);

            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ppu.Gpr[instruction.Ra].Value64 = (rotated & mask) | (ra & (~mask));

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rlwimi(PpuInstruction instruction, PpuThread ppu)
        {
            int n = instruction.Sh & 0x1F; //Rotate 32 takes only 5 bits of SH

            //The 64-bit value rotated is the lower 32 bit (2 copies)
            ulong value = ppu.Gpr[instruction.Rs].Value32 | (((ulong)ppu.Gpr[instruction.Rs].Value32) << 32);
            ulong rotated = BitOperations.RotateLeft(value, n);

            ulong mask = GetRotationMask(instruction.Mb5 + 32, instruction.Me5 + 32);

            ulong ra = ppu.Gpr[instruction.Ra].Value64;
            ppu.Gpr[instruction.Ra].Value64 = (rotated & mask) | (ra & (~mask));

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rlwinm(PpuInstruction instruction, PpuThread ppu)
        {
            int n = instruction.Sh & 0x1F; //Rotate 32 takes only 5 bits of SH

            //The 64-bit value rotated is the lower 32 bit (2 copies)
            ulong value = ppu.Gpr[instruction.Rs].Value32 | (((ulong)ppu.Gpr[instruction.Rs].Value32) << 32);
            ulong rotated = BitOperations.RotateLeft(value, n);
            ulong mask = GetRotationMask(instruction.Mb5 + 32, instruction.Me5 + 32);

            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        internal static void Rlwnm(PpuInstruction instruction, PpuThread ppu)
        {
            int n = (int)(ppu.Gpr[instruction.Rb].Value32 & 0x1F); //Rotate 32 takes only 5 bits of RB

            //The 64-bit value rotated is the lower 32 bit (2 copies)
            ulong value = ppu.Gpr[instruction.Rs].Value32 | (((ulong)ppu.Gpr[instruction.Rs].Value32) << 32);
            ulong rotated = BitOperations.RotateLeft(value, n);
            ulong mask = GetRotationMask(instruction.Mb5 + 32, instruction.Me5 + 32);

            ppu.Gpr[instruction.Ra].Value64 = rotated & mask;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }
    }
}
