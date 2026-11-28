using PS3Sharp.Common;
using static PS3Sharp.Cell.Ppu.PpuThread;

namespace PS3Sharp.Cell.Ppu.Interpreter
{
    internal static unsafe partial class PpuInterpreter
    {
        internal enum MemAccessForm
        {
            DForm,
            DSForm,
            XForm,
        }

        // A helper to compute EA
        internal static uint GetEA(PpuInstruction instruction, PpuThread ppu, MemAccessForm form, bool checkRA)
        {
            /*ulong b = instruction.Ra != 0 ? ppu.GPR[instruction.Ra].Value64 : 0;
            short d = da_form? (short)(instruction.DS << 2) : (short)instruction.D;
            uint ea = (uint)(b + ((ulong)(long)d));*/

            long b;
            if (checkRA)
            {
                b = instruction.Ra != 0 ? ppu.Gpr[instruction.Ra].SValue64 : 0;
            }
            else
            {
                b = ppu.Gpr[instruction.Ra].SValue64;
            }

            uint ea;

            switch (form)
            {
                case MemAccessForm.DForm:
                    ea = (uint)(b + (long)(short)instruction.D);
                    break;

                case MemAccessForm.DSForm:
                    ea = (uint)(b + (long)(short)(instruction.Ds << 2));
                    break;

                case MemAccessForm.XForm:
                    ea = (uint)(b + ppu.Gpr[instruction.Rb].SValue64);
                    break;

                // Should be unreachable
                default:
                    throw new Exception($"Unknown Form {form}");
            }

            return ea;
        }

        // Store Byte Instructions
        private static void Stb(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.MemoryManager.Write<byte>(ea, ppu.Gpr[instruction.Rs].Value8);
        }

        private static void Stbx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<byte>(ea, ppu.Gpr[instruction.Rs].Value8);
        }

