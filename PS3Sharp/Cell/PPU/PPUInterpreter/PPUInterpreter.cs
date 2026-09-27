using PS3Sharp.Cell.PPU.PPURecompiler.IR;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.PPU.PPU;

namespace PS3Sharp.Cell.PPU.PPUInterpreter
{
    public static unsafe partial class PPUInterpreter
    {
        public const uint NOP = 0x60000000;
        public static Stopwatch stopwatch = new Stopwatch(); //debug
        public static bool Log = false; //debug
        static bool lastBranch;         //debug
        static bool crash = false;
        public static void CrashAt(PPU ppu, uint address, bool printRegisters = false, bool printTrace = false, bool dumpMemory = false)
        {
            if (ppu.State->PC == address)
            {
                if (printRegisters)
                {
                    PrintAllGPRs(ppu);
                }

                if (dumpMemory)
                {
                    uint base_addr = 0xd736e8 & ~15u;
                    uint length = 512;
                    uint counter = 0;

                    for (uint i = 0; i < length; i += 1)
                    {
                        uint addr = base_addr + i;
                        uint w = MemoryManager.Read<byte>(addr);
                        Console.Write($"0x{w:X2} ");

                        counter++;
                        if (counter % 16 == 0)
                        {
                            Console.Write("\n");
                        }
                    }
                }

                if (printTrace)
                {
                    ppu.PrintTraceQueue();
                }

                throw new Exception($"PPU reached crash address");
            }
        }

        public static void Step(PPU ppu)
        {
            //if (ppu.State->PC == 0x0005d918 || ppu.State->PC == 0x00876858) {
            //    ppu.State->PC = ppu.State->LR;
            //}

            //CrashAt(ppu, address: 0x0060b5b8, printRegisters: true, printTrace: false, dumpMemory: true);


            PPUInstruction instruction;
            instruction.Raw = MemoryManager.ReadPPUInstruction(ppu.State->PC);

            ppu.AddTrace(ppu.State->PC, instruction);

            if (Log && ppu.ThreadName == "main_thread")
            {
                Console.WriteLine($"[PPU: {ppu.ThreadName}] [0x{ppu.State->PC:X8}] 0x{instruction.Raw:X8}");
            }

            //if (ppu.IsMainThread && instruction.Raw == 0x48000000) {
            //    Console.WriteLine($"[main_thread] Infinite loop reached at: 0x{ppu.State->PC:X8}");
            //    throw new Exception();
            //}

            uint oldPC = ppu.State->PC;
            uint oldLR = ppu.State->LR;
            ppu.State->PC += 4;
            ppu.State->TB++;

            if (instruction.Raw != NOP)
            {
                Execute(instruction, ppu);
            }
        }

        public static void PrintAllGPRs(PPU ppu)
        {
            for (int i = 0; i < 32; i++)
            {
                Console.WriteLine($"GPR[{i}] = 0x{ppu.GPR[i].Value64:X16}");
            }

            Console.WriteLine($"LR = 0x{ppu.State->LR:X8}");
            Console.WriteLine($"CR = 0x{ppu.State->CR.Raw:X8}");
            Console.WriteLine($"CTR = 0x{ppu.State->CTR:X16}");
            Console.WriteLine($"CA = 0x{ppu.State->XER.CA:X8}");
        }

        public static void PrintCallstack(PPU ppu)
        {
            int length = ppu.CallStack.Count;
            int lastIndex = length - 1;
            for (int i = 0; i < length; i++)
            {
                uint addr = ppu.CallStack[i];
                Console.Write($"0x{addr:X8}" + (i == lastIndex ? "\n" : " -> "));
            }
        }

