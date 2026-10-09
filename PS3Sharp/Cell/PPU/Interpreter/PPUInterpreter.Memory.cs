using static PS3Sharp.Cell.PPU.PPUThread;

namespace PS3Sharp.Cell.PPU.Interpreter
{
    internal static unsafe partial class PPUInterpreter
    {
        internal enum MemAccessForm
        {
            DForm,
            DSForm,
            XForm,
        }

        // A helper to compute EA
        internal static uint GetEA(PPUInstruction instruction, PPUThread ppu, MemAccessForm form, bool checkRA)
        {
            /*ulong b = instruction.Ra != 0 ? ppu.GPR[instruction.Ra].Value64 : 0;
            short d = da_form? (short)(instruction.DS << 2) : (short)instruction.D;
            uint ea = (uint)(b + ((ulong)(long)d));*/

            long b;
            if (checkRA)
            {
                b = instruction.Ra != 0 ? ppu.GPR[instruction.Ra].SValue64 : 0;
            }
            else
            {
                b = ppu.GPR[instruction.Ra].SValue64;
            }

            uint ea;

            switch (form)
            {
                case MemAccessForm.DForm:
                    ea = (uint)(b + (long)(short)instruction.D);
                    break;

                case MemAccessForm.DSForm:
                    ea = (uint)(b + (long)(short)(instruction.DS << 2));
                    break;

                case MemAccessForm.XForm:
                    ea = (uint)(b + ppu.GPR[instruction.Rb].SValue64);
                    break;

                // Should be unreachable
                default:
                    throw new Exception($"Unknown Form {form}");
            }

            return ea;
        }

        // Store Byte Instructions
        private static void STB(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.MemoryManager.Write<byte>(ea, ppu.GPR[instruction.Rs].Value8);
        }

        private static void STBX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<byte>(ea, ppu.GPR[instruction.Rs].Value8);
        }

