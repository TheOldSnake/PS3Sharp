using PS3Sharp.Common;
using System.Diagnostics;
using System.Numerics;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.Ppu.PpuThread;

namespace PS3Sharp.Cell.Ppu.Interpreter
{
    internal static unsafe partial class PpuInterpreter
    {
        internal const uint Nop = 0x60000000;
        internal static Stopwatch stopwatch = new(); //debug
        internal static bool Log = false; //debug
        static bool lastBranch;         //debug
        static bool crash = false;
        internal static void CrashAt(PpuThread ppu, uint address, bool printRegisters = false, bool printTrace = false, bool dumpMemory = false)
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
                        uint w = ppu.MemoryManager.Read<byte>(addr);
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

        internal static void Step(PpuThread ppu)
        {
            //if (ppu.State->PC == 0x0005d918 || ppu.State->PC == 0x00876858) {
            //    ppu.State->PC = ppu.State->LR;
            //}

            //CrashAt(ppu, address: 0x0060b5b8, printRegisters: true, printTrace: false, dumpMemory: true);


            PpuInstruction instruction;
            instruction.Raw = ppu.MemoryManager.ReadPPUInstruction(ppu.State->PC);

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

            if (instruction.Raw != Nop)
            {
                Execute(instruction, ppu);
            }
        }

        internal static void PrintAllGPRs(PpuThread ppu)
        {
            for (int i = 0; i < 32; i++)
            {
                Console.WriteLine($"GPR[{i}] = 0x{ppu.Gpr[i].Value64:X16}");
            }

            Console.WriteLine($"LR = 0x{ppu.State->LR:X8}");
            Console.WriteLine($"CR = 0x{ppu.State->CR.Raw:X8}");
            Console.WriteLine($"CTR = 0x{ppu.State->Ctr:X16}");
            Console.WriteLine($"CA = 0x{ppu.State->Xer.CA:X8}");
        }

        internal static void PrintCallstack(PpuThread ppu)
        {
            int length = ppu.CallStack.Count;
            int lastIndex = length - 1;
            for (int i = 0; i < length; i++)
            {
                uint addr = ppu.CallStack[i];
                Console.Write($"0x{addr:X8}" + (i == lastIndex ? "\n" : " -> "));
            }
        }