        private static void Execute(PPUInstruction instruction, PPU ppu)
        {
            switch (instruction.Op)
            {
                case Op.Altivec: Altivec(instruction, ppu); break;

                case Op.MULLI: MULLI(instruction, ppu); break;
                case Op.SUBFIC: SUBFIC(instruction, ppu); break;
                case Op.CMPLI: CMPLI(instruction, ppu); break;
                case Op.CMPI: CMPI(instruction, ppu); break;
                case Op.ADDIC: ADDIC(instruction, ppu); break;
                case Op.ADDIC_Record: ADDIC_Record(instruction, ppu); break;
                case Op.ADDI: ADDI(instruction, ppu); break;
                case Op.ADDIS: ADDIS(instruction, ppu); break;
                case Op.BC: BC(instruction, ppu); break;
                case Op.SC: SC(instruction, ppu); break;
                case Op.Branch: Branch(instruction, ppu); break;
                case Op.G13: Group13(instruction, ppu); break;
                case Op.RLWIMI: RLWIMI(instruction, ppu); break;
                case Op.RLWINM: RLWINM(instruction, ppu); break;

                case Op.RLWNM: RLWNM(instruction, ppu); break;
                case Op.ORI: ORI(instruction, ppu); break;
                case Op.ORIS: ORIS(instruction, ppu); break;
                case Op.XORI: XORI(instruction, ppu); break;
                case Op.XORIS: XORIS(instruction, ppu); break;

                case Op.ANDI: ANDI(instruction, ppu); break;
                case Op.ANDIS: ANDIS(instruction, ppu); break;

                case Op.RotateLeft: RotateLeft(instruction, ppu); break;
                case Op.G1F: Group1F(instruction, ppu); break;
                case Op.G3B: Group3B(instruction, ppu); break;
                case Op.G3F: Group3F(instruction, ppu); break;

                case Op.LWZ: LWZ(instruction, ppu); break;
                case Op.LWZU: LWZU(instruction, ppu); break;
                case Op.LBZ: LBZ(instruction, ppu); break;
                case Op.LBZU: LBZU(instruction, ppu); break;
                case Op.STW: STW(instruction, ppu); break;
                case Op.STWU: STWU(instruction, ppu); break;
                case Op.STB: STB(instruction, ppu); break;
                case Op.STBU: STBU(instruction, ppu); break;
                case Op.LHZ: LHZ(instruction, ppu); break;
                case Op.LHZU: LHZU(instruction, ppu); break;
                case Op.LHA: LHA(instruction, ppu); break;
                case Op.LHAU: LHAU(instruction, ppu); break;

                case Op.STH: STH(instruction, ppu); break;
                case Op.STHU: STHU(instruction, ppu); break;
                case Op.LMW: LMW(instruction, ppu); break;
                case Op.STMW: STMW(instruction, ppu); break;

                case Op.LFS: LFS(instruction, ppu); break;
                case Op.LFSU: LFSU(instruction, ppu); break;
                case Op.LFD: LFD(instruction, ppu); break;
                case Op.LFDU: LFDU(instruction, ppu); break;
                case Op.STFS: STFS(instruction, ppu); break;
                case Op.STFSU: STFSU(instruction, ppu); break;
                case Op.STFD: STFD(instruction, ppu); break;
                case Op.STFDU: STFDU(instruction, ppu); break;

                case Op.LD: LD(instruction, ppu); break;
                case Op.STD: STD(instruction, ppu); break;

                default:
                    //PrintTraceQueue();
                    throw new Exception($"Unhandled PPU OP: 0x{instruction.Op:X} = {instruction.Op} -- raw = 0x{instruction.Raw:X8} r3 = {ppu.GPR[3]}");
            }
        }


        private static void Group13(PPUInstruction instruction, PPU ppu)
        {
            switch (instruction.G13)
            {
                case G13.MCRF: MCRF(instruction, ppu); break;
                case G13.BCLR: BCLR(instruction, ppu); break;
                case G13.ISYNC: /* ISYNC */ break;

                //Condition Register bit op
                case G13.CRNOR: CRNOR(instruction, ppu); break;
                case G13.CRANDC: CRANDC(instruction, ppu); break;
                case G13.CRXOR: CRXOR(instruction, ppu); break;
                case G13.CRNAND: CRNAND(instruction, ppu); break;
                case G13.CRAND: CRAND(instruction, ppu); break;
                case G13.CREQV: CREQV(instruction, ppu); break;
                case G13.CRORC: CRORC(instruction, ppu); break;
                case G13.CROR: CROR(instruction, ppu); break;

                case G13.BCCTR: BCCTR(instruction, ppu); break;
                default: throw new Exception($"Unimplemented Opcode 0x13 Field = 0x{instruction.G1F:X} -- raw = 0x{instruction.Raw:X8}");
            }
        }