        private static void STBU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.MemoryManager.Write<byte>(ea, ppu.GPR[instruction.Rs].Value8);
            ppu.GPR[instruction.Ra].Value64 = ea; //update
        }

        private static void STBUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<byte>(ea, ppu.GPR[instruction.Rs].Value8);
            ppu.GPR[instruction.Ra].Value64 = ea; //update
        }

        // Store Half Instructions
        private static void STH(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.MemoryManager.Write<ushort>(ea, ppu.GPR[instruction.Rs].Value16);
        }

        private static void STHX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<ushort>(ea, ppu.GPR[instruction.Rs].Value16);
        }

        private static void STHU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.MemoryManager.Write<ushort>(ea, ppu.GPR[instruction.Rs].Value16);
            ppu.GPR[instruction.Ra].Value64 = ea; //update
        }

        private static void STHUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<ushort>(ea, ppu.GPR[instruction.Rs].Value16);
            ppu.GPR[instruction.Ra].Value64 = ea; //update
        }

        private static void STHBRX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.WriteBE<ushort>(ea, ppu.GPR[instruction.Rs].Value16);
        }

        // Store Word Instructions
        private static void STW(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.MemoryManager.Write<uint>(ea, ppu.GPR[instruction.Rs].Value32);
        }

        private static void STWX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.Write<uint>(ea, ppu.GPR[instruction.Rs].Value32);
        }

        private static void STWU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.MemoryManager.Write<uint>(ea, ppu.GPR[instruction.Rs].Value32);
            ppu.GPR[instruction.Ra].Value64 = ea; //update
        }

        private static void STWUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<uint>(ea, ppu.GPR[instruction.Rs].Value32);
            ppu.GPR[instruction.Ra].Value64 = ea; //update
        }

        private static void STWBRX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.MemoryManager.WriteBE<uint>(ea, ppu.GPR[instruction.Rs].Value32);
        }

        private static void STWCX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);

            uint value = ppu.GPR[instruction.Rs].Value32;
            bool ok = ppu.MemoryManager.StoreConditional<uint>(ea, ppu.ThreadID, value, ppu);
            ppu.State->CR.WriteCRField(CRNumber.CR0, ok ? PPUThread.ConditionRegister.EQ : 0);
        }

        private static void STMW(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            uint r = instruction.Rs;
            while (r <= 31)
            {
                ppu.MemoryManager.Write<uint>(ea, ppu.GPR[r].Value32);
                ea += 4;
                r++;
            }
        }

        // Store Double Word Instructions
        private static void STD(PPUInstruction instruction, PPUThread ppu)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            uint ea = GetEA(instruction, ppu, MemAccessForm.DSForm, checkRa);
            ppu.MemoryManager.Write<ulong>(ea, ppu.GPR[instruction.Rs].Value64);
            if (update)
            {
                ppu.GPR[instruction.Ra].Value64 = ea;
            }
        }

        private static void STDX(PPUInstruction instruction, PPUThread ppu)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, checkRa);
            ppu.MemoryManager.Write<ulong>(ea, ppu.GPR[instruction.Rs].Value64);
            if (update)
            {
                ppu.GPR[instruction.Ra].Value64 = ea;
            }
        }

        private static void STDUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.MemoryManager.Write<ulong>(ea, ppu.GPR[instruction.Rs].Value64);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void STDCX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);

            ulong value = ppu.GPR[instruction.Rs].Value64;
            bool ok = ppu.MemoryManager.StoreConditional<ulong>(ea, ppu.ThreadID, value, ppu);
            ppu.State->CR.WriteCRField(CRNumber.CR0, ok ? PPUThread.ConditionRegister.EQ : 0);
        }

        // Load Byte Instructions
        private static void LBZ(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
        }

        private static void LBZX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
        }

        private static void LBZU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LBZUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<byte>(ea); //zeroes the top 56-bits
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        // Load Half Instructions

        private static void LHZ(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ushort>(ea); //zeroes the top 48-bits
        }

        private static void LHZU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ushort>(ea); //zeroes the top 48-bits
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LHZX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ushort>(ea); //zeroes the top 48-bits
        }

        private static void LHZUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ushort>(ea); //zeroes the top 48-bits
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LHBRX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.ReadBE<ushort>(ea); //zeroes the top 48-bits
        }

        private static void LHA(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            long se = (long)ppu.MemoryManager.Read<short>(ea);                     //sign extended
            ppu.GPR[instruction.Rt].SValue64 = se;
        }

        private static void LHAX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            long se = (long)ppu.MemoryManager.Read<short>(ea);                     //sign extended
            ppu.GPR[instruction.Rt].SValue64 = se;
        }

        private static void LHAU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            long se = (long)ppu.MemoryManager.Read<short>(ea);                     //sign extended
            ppu.GPR[instruction.Rt].SValue64 = se;
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LHAUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            long se = (long)ppu.MemoryManager.Read<short>(ea);                     //sign extended
            ppu.GPR[instruction.Rt].Value64 = (ulong)se;
            ppu.GPR[instruction.Ra].Value64 = ea;
        }


        // Load Word Instructions
        private static void LWZ(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<uint>(ea); //zeroes the top 32-bits
        }

        private static void LWZU(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<uint>(ea); //zeroes the top 32-bits
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LWZX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<uint>(ea); //zeroes the top 32-bits
        }

        private static void LWZUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<uint>(ea); //zeroes the top 32-bits
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LWBRX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.ReadBE<uint>(ea); //zeroes the top 32-bits
        }

        private static void LWARX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            uint value = ppu.MemoryManager.LoadAndReserve<uint>(ea, ppu.ThreadID, ppu);
            ppu.GPR[instruction.Rt].Value64 = value;                         //zeroes the top 32-bits                
        }

        private static void LWA(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DSForm, true);
            long se = ppu.MemoryManager.Read<int>(ea);   //Sign extended
            ppu.GPR[instruction.Rt].SValue64 = se;
        }

        private static void LWAX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            long se = ppu.MemoryManager.Read<int>(ea);   //Sign extended
            ppu.GPR[instruction.Rt].SValue64 = se;
        }

        private static void LWAUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            long se = ppu.MemoryManager.Read<int>(ea);  //Sign extended
            ppu.GPR[instruction.Rt].SValue64 = se;
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LMW(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.DForm, true);
            uint r = instruction.Rt;
            while (r <= 31)
            {
                ppu.GPR[r].Value64 = ppu.MemoryManager.Read<uint>(ea);
                ea += 4;
                r++;
            }
        }

        // Load Double Word Instructions
        private static void LD(PPUInstruction instruction, PPUThread ppu)
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


        private static void LoadDoubleWord(PPUInstruction instruction, PPUThread ppu)
        {
            bool update = (instruction.Raw & 0x3) == 1;
            bool checkRa = !update;

            uint ea = GetEA(instruction, ppu, MemAccessForm.DSForm, checkRa);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ulong>(ea);
            if (update)
            {
                ppu.GPR[instruction.Ra].Value64 = ea;
            }
        }

        private static void LDX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ulong>(ea);
        }

        private static void LDUX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, false);
            ppu.GPR[instruction.Rt].Value64 = ppu.MemoryManager.Read<ulong>(ea);
            ppu.GPR[instruction.Ra].Value64 = ea;
        }

        private static void LDARX(PPUInstruction instruction, PPUThread ppu)
        {
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ulong value = ppu.MemoryManager.LoadAndReserve<ulong>(ea, ppu.ThreadID, ppu);
            ppu.GPR[instruction.Rt].Value64 = value;
        }

        private static void DCBZ(PPUInstruction instruction, PPUThread ppu)
        {
            //Data Cache Block set to Zero
            uint ea = GetEA(instruction, ppu, MemAccessForm.XForm, true);
            ea &= ~127U;
            ppu.MemoryManager.ClearMemory(ea, 128);
        }

        // Load String instructions

        internal static void LSWI(PPUInstruction instruction, PPUThread ppu)
        {
            // TODO

            uint ra = instruction.Ra;
            uint rb = instruction.Rb;

            uint ea = ra == 0 ? 0 : ppu.GPR[ra].Value32;
            uint n = rb == 0 ? 32 : rb;


            uint r = instruction.Rt - 1;
            uint i = 32;

            while (n > 0)
            {
                if (i == 32)
                {
                    r = (r + 1) & 31; // % 32
                    ppu.GPR[r].Value64 = 0;

                }
            }
        }
    }
}