        private static void Execute(PpuInstruction instruction, PpuThread ppu)
        {
            switch (instruction.Op)
            {
                case Op.Altivec: 
                    Altivec(instruction, ppu); 
                    break;

                case Op.Mulli: 
                    Mulli(instruction, ppu); 
                    break;

                case Op.Subfic: 
                    Subfic(instruction, ppu);
                    break;

                case Op.Cmpli: 
                    Cmpli(instruction, ppu);
                    break;

                case Op.Cmpi: 
                    Cmpi(instruction, ppu); 
                    break;

                case Op.Addic: 
                    Addic(instruction, ppu); 
                    break;

                case Op.AddicRecord:
                    AddicRecord(instruction, ppu);
                    break;

                case Op.Addi: 
                    Addi(instruction, ppu);
                    break;

                case Op.Addis: 
                    Addis(instruction, ppu);
                    break;

                case Op.Bc: 
                    Bc(instruction, ppu); 
                    break;

                case Op.Sc: 
                    Sc(instruction, ppu);
                    break;

                case Op.Branch: 
                    Branch(instruction, ppu); 
                    break;

                case Op.G13: 
                    Group13(instruction, ppu); 
                    break;

                case Op.Rlwimi: 
                    Rlwimi(instruction, ppu); 
                    break;

                case Op.Rlwinm: 
                    Rlwinm(instruction, ppu);
                    break;

                case Op.Rlwnm: 
                    Rlwnm(instruction, ppu);
                    break;

                case Op.Ori: 
                    Ori(instruction, ppu); 
                    break;

                case Op.Oris: 
                    Oris(instruction, ppu);
                    break;

                case Op.Xori: 
                    Xori(instruction, ppu);
                    break;

                case Op.Xoris:
                    Xoris(instruction, ppu);
                    break;

                case Op.Andi: 
                    Andi(instruction, ppu); 
                    break;

                case Op.Andis: 
                    Andis(instruction, ppu);
                    break;

                case Op.RotateLeft:
                    RotateLeft(instruction, ppu);
                    break;

                case Op.G1F: 
                    Group1F(instruction, ppu);
                    break;

                case Op.Lwz:
                    Lwz(instruction, ppu);
                    break;

                case Op.Lwzu:
                    Lwzu(instruction, ppu);
                    break;

                case Op.Lbz:
                    Lbz(instruction, ppu);
                    break;

                case Op.Lbzu:
                    Lbzu(instruction, ppu);
                    break;

                case Op.Stw:
                    Stw(instruction, ppu);
                    break;

                case Op.Stwu:
                    Stwu(instruction, ppu);
                    break;

                case Op.Stb:
                    Stb(instruction, ppu);
                    break;

                case Op.Stbu:
                    Stbu(instruction, ppu);
                    break;

                case Op.Lhz:
                    Lhz(instruction, ppu);
                    break;

                case Op.Lhzu:
                    Lhzu(instruction, ppu);
                    break;

                case Op.Lha:
                    Lha(instruction, ppu);
                    break;

                case Op.Lhau:
                    Lhau(instruction, ppu);
                    break;

                case Op.Sth:
                    Sth(instruction, ppu);
                    break;

                case Op.Sthu:
                    Sthu(instruction, ppu);
                    break;

                case Op.Lmw:
                    Lmw(instruction, ppu);
                    break;

                case Op.Stmw:
                    Stmw(instruction, ppu);
                    break;

                case Op.Lfs:
                    Lfs(instruction, ppu);
                    break;

                case Op.Lfsu:
                    Lfsu(instruction, ppu);
                    break;

                case Op.Lfd:
                    Lfd(instruction, ppu);
                    break;

                case Op.Lfdu:
                    Lfdu(instruction, ppu);
                    break;

                case Op.Stfs:
                    Stfs(instruction, ppu);
                    break;

                case Op.Stfsu:
                    Stfsu(instruction, ppu);
                    break;

                case Op.Stfd:
                    Stfd(instruction, ppu);
                    break;

                case Op.Stfdu:
                    Stfdu(instruction, ppu);
                    break;

                case Op.Ld:
                    Ld(instruction, ppu);
                    break;

                case Op.G3B: 
                    Group3B(instruction, ppu); 
                    break;

                case Op.G3F: 
                    Group3F(instruction, ppu); 
                    break;           

                case Op.Std: 
                    Std(instruction, ppu); 
                    break;

                default:
                    throw new Exception($"Unhandled PPU OP: 0x{instruction.Op:X} = {instruction.Op} -- raw = 0x{instruction.Raw:X8} r3 = {ppu.Gpr[3]}");
            }
        }