        private static void Group1F(PPUInstruction instruction, PPU ppu)
        {
            switch (instruction.G1F)
            {

                // String instructions
                case G1F.LSWI:
                case G1F.LSWX:
                case G1F.STSWI:
                case G1F.STSWX:
                    Console.WriteLine($"[{ppu.ThreadName}] [PPUInterpreter] Ignored string instruction: 0x{instruction.Raw:X8}");
                    break;

                case G1F.CMP: CMP(instruction, ppu); break;
                case G1F.LVEBX: LVEBX(instruction, ppu); break;
                case G1F.LVSL: LVSL(instruction, ppu); break;
                case G1F.SUBFC: SUBFC(instruction, ppu); break;
                case G1F.MULHDU: MULHDU(instruction, ppu); break;
                case G1F.ADDC: ADDC(instruction, ppu); break;
                case G1F.ADDCO: ADDC(instruction, ppu); break;
                case G1F.MULHWU: MULHWU(instruction, ppu); break;
                case G1F.MFCR: MFCR(instruction, ppu); break;
                case G1F.LWARX: LWARX(instruction, ppu); break;
                case G1F.LDX: LDX(instruction, ppu); break;
                case G1F.LWZX: LWZX(instruction, ppu); break;
                case G1F.SLW: SLW(instruction, ppu); break;
                case G1F.CNTLZW: CNTLZW(instruction, ppu); break;
                case G1F.SLD: SLD(instruction, ppu); break;
                case G1F.AND: AND(instruction, ppu); break;
                case G1F.CMPL: CMPL(instruction, ppu); break;
                case G1F.SUBF: SUBF(instruction, ppu); break;
                case G1F.SUBFO: SUBF(instruction, ppu); break;
                case G1F.LDUX: LDUX(instruction, ppu); break;
                case G1F.DCbst: /* dcbst */ break;
                case G1F.LWZUX: LWZUX(instruction, ppu); break;
                case G1F.LVEHX: LVEHX(instruction, ppu); break;
                case G1F.CNTLZD: CNTLZD(instruction, ppu); break;
                case G1F.ANDC: ANDC(instruction, ppu); break;
                case G1F.LVEWX: LVEWX(instruction, ppu); break;
                case G1F.MULHD: MULHD(instruction, ppu); break;
                case G1F.MULHW: MULHW(instruction, ppu); break;
                case G1F.LDARX: LDARX(instruction, ppu); break;
                case G1F.LBZX: LBZX(instruction, ppu); break;
                case G1F.LVX: LVX(instruction, ppu); break;
                case G1F.NEG: NEG(instruction, ppu); break;
                case G1F.LBZUX: LBZUX(instruction, ppu); break;
                case G1F.NOR: NOR(instruction, ppu); break;
                case G1F.STVEBX: STVEBX(instruction, ppu); break;
                case G1F.SUBFE: SUBFE(instruction, ppu); break;
                case G1F.ADDE: ADDE(instruction, ppu); break;
                case G1F.MTCRF: MTCRF(instruction, ppu); break;
                case G1F.STDX: STDX(instruction, ppu); break;
                case G1F.STWCX: STWCX(instruction, ppu); break;
                case G1F.STWX: STWX(instruction, ppu); break;
                case G1F.STVEHX: STVEHX(instruction, ppu); break;
                case G1F.STDUX: STDUX(instruction, ppu); break;
                case G1F.STWUX: STWUX(instruction, ppu); break;
                case G1F.STVEWX: STVEWX(instruction, ppu); break;
                case G1F.SUBFZE: SUBFZE(instruction, ppu); break;
                case G1F.ADDZE: ADDZE(instruction, ppu); break;
                case G1F.STDCX: STDCX(instruction, ppu); break;
                case G1F.STBX: STBX(instruction, ppu); break;
                case G1F.STVX: STVX(instruction, ppu); break;
                case G1F.SUBFME: SUBFME(instruction, ppu); break;
                case G1F.MULLD: MULLD(instruction, ppu); break;
                case G1F.ADDME: ADDME(instruction, ppu); break;
                case G1F.MULLW: MULLW(instruction, ppu); break;
                case G1F.Dcbtst: /* dcbtst */ break;
                case G1F.STBUX: STBUX(instruction, ppu); break;
                case G1F.ADD: ADD(instruction, ppu); break;
                case G1F.ADDO: ADD(instruction, ppu); break;
                case G1F.Dcbt: /* dcbt */ break;
                case G1F.LHZX: LHZX(instruction, ppu); break;
                case G1F.EQV: EQV(instruction, ppu); break;
                case G1F.LHZUX: LHZUX(instruction, ppu); break;
                case G1F.XOR: XOR(instruction, ppu); break;
                case G1F.MFSPR: MFSPR(instruction, ppu); break;
                case G1F.LWAX: LWAX(instruction, ppu); break;
                case G1F.LHAX: LHAX(instruction, ppu); break;
                case G1F.MFTB: MFTB(instruction, ppu); break;
                case G1F.LWAUX: LWAUX(instruction, ppu); break;
                case G1F.LHAUX: LHAUX(instruction, ppu); break;
                case G1F.STHX: STHX(instruction, ppu); break;
                case G1F.ORC: ORC(instruction, ppu); break;
                case G1F.STHUX: STHUX(instruction, ppu); break;
                case G1F.OR: OR(instruction, ppu); break;
                case G1F.DIVDU: DIVDU(instruction, ppu); break;
                case G1F.DIVWU: DIVWU(instruction, ppu); break;
                case G1F.MTSPR: MTSPR(instruction, ppu); break;
                case G1F.NAND: NAND(instruction, ppu); break;
                case G1F.DIVD: DIVD(instruction, ppu); break;
                case G1F.DIVW: DIVW(instruction, ppu); break;
                case G1F.LVLX: LVLX(instruction, ppu); break;
                case G1F.LWBRX: LWBRX(instruction, ppu); break;
                case G1F.LFSX: LFSX(instruction, ppu); break;
                case G1F.SRW: SRW(instruction, ppu); break;
                case G1F.SRD: SRD(instruction, ppu); break;
                case G1F.LVRX: LVRX(instruction, ppu); break;
                case G1F.LFSUX: LFSUX(instruction, ppu); break;
                case G1F.Sync: /* Sync */ break;
                case G1F.LFDX: LFDX(instruction, ppu); break;
                case G1F.LFDUX: LFDUX(instruction, ppu); break;
                case G1F.STWBRX: STWBRX(instruction, ppu); break;
                case G1F.STFSX: STFSX(instruction, ppu); break;
                case G1F.STFSUX: STFSUX(instruction, ppu); break;
                case G1F.STFDX: STFDX(instruction, ppu); break;
                case G1F.STFDUX: STFDUX(instruction, ppu); break;
                case G1F.LHBRX: LHBRX(instruction, ppu); break;
                case G1F.SRAW: SRAW(instruction, ppu); break;
                case G1F.SRAD: SRAD(instruction, ppu); break;
                case G1F.SRAWI: SRAWI(instruction, ppu); break;
                case G1F.SRADI: SRADI(instruction, ppu); break;
                case G1F.SRADI_RC: SRADI(instruction, ppu); break;
                case G1F.EIEIO: /* EIEIO */ break;
                case G1F.STHBRX: STHBRX(instruction, ppu); break;
                case G1F.EXTSH: EXTSH(instruction, ppu); break;
                case G1F.EXTSB: EXTSB(instruction, ppu); break;
                case G1F.EXTSW: EXTSW(instruction, ppu); break;
                case G1F.STFIWX: STFIWX(instruction, ppu); break;
                case G1F.DCBZ: DCBZ(instruction, ppu); break;

                default:
                    // PrintTraceQueue();
                    throw new Exception($"Unimplemented Opcode 0x1F Field = 0x{instruction.G1F:X} = {instruction.G1F} -- raw = 0x{instruction.Raw:X8}");
            }
        }