        private static void Stbu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.MemoryManager.Write<byte>(ea, ppu.Gpr[instruction.Rs].Value8);
            ppu.Gpr[instruction.Ra].Value64 = ea; //update
        }

        private static void Stbux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<byte>(ea, ppu.Gpr[instruction.Rs].Value8);
            ppu.Gpr[instruction.Ra].Value64 = ea; //update
        }

        // Store Half Instructions
        private static void Sth(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.MemoryManager.Write<BigEndian<ushort>>(ea, ppu.Gpr[instruction.Rs].Value16);
        }

        private static void Sthx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<BigEndian<ushort>>(ea, ppu.Gpr[instruction.Rs].Value16);
        }

        private static void Sthu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.MemoryManager.Write<BigEndian<ushort>>(ea, ppu.Gpr[instruction.Rs].Value16);
            ppu.Gpr[instruction.Ra].Value64 = ea; //update
        }

        private static void Sthux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<BigEndian<ushort>>(ea, ppu.Gpr[instruction.Rs].Value16);
            ppu.Gpr[instruction.Ra].Value64 = ea; //update
        }

        private static void Sthbrx(PpuInstruction instruction, PpuThread ppu)
        {
            // Write without endian conversion
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<ushort>(ea, ppu.Gpr[instruction.Rs].Value16);
        }

        // Store Word Instructions
        private static void Stw(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, ppu.Gpr[instruction.Rs].Value32);
        }

        private static void Stwx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, ppu.Gpr[instruction.Rs].Value32);
        }

        private static void Stwu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, ppu.Gpr[instruction.Rs].Value32);
            ppu.Gpr[instruction.Ra].Value64 = ea; //update
        }

        private static void Stwux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<BigEndian<uint>>(ea, ppu.Gpr[instruction.Rs].Value32);
            ppu.Gpr[instruction.Ra].Value64 = ea; //update
        }

        private static void Stwbrx(PpuInstruction instruction, PpuThread ppu)
        {
            // Write without endian conversion
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<uint>(ea, ppu.Gpr[instruction.Rs].Value32);
        }

        private static void Stwcx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);

            uint value = ppu.Gpr[instruction.Rs].Value32;
            bool ok = ppu.MemoryManager.StoreConditional<BigEndian<uint>>(ea, ppu.ThreadID, value);
            ppu.State->CR.WriteCrField(CrNumber.CR0, ok ? PpuThread.ConditionRegister.Eq : 0);
        }

        private static void Stmw(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            uint r = instruction.Rs;
            while (r <= 31)
            {
                ppu.MemoryManager.Write<BigEndian<uint>>(ea, ppu.Gpr[r].Value32);
                ea += 4;
                r++;
            }
        }

        // Store Double Word Instructions
        private static void Std(PpuInstruction instruction, PpuThread ppu)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            uint ea = GetEA(instruction, ppu, MemAccessForm.DSForm, checkRa);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, ppu.Gpr[instruction.Rs].Value64);
            if (update)
            {
                ppu.Gpr[instruction.Ra].Value64 = ea;
            }
        }

        private static void Stdx(PpuInstruction instruction, PpuThread ppu)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, checkRa);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, ppu.Gpr[instruction.Rs].Value64);
            if (update)
            {
                ppu.Gpr[instruction.Ra].Value64 = ea;
            }
        }

        private static void Stdux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<BigEndian<ulong>>(ea, ppu.Gpr[instruction.Rs].Value64);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Stdcx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);

            ulong value = ppu.Gpr[instruction.Rs].Value64;
            bool ok = ppu.MemoryManager.StoreConditional<BigEndian<ulong>>(ea, ppu.ThreadID, value);
            ppu.State->CR.WriteCrField(CrNumber.CR0, ok ? PpuThread.ConditionRegister.Eq : 0);
        }

        // Load Byte Instructions
        private static void Lbz(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
        }

        private static void Lbzx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
        }

        private static void Lbzu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lbzux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        // Load Half Instructions

        private static void Lhz(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ushort>>(ea); //zeroes the top 48-bits
        }

        private static void Lhzu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ushort>>(ea); //zeroes the top 48-bits
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lhzx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ushort>>(ea); //zeroes the top 48-bits
        }

        private static void Lhzux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ushort>>(ea); //zeroes the top 48-bits
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lhbrx(PpuInstruction instruction, PpuThread ppu)
        {
            // Read without endian conversion
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<ushort>(ea); //zeroes the top 48-bits
        }

        private static void Lha(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            long se = (long)ppu.MemoryManager.Read<BigEndian<short>>(ea);                     //sign extended
            ppu.Gpr[instruction.Rt].SValue64 = se;
        }

        private static void Lhax(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            long se = (long)ppu.MemoryManager.Read<BigEndian<short>>(ea);                     //sign extended
            ppu.Gpr[instruction.Rt].SValue64 = se;
        }

        private static void Lhau(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            long se = (long)ppu.MemoryManager.Read<BigEndian<short>>(ea);                     //sign extended
            ppu.Gpr[instruction.Rt].SValue64 = se;
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lhaux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            long se = (long)ppu.MemoryManager.Read<BigEndian<short>>(ea);                     //sign extended
            ppu.Gpr[instruction.Rt].Value64 = (ulong)se;
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }


        // Load Word Instructions
        private static void Lwz(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<uint>>(ea); //zeroes the top 32-bits
        }

        private static void Lwzu(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<uint>>(ea); //zeroes the top 32-bits
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lwzx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<uint>>(ea); //zeroes the top 32-bits
        }

        private static void Lwzux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<uint>>(ea); //zeroes the top 32-bits
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lwbrx(PpuInstruction instruction, PpuThread ppu)
        {
            // Read without endian conversion
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<uint>(ea);   //zeroes the top 32-bits
        }

        private static void Lwarx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint value = ppu.MemoryManager.LoadAndReserve<BigEndian<uint>>(ea, ppu.ThreadID);
            ppu.Gpr[instruction.Rt].Value64 = value;                         //zeroes the top 32-bits                
        }

        private static void LWA(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DSForm, true);
            long se = ppu.MemoryManager.Read<BigEndian<int>>(ea);   //Sign extended
            ppu.Gpr[instruction.Rt].SValue64 = se;
        }

        private static void Lwax(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            long se = ppu.MemoryManager.Read<BigEndian<int>>(ea);   //Sign extended
            ppu.Gpr[instruction.Rt].SValue64 = se;
        }

        private static void Lwaux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            long se = ppu.MemoryManager.Read<BigEndian<int>>(ea);  //Sign extended
            ppu.Gpr[instruction.Rt].SValue64 = se;
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Lmw(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            uint r = instruction.Rt;
            while (r <= 31)
            {
                ppu.Gpr[r].Value64 = ppu.MemoryManager.Read<BigEndian<uint>>(ea);
                ea += 4;
                r++;
            }
        }

        // Load Double Word Instructions
        private static void Ld(PpuInstruction instruction, PpuThread ppu)
        {
            switch (instruction.Raw & 0x3)
            {
                case 0:
                case 1:
                    LoadDoubleWord(instruction, ppu);
                    break;

                //LoadWordAlgebraic
                case 2:
                    LWA(instruction, ppu);
                    break;
            }
        }


        private static void LoadDoubleWord(PpuInstruction instruction, PpuThread ppu)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            uint ea = GetEA(instruction, ppu, MemAccessForm.DSForm, checkRa);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
            if (update)
            {
                ppu.Gpr[instruction.Ra].Value64 = ea;
            }
        }

        private static void Ldx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
        }

        private static void Ldux(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.Gpr[instruction.Rt].Value64 = ppu.MemoryManager.Read<BigEndian<ulong>>(ea);
            ppu.Gpr[instruction.Ra].Value64 = ea;
        }

        private static void Ldarx(PpuInstruction instruction, PpuThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ulong value = ppu.MemoryManager.LoadAndReserve<BigEndian<ulong>>(ea, ppu.ThreadID);
            ppu.Gpr[instruction.Rt].Value64 = value;
        }

        private static void Dcbz(PpuInstruction instruction, PpuThread ppu)
        {
            //Data Cache Block set to Zero
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ea &= ~127U;
            ppu.MemoryManager.ClearVirtualMemory(ea, 128);
        }

        // Load String instructions

        internal static void LSWI(PpuInstruction instruction, PpuThread ppu)
        {
            // TODO

            uint ra = instruction.Ra;
            uint rb = instruction.Rb;

            uint ea = ra == 0 ? 0 : ppu.Gpr[ra].Value32;
            uint n = rb == 0 ? 32 : rb;


            uint r = instruction.Rt - 1;
            uint i = 32;

            while (n > 0)
            {
                if (i == 32)
                {
                    r = (r + 1) & 31; // % 32
                    ppu.Gpr[r].Value64 = 0;

                }
            }
        }
    }
}