        private static void Altivec(PpuInstruction instruction, PpuThread ppu)
        {
            switch ((G04)((uint)instruction.G04 & 0x3F))
            {
                // Check for instructions with 6 bits opcode first
                case G04.Vsel: 
                    Vsel(instruction, ppu);
                    break;

                case G04.Vprem: 
                    Vprem(instruction, ppu); 
                    break;

                case G04.Vsldoi: 
                    Vsldoi(instruction, ppu); 
                    break;

                case G04.Vmaddfp: 
                    Vmaddfp(instruction, ppu); 
                    break;

                case G04.Vnmsubfp: 
                    Vnmsubfp(instruction, ppu); 
                    break;

                default:
                    switch (instruction.G04)
                    {
                        case G04.Vaddfp: 
                            Vaddfp(instruction, ppu); 
                            break;

                        case G04.Vadduhm:
                            Vadduhm(instruction, ppu);
                            break;

                        case G04.Vsubfp:
                            Vsubfp(instruction, ppu);
                            break;

                        case G04.Vadduwm: 
                            Vadduwm(instruction, ppu); 
                            break;

                        case G04.Vcmpequw: 
                            Vcmpequw(instruction, ppu); 
                            break;

                        case G04.Vmrghw: 
                            Vmrghw(instruction, ppu); 
                            break;

                        case G04.Vcmpeqfw:
                            Vcmpeqfw(instruction, ppu);
                            break;

                        case G04.Vrefp: 
                            Vrefp(instruction, ppu); 
                            break;

                        case G04.Vmulosh: 
                            Vmulosh(instruction, ppu);
                            break;

                        case G04.Vrsqrtefp: 
                            Vrsqrtefp(instruction, ppu); 
                            break;

                        case G04.Vslw: 
                            Vslw(instruction, ppu); 
                            break;

                        case G04.Vmrglw: 
                            Vmrglw(instruction, ppu);
                            break;

                        case G04.Vcmpgefp:
                            Vcmpgefp(instruction, ppu);
                            break;

                        case G04.Vpkswss: 
                            Vpkswss(instruction, ppu);
                            break;

                        case G04.Vupkhsh:
                            Vupkhsh(instruction, ppu);
                            break;

                        case G04.Vspltw: 
                            Vspltw(instruction, ppu); 
                            break;

                        case G04.Vcmpgtfp: 
                            Vcmpgtfp(instruction, ppu);
                            break;

                        case G04.Vupklsh:
                            Vupklsh(instruction, ppu);
                            break;

                        case G04.Vcfux: 
                            Vcfux(instruction, ppu); 
                            break;

                        case G04.Vmulesh:
                            Vmulesh(instruction, ppu);
                            break;

                        case G04.Vcfsx: 
                            Vcfsx(instruction, ppu); 
                            break;

                        case G04.Vspltish: 
                            Vspltish(instruction, ppu); 
                            break;

                        case G04.Vsraw: 
                            Vsraw(instruction, ppu); 
                            break;

                        case G04.Vctuxs: 
                            Vctuxs(instruction, ppu); 
                            break;

                        case G04.Vspltisw: 
                            Vspltisw(instruction, ppu); 
                            break;

                        case G04.Vctsxs: 
                            Vctsxs(instruction, ppu); 
                            break;

                        case G04.Vand: 
                            Vand(instruction, ppu);
                            break;

                        case G04.Vmaxfp: 
                            Vmaxfp(instruction, ppu);
                            break;

                        case G04.Vsubuhm: 
                            Vsubuhm(instruction, ppu); 
                            break;

                        case G04.Vandc: 
                            Vandc(instruction, ppu);
                            break;

                        case G04.Vminfp: 
                            Vminfp(instruction, ppu); 
                            break;

                        case G04.Vor: 
                            Vor(instruction, ppu); 
                            break;

                        case G04.Vxor: 
                            Vxor(instruction, ppu);
                            break;

                        case G04.VcmpeqfwRc:
                            Vcmpeqfw(instruction, ppu);
                            break;

                        case G04.Vnor: 
                            Vnor(instruction, ppu);
                            break;

                        case G04.VcmpgefpRc:
                            Vcmpgefp(instruction, ppu);
                            break;

                        case G04.VcmpgtfpRc:
                            Vcmpgtfp(instruction, ppu);
                            break;

                        default: 
                            throw new Exception($"Unhandled PPU Altivec OP: 0x{(uint)instruction.G04:X} = {instruction.G04} -- raw = 0x{instruction.Raw:X8}");
                    }

                    break;
            }
        }

        private static void Group13(PpuInstruction instruction, PpuThread ppu)
        {
            switch (instruction.G13)
            {
                case G13.Mcrf: 
                    Mcrf(instruction, ppu); 
                    break;

                case G13.Bclr: 
                    Bclr(instruction, ppu); 
                    break;

                case G13.Crnor: 
                    Crnor(instruction, ppu); 
                    break;

                case G13.Crandc: 
                    Crandc(instruction, ppu); 
                    break;

                case G13.Isync:
                    /* ISYNC */
                    break;

                case G13.Crxor: 
                    Crxor(instruction, ppu); 
                    break;

                case G13.Crnand: 
                    Crnand(instruction, ppu); 
                    break;

                case G13.Crand: 
                    Crand(instruction, ppu); 
                    break;

                case G13.Creqv: 
                    Creqv(instruction, ppu); 
                    break;

                case G13.Crorc: 
                    Crorc(instruction, ppu); 
                    break;

                case G13.Cror: 
                    Cror(instruction, ppu);
                    break;

                case G13.Bcctr: 
                    Bcctr(instruction, ppu); 
                    break;

                default: 
                    throw new Exception($"Unimplemented Opcode 0x13 Field = 0x{instruction.G1f:X} -- raw = 0x{instruction.Raw:X8}");
            }
        }