        private static void UpdateCRRegister(ulong a, ulong b, PPU.CRNumber cr, PPU ppu)
        {
            //Compare a with b and set CR value based on the result
            uint result;
            if (a < b)
            {
                result = ConditionRegister.LT;
            }
            else if (a > b)
            {
                result = ConditionRegister.GT;
            }
            else
            {
                result = ConditionRegister.EQ;
            }

            //result |= (ppu.XER.SO);
            ppu.State->CR.WriteCRField(cr, result);
        }

        private static void UpdateCRRegister(long a, long b, PPU.CRNumber cr, PPU ppu)
        {
            //Compare a with b and set CR value based on the result
            uint result;
            if (a < b)
            {
                result = ConditionRegister.LT;
            }
            else if (a > b)
            {
                result = ConditionRegister.GT;
            }
            else
            {
                result = ConditionRegister.EQ;
            }

            //result |= (ppu.XER.SO);
            ppu.State->CR.WriteCRField(cr, result);
        }

        private static void UpdateCRRegister(double a, double b, PPU.CRNumber cr, PPU ppu)
        {
            //Compare a with b and set CR value based on the result
            uint result;
            if (double.IsNaN(a) || double.IsNaN(b))
            {
                result = 0b0001;
            }
            else if (a < b)
            {
                result = ConditionRegister.LT;
            }
            else if (a > b)
            {
                result = ConditionRegister.GT;
            }
            else
            {
                result = ConditionRegister.EQ;
            }

            //result |= (ppu.XER.SO);
            ppu.State->CR.WriteCRField(cr, result);
        }