        private static void Group1F(PpuInstruction instruction, PpuThread ppu)
        {
            switch (instruction.G1f)
            {
                case G1f.Cmp:
                    Cmp(instruction, ppu);
                    break;

                case G1f.Lvsl:
                    Lvsl(instruction, ppu);
                    break;

                case G1f.Lvebx:
                    Lvebx(instruction, ppu);
                    break;

                case G1f.Subfc:
                    Subfc(instruction, ppu);
                    break;

                case G1f.Mulhdu:
                    Mulhdu(instruction, ppu);
                    break;

                case G1f.Addc:
                    Addc(instruction, ppu);
                    break;

                case G1f.Mulhwu:
                    Mulhwu(instruction, ppu);
                    break;

                case G1f.Mfcr:
                    Mfcr(instruction, ppu);
                    break;

                case G1f.Lwarx:
                    Lwarx(instruction, ppu);
                    break;

                case G1f.Ldx:
                    Ldx(instruction, ppu);
                    break;

                case G1f.Lwzx:
                    Lwzx(instruction, ppu);
                    break;

                case G1f.Slw:
                    Slw(instruction, ppu);
                    break;

                case G1f.Cntlzw:
                    Cntlzw(instruction, ppu);
                    break;

                case G1f.Sld:
                    Sld(instruction, ppu);
                    break;

                case G1f.And:
                    And(instruction, ppu);
                    break;

                case G1f.Cmpl:
                    Cmpl(instruction, ppu);
                    break;

                case G1f.Lvsr:
                    Lvsr(instruction, ppu);
                    break;

                case G1f.Subf:
                    Subf(instruction, ppu);
                    break;

                case G1f.Ldux:
                    Ldux(instruction, ppu);
                    break;

                case G1f.Dcbst:
                    /* Dcbst */
                    break;

                case G1f.Lwzux:
                    Lwzux(instruction, ppu);
                    break;

                case G1f.Lvehx:
                    Lvehx(instruction, ppu);
                    break;

                case G1f.Cntlzd:
                    Cntlzd(instruction, ppu);
                    break;

                case G1f.Andc:
                    Andc(instruction, ppu);
                    break;

                case G1f.Lvewx:
                    Lvewx(instruction, ppu);
                    break;

                case G1f.Mulhd:
                    Mulhd(instruction, ppu);
                    break;

                case G1f.Mulhw:
                    Mulhw(instruction, ppu);
                    break;

                case G1f.Ldarx:
                    Ldarx(instruction, ppu);
                    break;

                case G1f.Lbzx:
                    Lbzx(instruction, ppu);
                    break;

                case G1f.Lvx:
                    Lvx(instruction, ppu);
                    break;

                case G1f.Neg:
                    Neg(instruction, ppu);
                    break;

                case G1f.Lbzux:
                    Lbzux(instruction, ppu);
                    break;

                case G1f.Nor:
                    Nor(instruction, ppu);
                    break;

                case G1f.Stvebx:
                    Stvebx(instruction, ppu);
                    break;

                case G1f.Subfe:
                    Subfe(instruction, ppu);
                    break;

                case G1f.Adde:
                    Adde(instruction, ppu);
                    break;

                case G1f.Mtcrf:
                    Mtcrf(instruction, ppu);
                    break;

                case G1f.Stdx:
                    Stdx(instruction, ppu);
                    break;

                case G1f.Stwcx:
                    Stwcx(instruction, ppu);
                    break;

                case G1f.Stwx:
                    Stwx(instruction, ppu);
                    break;

                case G1f.Stvehx:
                    Stvehx(instruction, ppu);
                    break;

                case G1f.Stdux:
                    Stdux(instruction, ppu);
                    break;

                case G1f.Stwux:
                    Stwux(instruction, ppu);
                    break;

                case G1f.Stvewx:
                    Stvewx(instruction, ppu);
                    break;

                case G1f.Subfze:
                    Subfze(instruction, ppu);
                    break;

                case G1f.Addze:
                    Addze(instruction, ppu);
                    break;

                case G1f.Stdcx:
                    Stdcx(instruction, ppu);
                    break;

                case G1f.Stbx:
                    Stbx(instruction, ppu);
                    break;

                case G1f.Stvx:
                    Stvx(instruction, ppu);
                    break;

                case G1f.Subfme:
                    Subfme(instruction, ppu);
                    break;

                case G1f.Mulld:
                    Mulld(instruction, ppu);
                    break;

                case G1f.Addme:
                    Addme(instruction, ppu);
                    break;

                case G1f.Mullw:
                    Mullw(instruction, ppu);
                    break;

                case G1f.Dcbtst:
                    /* Dcbtst */
                    break;

                case G1f.Stbux:
                    Stbux(instruction, ppu);
                    break;

                case G1f.Add:
                    Add(instruction, ppu);
                    break;

                case G1f.Dcbt:
                    /* Dcbt */
                    break;

                case G1f.Lhzx:
                    Lhzx(instruction, ppu);
                    break;

                case G1f.Eqv:
                    Eqv(instruction, ppu);
                    break;

                case G1f.Lhzux:
                    Lhzux(instruction, ppu);
                    break;

                case G1f.Xor:
                    Xor(instruction, ppu);
                    break;

                case G1f.Mfspr:
                    Mfspr(instruction, ppu);
                    break;

                case G1f.Lwax:
                    Lwax(instruction, ppu);
                    break;

                case G1f.Lhax:
                    Lhax(instruction, ppu);
                    break;

                case G1f.Mftb:
                    Mftb(instruction, ppu);
                    break;

                case G1f.Lwaux:
                    Lwaux(instruction, ppu);
                    break;

                case G1f.Lhaux:
                    Lhaux(instruction, ppu);
                    break;

                case G1f.Sthx:
                    Sthx(instruction, ppu);
                    break;

                case G1f.Orc:
                    Orc(instruction, ppu);
                    break;

                case G1f.Sthux:
                    Sthux(instruction, ppu);
                    break;

                case G1f.Or:
                    Or(instruction, ppu);
                    break;

                case G1f.Divdu:
                    Divdu(instruction, ppu);
                    break;

                case G1f.Divwu:
                    Divwu(instruction, ppu);
                    break;

                case G1f.Mtspr:
                    Mtspr(instruction, ppu);
                    break;

                case G1f.Nand:
                    Nand(instruction, ppu);
                    break;

                case G1f.Divd:
                    Divd(instruction, ppu);
                    break;

                case G1f.Divw:
                    Divw(instruction, ppu);
                    break;

                case G1f.Lvlx:
                    Lvlx(instruction, ppu);
                    break;

                case G1f.Addco:
                    Addc(instruction, ppu);
                    break;

                case G1f.Lswx:
                    Console.WriteLine($"[{ppu.ThreadName}] [PPUInterpreter] Ignored string instruction: 0x{instruction.Raw:X8}");
                    break;

                case G1f.Lwbrx:
                    Lwbrx(instruction, ppu);
                    break;

                case G1f.Lfsx:
                    Lfsx(instruction, ppu);
                    break;

                case G1f.Srw:
                    Srw(instruction, ppu);
                    break;

                case G1f.Srd:
                    Srd(instruction, ppu);
                    break;

                case G1f.Lvrx:
                    Lvrx(instruction, ppu);
                    break;

                case G1f.Subfo:
                    Subf(instruction, ppu);
                    break;

                case G1f.Lfsux:
                    Lfsux(instruction, ppu);
                    break;

                case G1f.Lswi:
                    Console.WriteLine($"[{ppu.ThreadName}] [PPUInterpreter] Ignored string instruction: 0x{instruction.Raw:X8}");
                    break;

                case G1f.Sync:
                    /* Sync */
                    break;

                case G1f.Lfdx:
                    Lfdx(instruction, ppu);
                    break;

                case G1f.Lfdux:
                    Lfdux(instruction, ppu);
                    break;

                case G1f.Stswx:
                    Console.WriteLine($"[{ppu.ThreadName}] [PPUInterpreter] Ignored string instruction: 0x{instruction.Raw:X8}");
                    break;

                case G1f.Stwbrx:
                    Stwbrx(instruction, ppu);
                    break;

                case G1f.Stfsx:
                    Stfsx(instruction, ppu);
                    break;

                case G1f.Stfsux:
                    Stfsux(instruction, ppu);
                    break;

                case G1f.Stswi:
                    Console.WriteLine($"[{ppu.ThreadName}] [PPUInterpreter] Ignored string instruction: 0x{instruction.Raw:X8}");
                    break;

                case G1f.Stfdx:
                    Stfdx(instruction, ppu);
                    break;

                case G1f.Stfdux:
                    Stfdux(instruction, ppu);
                    break;

                case G1f.Addo:
                    Add(instruction, ppu);
                    break;

                case G1f.Lhbrx:
                    Lhbrx(instruction, ppu);
                    break;

                case G1f.Sraw:
                    Sraw(instruction, ppu);
                    break;

                case G1f.Srad:
                    Srad(instruction, ppu);
                    break;

                case G1f.Srawi:
                    Srawi(instruction, ppu);
                    break;

                case G1f.Sradi:
                    Sradi(instruction, ppu);
                    break;

                case G1f.SradiRc:
                    Sradi(instruction, ppu);
                    break;

                case G1f.Eieio:
                    /* Eieio */
                    break;

                case G1f.Sthbrx:
                    Sthbrx(instruction, ppu);
                    break;

                case G1f.Extsh:
                    Extsh(instruction, ppu);
                    break;

                case G1f.Extsb:
                    Extsb(instruction, ppu);
                    break;

                case G1f.Stfiwx:
                    Stfiwx(instruction, ppu);
                    break;

                case G1f.Extsw:
                    Extsw(instruction, ppu);
                    break;

                case G1f.Dcbz:
                    Dcbz(instruction, ppu);
                    break;

                default:
                    throw new Exception($"Unimplemented Opcode 0x1F Field = 0x{instruction.G1f:X} = {instruction.G1f} -- raw = 0x{instruction.Raw:X8}");
            }
        }

        private static void Group3B(PpuInstruction instruction, PpuThread ppu)
        {
            // Single float instructions

            switch (instruction.G3b)
            {
                case G3b.Fdivs: 
                    Fdivs(instruction, ppu); 
                    break;

                case G3b.Fsubs: 
                    Fsubs(instruction, ppu);
                    break;

                case G3b.Fadds: 
                    Fadds(instruction, ppu); 
                    break;

                case G3b.Fsqrts: 
                    Fsqrts(instruction, ppu); 
                    break;

                case G3b.Fres: 
                    Fres(instruction, ppu); 
                    break;

                case G3b.Fmuls:
                    Fmuls(instruction, ppu); 
                    break;

                case G3b.Frsqrtes: 
                    Frsqrtes(instruction, ppu); 
                    break;

                case G3b.Fmsubs: 
                    Fmsubs(instruction, ppu); 
                    break;

                case G3b.Fmadds: 
                    Fmadds(instruction, ppu); 
                    break;

                case G3b.Fnmsubs: 
                    Fnmsubs(instruction, ppu); 
                    break;

                case G3b.Fnmadds: 
                    Fnmadds(instruction, ppu); 
                    break;


                default: 
                    throw new Exception($"Unimplemented Opcode 0x3B Field = 0x{instruction.G3b:X} = {instruction.G3b} -- raw = 0x{instruction.Raw:X8}");
            }
        }