        private static void Branch(PPUInstruction instruction, PPU ppu)
        {
            // Branch L form 

            bool link = (instruction.Raw & 1) == 1;
            bool absolute_address = ((instruction.Raw >> 1) & 1) == 1;

            int li = (int)((instruction.Raw >> 2) & 0xFFFFFF) << 2; //26-bits
            long li_signExtend = (long)(int)((li << 6) >> 6);       //singed 32-bits
            long currentPC = ppu.State->PC - 4;                     //Address of this opcode
            long target = li_signExtend;

            // Compute relative address if !absolute_address
            if (!absolute_address)
            {
                target += currentPC;
            }

            // Link if needed
            if (link)
            {
                ppu.State->LR = ppu.State->PC;
                ppu.CallStack.Add(ppu.State->PC);
            }

            ppu.State->PC = (uint)target;

            //Console.WriteLine($"br pc = {currentPC:X} lk = {link} - aa = {absolute_address} - li: 0x{li:X} target = 0x{target:X}");
        }

        // Helper for conditional branches
        public static bool TestBranchCondition(uint bo, uint bi, PPU ppu)
        {
            //Based on PPC manual page 34

            uint bit1 = (bo >> 1) & 1; //CTR condition: if set, compares with 0, if unset compares with != 0
            uint bit2 = (bo >> 2) & 1; //If set don't test CTR (and don't decrement)
            uint bit3 = (bo >> 3) & 1; //CR condition: if set, compares with 1, if unset compares with 0 (the opposite of bit 1), basically compare this bit with cr bi bit
            uint bit4 = (bo >> 4) & 1; //if set don't test CR

            //If both bits 2 and 4 are set, don't test anything and the result is always true
            if ((bit2 & bit4) == 1) { return true; }

            //CTR condition
            bool ctr_condition = true;
            if (bit2 == 0)
            {
                ppu.State->CTR--;
                ctr_condition = bit1 == 1 ? (ppu.State->CTR == 0) : (ppu.State->CTR != 0);
            }

            //CR Condition
            bool cr_condition = true;
            if (bit4 == 0)
            {
                uint cr_bi_bit = ppu.State->CR.ReadCRBit(bi);
                cr_condition = cr_bi_bit == bit3;
            }

            return ctr_condition && cr_condition;
        }