        private static void Group3F(PpuInstruction instruction, PpuThread ppu)
        {
            // Double float instructions

            // First check for instructions with FRC field (where opcode is 5 bits only)

            switch ((G3f)((uint)instruction.G3f & 0x1F))
            {
                case G3f.Fsel: 
                    Fsel(instruction, ppu); 
                    break;

                case G3f.Fre:
                    Fre(instruction, ppu);
                    break;

                case G3f.Fmul:
                    Fmul(instruction, ppu); 
                    break;

                case G3f.Frsqrte: 
                    Frsqrte(instruction, ppu);
                    break;

                case G3f.Fmsub: 
                    Fmsub(instruction, ppu); 
                    break;

                case G3f.Fmadd: 
                    Fmadd(instruction, ppu);
                    break;

                case G3f.Fnmsub: 
                    Fnmsub(instruction, ppu); 
                    break;

                case G3f.Fnmadd: 
                    Fnmadd(instruction, ppu); 
                    break;

                default:
                    switch (instruction.G3f)
                    {
                        case G3f.Fcmpu:
                            Fcmpu(instruction, ppu);
                            break;

                        case G3f.Frsp: 
                            Frsp(instruction, ppu); 
                            break;

                        case G3f.Fctiw: 
                            Fctiw(instruction, ppu); 
                            break;

                        case G3f.Fctiwz: 
                            Fctiwz(instruction, ppu);
                            break;

                        case G3f.Fdiv: 
                            Fdiv(instruction, ppu); 
                            break;

                        case G3f.Fsub: 
                            Fsub(instruction, ppu); 
                            break;

                        case G3f.Fadd:
                            Fadd(instruction, ppu); 
                            break;

                        case G3f.Fsqrt: 
                            Fsqrt(instruction, ppu); 
                            break;

                        case G3f.Fcmpo: 
                            Fcmpo(instruction, ppu);
                            break;

                        case G3f.Fneg: 
                            Fneg(instruction, ppu); 
                            break;

                        case G3f.Fmr:
                            Fmr(instruction, ppu);
                            break;

                        case G3f.Mtfsfi: 
                            Mtfsfi(instruction, ppu);
                            break;

                        case G3f.Fnabs: 
                            Fnabs(instruction, ppu); 
                            break;

                        case G3f.Fabs: 
                            Fabs(instruction, ppu); 
                            break;

                        case G3f.Mffs: 
                            Mffs(instruction, ppu);
                            break;

                        case G3f.Mtfsf: 
                            Mtfsf(instruction, ppu); 
                            break;

                        case G3f.Fctid: 
                            Fctid(instruction, ppu);
                            break;

                        case G3f.Fctidz: 
                            Fctidz(instruction, ppu); 
                            break;

                        case G3f.Fcfid: 
                            Fcfid(instruction, ppu); 
                            break;

                        default: 
                            throw new Exception($"Unimplemented Opcode 0x3F Field = 0x{instruction.G3f:X} = {instruction.G3f} raw = {instruction.Raw:X}");
                    }

                    break;
            }
        }

        private static void UpdateCRRegister(ulong a, ulong b, PpuThread.CrNumber cr, PpuThread ppu)
        {
            //Compare a with b and set CR value based on the result
            uint result;
            if (a < b)
            {
                result = ConditionRegister.Lt;
            }
            else if (a > b)
            {
                result = ConditionRegister.Gt;
            }
            else
            {
                result = ConditionRegister.Eq;
            }

            //result |= (ppu.XER.SO);
            ppu.State->CR.WriteCrField(cr, result);
        }

        private static void UpdateCRRegister(long a, long b, PpuThread.CrNumber cr, PpuThread ppu)
        {
            //Compare a with b and set CR value based on the result
            uint result;
            if (a < b)
            {
                result = ConditionRegister.Lt;
            }
            else if (a > b)
            {
                result = ConditionRegister.Gt;
            }
            else
            {
                result = ConditionRegister.Eq;
            }

            //result |= (ppu.XER.SO);
            ppu.State->CR.WriteCrField(cr, result);
        }

        private static void UpdateCRRegister(double a, double b, PpuThread.CrNumber cr, PpuThread ppu)
        {
            //Compare a with b and set CR value based on the result
            uint result;
            if (double.IsNaN(a) || double.IsNaN(b))
            {
                result = 0b0001;
            }
            else if (a < b)
            {
                result = ConditionRegister.Lt;
            }
            else if (a > b)
            {
                result = ConditionRegister.Gt;
            }
            else
            {
                result = ConditionRegister.Eq;
            }

            //result |= (ppu.XER.SO);
            ppu.State->CR.WriteCrField(cr, result);
        }

        private static void Branch(PpuInstruction instruction, PpuThread ppu)
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
        private static bool TestBranchCondition(uint bo, uint bi, PpuThread ppu)
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
                ppu.State->Ctr--;
                ctr_condition = bit1 == 1 ? (ppu.State->Ctr == 0) : (ppu.State->Ctr != 0);
            }

            //CR Condition
            bool cr_condition = true;
            if (bit4 == 0)
            {
                uint cr_bi_bit = ppu.State->CR.ReadCrBit(bi);
                cr_condition = cr_bi_bit == bit3;
            }