        public static void BC(PPUInstruction instruction, PPU ppu)
        {    //Branch Conditional
            bool lk = (instruction.Raw & 1) == 1;
            bool aa = ((instruction.Raw >> 1) & 1) == 1;
            int bd = (int)(short)(instruction.BD << 2);
            long bd_signExtend = (long)(int)((bd << 6) >> 6);       //singed 32-bits
            long currentPC = ppu.State->PC - 4;                     //Address of this opcode
            long target = bd_signExtend;
            if (!aa)
            {
                target += currentPC;
            }

            if (lk)
            {
                ppu.State->LR = ppu.State->PC;
                ppu.CallStack.Add(ppu.State->PC);
            }


            if (TestBranchCondition(instruction.Bo, instruction.Bi, ppu))
            {
                ppu.State->PC = (uint)target;
                lastBranch = true;
                //Console.WriteLine($"br pc = {currentPC:X} lk = {lk} - aa = {aa} - target = 0x{target:X}");
            }
            else
            {
                lastBranch = false;
            }
        }

        public static void BCLR(PPUInstruction instruction, PPU ppu)
        { //Branch Conditional to link register
            bool lk = (instruction.Raw & 1) == 1;
            long currentPC = ppu.State->PC - 4;  //Not needed
            ulong target = ppu.State->LR;
            ppu.CallStack.Remove((uint)target);

            if (lk)
            {
                ppu.State->LR = ppu.State->PC;
                ppu.CallStack.Add(ppu.State->PC);
            }

            if (TestBranchCondition(instruction.Bo, instruction.Bi, ppu))
            {
                ppu.State->PC = (uint)target;
                //Console.WriteLine($"BCLR pc = {currentPC:X} lk = {lk} - target = 0x{target:X}");
            }
        }

        public static void BCCTR(PPUInstruction instruction, PPU ppu)
        { //Branch Conditional to CTR
            bool lk = (instruction.Raw & 1) == 1;
            long currentPC = ppu.State->PC - 4;  //Not needed
            ulong target = ppu.State->CTR;

            if (lk)
            {
                ppu.State->LR = ppu.State->PC;
                ppu.CallStack.Add(ppu.State->PC);
            }

            if (TestBranchCondition(instruction.Bo, instruction.Bi, ppu))
            {
                ppu.State->PC = (uint)target;
                //Console.WriteLine($"BCCTR pc = {currentPC:X} lk = {lk} - target = 0x{target:X}");
            }
        }

        // Compare instructions
        public static void CMPI(PPUInstruction instruction, PPU ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);
            long si64 = instruction.SI_64;
            long a;

            if (l)
            {
                a = ppu.GPR[instruction.Ra].SValue64;
            }
            else
            {
                a = ppu.GPR[instruction.Ra].SValue32;
            }

            //Console.WriteLine($"bf = {bf} si = {si64} r{instruction.Ra} a = {a:X}");
            UpdateCRRegister(a, si64, (PPU.CRNumber)bf, ppu);
        }

        public static void CMPLI(PPUInstruction instruction, PPU ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);
            ulong ui = instruction.UI;

            ulong a;
            if (l)
            {
                a = ppu.GPR[instruction.Ra].Value64;
            }
            else
            {
                //no sign extension
                a = ppu.GPR[instruction.Ra].Value32;
            }