            return ctr_condition && cr_condition;
        }

        private static void Bc(PpuInstruction instruction, PpuThread ppu)
        {    //Branch Conditional
            bool lk = (instruction.Raw & 1) == 1;
            bool aa = ((instruction.Raw >> 1) & 1) == 1;
            int bd = (int)(short)(instruction.Bd << 2);
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

        private static void Bclr(PpuInstruction instruction, PpuThread ppu)
        {
            // Branch Conditional to link register
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

        private static void Bcctr(PpuInstruction instruction, PpuThread ppu)
        {
            // Branch Conditional to CTR
            bool lk = (instruction.Raw & 1) == 1;
            long currentPC = ppu.State->PC - 4;  //Not needed
            ulong target = ppu.State->Ctr;

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
        private static void Cmpi(PpuInstruction instruction, PpuThread ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);
            long si64 = instruction.Si64;
            long a;

            if (l)
            {
                a = ppu.Gpr[instruction.Ra].SValue64;
            }
            else
            {
                a = ppu.Gpr[instruction.Ra].SValue32;
            }

            //Console.WriteLine($"bf = {bf} si = {si64} r{instruction.Ra} a = {a:X}");
            UpdateCRRegister(a, si64, (PpuThread.CrNumber)bf, ppu);
        }

        private static void Cmpli(PpuInstruction instruction, PpuThread ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);
            ulong ui = instruction.Ui;

            ulong a;
            if (l)
            {
                a = ppu.Gpr[instruction.Ra].Value64;
            }
            else
            {
                //no sign extension
                a = ppu.Gpr[instruction.Ra].Value32;
            }

            //Console.WriteLine($"bf = {bf} r{instruction.Ra} a = {a:X}");
            UpdateCRRegister(a, ui, (PpuThread.CrNumber)bf, ppu);
        }

        private static void Cmpl(PpuInstruction instruction, PpuThread ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);

            ulong a;
            ulong b;
            if (l)
            {
                a = ppu.Gpr[instruction.Ra].Value64;
                b = ppu.Gpr[instruction.Rb].Value64;
            }
            else
            {
                //no sign extension
                a = ppu.Gpr[instruction.Ra].Value32;
                b = ppu.Gpr[instruction.Rb].Value32;
            }

            //Console.WriteLine($"bf = {bf} r{instruction.Ra} a = {a:X}");
            UpdateCRRegister(a, b, (PpuThread.CrNumber)bf, ppu);
        }

        private static void Cmp(PpuInstruction instruction, PpuThread ppu)
        {
            bool l = ((instruction.Raw >> 21) & 0x1) == 1;
            uint bf = ((instruction.Raw >> 23) & 0x7);

            long a;
            long b;
            if (l)
            {
                a = ppu.Gpr[instruction.Ra].SValue64;
                b = ppu.Gpr[instruction.Rb].SValue64;
            }
            else
            {
                //Sign extended
                a = ppu.Gpr[instruction.Ra].SValue32;
                b = ppu.Gpr[instruction.Rb].SValue32;
            }

            UpdateCRRegister(a, b, (PpuThread.CrNumber)bf, ppu);
        }

        // Sign Extension instructions
        private static void Extsb(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue8;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        private static void Extsh(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue16;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        private static void Extsw(PpuInstruction instruction, PpuThread ppu)
        {
            ppu.Gpr[instruction.Ra].SValue64 = ppu.Gpr[instruction.Rs].SValue32;
            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].SValue64, 0, PpuThread.CrNumber.CR0, ppu);
            }
        }

        private static void Cntlzd(PpuInstruction instruction, PpuThread ppu)
        {
            ulong value = ppu.Gpr[instruction.Rs].Value64;
            uint zeroesCount = (uint)BitOperations.LeadingZeroCount(value); // Use Intrinsic
            ppu.Gpr[instruction.Ra].Value64 = zeroesCount;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].Value64, 0, PpuThread.CrNumber.CR0, ppu); // Unsigned compare
            }
        }

        private static void Cntlzw(PpuInstruction instruction, PpuThread ppu)
        {
            uint value = ppu.Gpr[instruction.Rs].Value32;
            uint zeroesCount = (uint)BitOperations.LeadingZeroCount(value); // Use Intrinsics
            ppu.Gpr[instruction.Ra].Value64 = zeroesCount;

            if (instruction.Rc == 1)
            {
                UpdateCRRegister(ppu.Gpr[instruction.Ra].Value64, 0, PpuThread.CrNumber.CR0, ppu);  // Unsigned compare
            }
        }

        //Execute a function from a function pointer
        internal static ulong ExecuteFunction(PpuThread ppu, GuestVoidPtr ptr_to_pc, bool toc, ReadOnlySpan<ulong> args)
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
                    ppu.Gpr[Arg0 + i].Value64 = args[i];
                }
            }

            // Set PC
            ppu.State->PC = ppu.MemoryManager.Read<BigEndian<uint>>(ptr_to_pc);

            // Set TOC if needed
            if (toc)
            {
                ppu.Gpr[2].Value64 = ppu.MemoryManager.Read<BigEndian<uint>>(ptr_to_pc + 4);
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

            r3_ret = ppu.Gpr[3].Value64;

            // Restore state
            ppu.CallStack.Remove(oldPC);
            *ppu.State = ppu.SavedStates.Pop();

            // Set R3 to the returned value from the function
            ppu.State->Gpr[3].Value64 = r3_ret;

            // the returned r3 is mainly just for logging / debugging
            return r3_ret;
        }
    }
}