            //Console.WriteLine($"bf = {bf} r{instruction.Ra} a = {a:X}");
            UpdateCRRegister(a, ui, (PPU.CRNumber)bf, ppu);
        }

        public static void CMPL(PPUInstruction instruction, PPU ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);

            ulong a;
            ulong b;
            if (l)
            {
                a = ppu.GPR[instruction.Ra].Value64;
                b = ppu.GPR[instruction.Rb].Value64;
            }
            else
            {
                //no sign extension
                a = ppu.GPR[instruction.Ra].Value32;
                b = ppu.GPR[instruction.Rb].Value32;
            }

            //Console.WriteLine($"bf = {bf} r{instruction.Ra} a = {a:X}");
            UpdateCRRegister(a, b, (PPU.CRNumber)bf, ppu);
        }

        public static void CMP(PPUInstruction instruction, PPU ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);

            long a;
            long b;
            if (l)
            {
                a = ppu.GPR[instruction.Ra].SValue64;
                b = ppu.GPR[instruction.Rb].SValue64;
            }
            else
            {
                //Sign extended
                a = ppu.GPR[instruction.Ra].SValue32;
                b = ppu.GPR[instruction.Rb].SValue32;
            }

            UpdateCRRegister(a, b, (PPU.CRNumber)bf, ppu);
        }

        // Sign Extension instructions
        private static void EXTSB(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue8;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        private static void EXTSH(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue16;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        private static void EXTSW(PPUInstruction instruction, PPU ppu)
        {
            ppu.GPR[instruction.Ra].SValue64 = ppu.GPR[instruction.Rs].SValue32;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].SValue64, 0, PPU.CRNumber.CR0, ppu);
            }
        }

        private static void CNTLZD(PPUInstruction instruction, PPU ppu)
        {
            ulong value = ppu.GPR[instruction.Rs].Value64;
            uint zeroesCount = (uint)BitOperations.LeadingZeroCount(value); // Use Intrinsic
            ppu.GPR[instruction.Ra].Value64 = zeroesCount;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].Value64, 0, PPU.CRNumber.CR0, ppu); // Unsigned compare
            }
        }

        private static void CNTLZW(PPUInstruction instruction, PPU ppu)
        {
            uint value = ppu.GPR[instruction.Rs].Value32;
            uint zeroesCount = (uint)BitOperations.LeadingZeroCount(value); // Use Intrinsics
            ppu.GPR[instruction.Ra].Value64 = zeroesCount;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.GPR[instruction.Ra].Value64, 0, PPU.CRNumber.CR0, ppu);  // Unsigned compare
            }
        }

        //Execute a function from a function pointer
        public static ulong ExecuteFunction(PPU ppu, GuestVoidPtr ptr_to_pc, bool toc, ReadOnlySpan<ulong> args)
        {
            if (ptr_to_pc.IsNullPtr)
            {
                throw new Exception($"Attempt to execute from nullptr");
            }

            bool has_args = false;

            if (args.Length > 0)
            {
                if (args.Length != 5)
                {
                    //To prevent accidents
                    throw new Exception($"Invalid arg length");
                }

                has_args = true;
            }

            // Save state
            uint oldPC = ppu.State->PC;
            ppu.SavedStates.Push(*ppu.State);

            if (has_args)
            {
                // Set the arguments
                for (int i = 0; i < args.Length; i++)
                {
                    ppu.GPR[ARG0 + i].Value64 = args[i];
                }
            }

            // Set PC
            ppu.State->PC = MemoryManager.Read<uint>(ptr_to_pc);

            // Set TOC if needed
            if (toc)
            {
                ppu.GPR[2].Value64 = MemoryManager.Read<uint>(ptr_to_pc + 4);
            }

            // Set LR
            ppu.State->LR = oldPC;
            ppu.CallStack.Add(oldPC);

            // Run the function
            // Should break when the function calls blr which sets pc to the return address

            bool done = false;
            ulong r3_ret = 0;

            while (!done)
            {
                Step(ppu);
                done = ppu.State->PC == oldPC;
            }

            r3_ret = ppu.GPR[3].Value64;

            // Restore state
            ppu.CallStack.Remove(oldPC);
            *ppu.State = ppu.SavedStates.Pop();

            // Set R3 to the returned value from the function
            ppu.State->GPR[3].Value64 = r3_ret;

            // the returned r3 is mainly just for logging / debugging
            return r3_ret;
        }
    }
}
