using PS3Sharp.GuestMemory;

namespace PS3Sharp.Cell.Ppu.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        private IRContext _irContext;
        private readonly IROptimizer _irOptimizer = new();

        internal IRBlock GenerateIRBlock(uint address, MemoryManager memoryManager)
        {
            // Console.WriteLine($"Generating IR - address = 0x{address:X8}");

            // Create new context
            _irContext = new IRContext(this);   // todo: remove this dependency
            _irContext.Initialize(address);

            PpuInstruction instruction;
            while (true)
            {
                instruction = new PpuInstruction
                {
                    Raw = memoryManager.ReadPPUInstruction(_irContext.CurrentPC)
                };

                _irContext.CurrentPC += 4;

                if (instruction.Raw != Interpreter.PpuInterpreter.Nop)
                {
                    GenerateIR(instruction);
                }

                if (IsBranch(instruction) || IsSyscall(instruction))
                {
                    _irContext.TerminateBlock();
                    break;
                }
            }

            Dictionary<Value, ulong> constants = _irOptimizer.ConstantFold(_irContext);
            _irOptimizer.CopyPropagation(_irContext, constants);

            uint endAddress = _irContext.CurrentPC - 4;
            int isSyscallExit = IsSyscall(instruction) ? 1 : 0;

            IRBlock block = new(address, endAddress, _irContext.GetIRCode(), constants, isSyscallExit);
            return block;
        }

        private void GenerateIR(PpuInstruction instruction)
        {
            // PPC -> IR Decoder 
            switch (instruction.Op)
            {

                case Op.Cmpi:
                    CMPI(instruction);
                    break;

                case Op.Cmpli:
                    CMPLI(instruction);
                    break;

                case Op.Bc:
                    BC(instruction);
                    break;

                case Op.Branch:
                    Branch(instruction);
                    break;

                case Op.Sc:
                    Syscall(instruction);
                    break;

                case Op.Addi:
                    ADDI(instruction);
                    break;

                case Op.Addis:
                    ADDIS(instruction);
                    break;

                case Op.Addic:
                case Op.AddicRecord:
                    ADDIC(instruction);
                    break;

                case Op.Subfic:
                    SUBFIC(instruction);
                    break;

                case Op.Andi:
                    ANDI(instruction);
                    break;

                case Op.Andis:
                    ANDIS(instruction);
                    break;

                case Op.Ori:
                    ORI(instruction);
                    break;

                case Op.Oris:
                    ORIS(instruction);
                    break;

                case Op.Xori:
                    XORI(instruction);
                    break;

                case Op.Xoris:
                    XORIS(instruction);
                    break;

                case Op.Mulli:
                    MULLI(instruction);
                    break;

                case Op.RotateLeft:
                    IRRotateLeft(instruction);
                    break;

                case Op.G13:
                    IR_G13(instruction);
                    break;

                case Op.Rlwimi:
                    RLWIMI(instruction);
                    break;

                case Op.Rlwinm:
                    RLWINM(instruction);
                    break;

                case Op.Rlwnm:
                    RLWNM(instruction);
                    break;

                case Op.G1F:
                    IR_G1F(instruction);
                    break;

                case Op.Lwz:
                    LWZ(instruction);
                    break;

                case Op.Lwzu:
                    LWZU(instruction);
                    break;

                case Op.Lbz:
                    LBZ(instruction);
                    break;

                case Op.Lbzu:
                    LBZU(instruction);
                    break;

                case Op.Stw:
                    STW(instruction);
                    break;

                case Op.Stwu:
                    STWU(instruction);
                    break;

                case Op.Stb:
                    STB(instruction);
                    break;

                case Op.Stbu:
                    STBU(instruction);
                    break;

                case Op.Lhz:
                    LHZ(instruction);
                    break;

                case Op.Lhzu:
                    LHZU(instruction);
                    break;

                case Op.Lha:
                    LHA(instruction);
                    break;

                case Op.Lhau:
                    LHAU(instruction);
                    break;

                case Op.Sth:
                    STH(instruction);
                    break;

                case Op.Sthu:
                    STHU(instruction);
                    break;

                case Op.Lmw:
                    LMW(instruction);
                    break;

                case Op.Ld:
                    LD(instruction);
                    break;

                case Op.Std:
                    STD(instruction);
                    break;


                // Float load/store
                case Op.Lfs:
                case Op.Lfsu:
                case Op.Lfd:
                case Op.Lfdu:
                case Op.Stfs:
                case Op.Stfsu:
                case Op.Stfd:
                case Op.Stfdu:

                default:
                    InterpreterFallback(instruction);
                    break;
            }
        }

        private void IR_G1F(PpuInstruction instruction)
        {
            switch (instruction.G1f)
            {

                case G1f.Sync:
                    //Nop
                    break;

                case G1f.Cmp:
                    CMP(instruction);
                    break;

                case G1f.Cmpl:
                    CMPL(instruction);
                    break;

                case G1f.Mfspr:
                    MFSPR(instruction);
                    break;

                case G1f.Mtspr:
                    MTSPR(instruction);
                    break;

                case G1f.Mfcr:
                    MFCR(instruction);
                    break;

                case G1f.Mtcrf:
                    MTCRF(instruction);
                    break;

                case G1f.Mftb:
                    MFTB(instruction);
                    break;

                case G1f.Adde:
                    ADDE(instruction);
                    break;

                case G1f.Add:
                    ADD(instruction);
                    break;

                case G1f.Addc:
                    ADDC(instruction);
                    break;

                case G1f.Addze:
                    ADDZE(instruction);
                    break;

                case G1f.Addme:
                    ADDME(instruction);
                    break;

                case G1f.Subf:
                    SUBF(instruction);
                    break;

                case G1f.Subfc:
                    SUBFC(instruction);
                    break;

                case G1f.Subfe:
                    SUBFE(instruction);
                    break;

                case G1f.Subfze:
                    SUBFZE(instruction);
                    break;

                case G1f.Subfme:
                    SUBFME(instruction);
                    break;

                case G1f.And:
                    AND(instruction);
                    break;

                case G1f.Andc:
                    ANDC(instruction);
                    break;

                case G1f.Or:
                    OR(instruction);
                    break;

                case G1f.Orc:
                    ORC(instruction);
                    break;

                case G1f.Xor:
                    XOR(instruction);
                    break;

                case G1f.Nand:
                    NAND(instruction);
                    break;

                case G1f.Nor:
                    NOR(instruction);
                    break;

                case G1f.Eqv:
                    EQV(instruction);
                    break;

                case G1f.Neg:
                    NEG(instruction);
                    break;

                case G1f.Mullw:
                    MULLW(instruction);
                    break;

                case G1f.Mulhw:
                    MULHW(instruction);
                    break;

                case G1f.Mulhwu:
                    MULHWU(instruction);
                    break;

                case G1f.Mulld:
                    MULLD(instruction);
                    break;

                case G1f.Mulhd:
                    MULHD(instruction);
                    break;

                case G1f.Mulhdu:
                    MULHDU(instruction);
                    break;

                case G1f.Divd:
                    DIVD(instruction);
                    break;

                case G1f.Divdu:
                    DIVDU(instruction);
                    break;

                case G1f.Divw:
                    DIVW(instruction);
                    break;

                case G1f.Divwu:
                    DIVWU(instruction);
                    break;

                case G1f.Sld:
                    SLD(instruction);
                    break;

                case G1f.Srd:
                    SRD(instruction);
                    break;

                case G1f.Slw:
                    SLW(instruction);
                    break;

                case G1f.Srw:
                    SRW(instruction);
                    break;

                case G1f.Srad:
                    SRAD(instruction);
                    break;

                case G1f.Sradi:
                    SRADI(instruction);
                    break;

                case G1f.Sraw:
                    SRAW(instruction);
                    break;

                case G1f.Srawi:
                    SRAWI(instruction);
                    break;

                case G1f.Extsb:
                    EXTSB(instruction);
                    break;

                case G1f.Extsh:
                    EXTSH(instruction);
                    break;

                case G1f.Extsw:
                    EXTSW(instruction);
                    break;

                case G1f.Cntlzd:
                    CNTLZD(instruction);
                    break;

                case G1f.Cntlzw:
                    CNTLZW(instruction);
                    break;

                case G1f.Stbx:
                    STBX(instruction);
                    break;

                case G1f.Stbux:
                    STBUX(instruction);
                    break;

                case G1f.Sthx:
                    STHX(instruction);
                    break;

                case G1f.Sthux:
                    STHUX(instruction);
                    break;

                case G1f.Sthbrx:
                    STHBRX(instruction);
                    break;

                case G1f.Stwx:
                    STWX(instruction);
                    break;

                case G1f.Stwux:
                    STWUX(instruction);
                    break;

                case G1f.Stwbrx:
                    STWBRX(instruction);
                    break;

                case G1f.Stwcx:
                    STWCX(instruction);
                    break;

                case G1f.Stdx:
                    STDX(instruction);
                    break;

                case G1f.Stdux:
                    STDUX(instruction);
                    break;

                case G1f.Stdcx:
                    STDCX(instruction);
                    break;

                case G1f.Lbzx:
                    LBZX(instruction);
                    break;

                case G1f.Lbzux:
                    LBZUX(instruction);
                    break;

                case G1f.Lhzx:
                    LHZX(instruction);
                    break;

                case G1f.Lhzux:
                    LHZUX(instruction);
                    break;

                case G1f.Lhbrx:
                    LHBRX(instruction);
                    break;

                case G1f.Lhax:
                    LHAX(instruction);
                    break;

                case G1f.Lhaux:
                    LHAUX(instruction);
                    break;

                case G1f.Lwzx:
                    LWZX(instruction);
                    break;

                case G1f.Lwzux:
                    LWZUX(instruction);
                    break;

                case G1f.Lwbrx:
                    LWBRX(instruction);
                    break;

                case G1f.Lwax:
                    LWAX(instruction);
                    break;

                case G1f.Lwaux:
                    LWAUX(instruction);
                    break;

                case G1f.Lwarx:
                    LWARX(instruction);
                    break;

                case G1f.Ldx:
                    LDX(instruction);
                    break;

                case G1f.Ldux:
                    LDUX(instruction);
                    break;

                case G1f.Ldarx:
                    LDARX(instruction);
                    break;

                default:
                    InterpreterFallback(instruction);
                    break;
            }
        }

        private void IR_G13(PpuInstruction instruction)
        {
            switch (instruction.G13)
            {
                case G13.Isync:
                    // Nop
                    break;

                case G13.Mcrf:
                    MCRF(instruction);
                    break;

                case G13.Bclr:
                    BCLR(instruction);
                    break;

                case G13.Bcctr:
                    BCCTR(instruction);
                    break;

                case G13.Cror:
                    CROR(instruction);
                    break;

                case G13.Crorc:
                    CRORC(instruction);
                    break;

                case G13.Crand:
                    CRAND(instruction);
                    break;

                case G13.Crandc:
                    CRANDC(instruction);
                    break;


                case G13.Crxor:
                    CRXOR(instruction);
                    break;

                case G13.Crnand:
                    CRNAND(instruction);
                    break;

                case G13.Crnor:
                    CRNOR(instruction);
                    break;

                case G13.Creqv:
                    CREQV(instruction);
                    break;

                default:
                    InterpreterFallback(instruction);
                    break;
            }
        }


        private void IRRotateLeft(PpuInstruction instruction)
        {
            // Rotate instructions

            // bits 27 - 30 in BE
            uint field = (instruction.Raw >> 1) & 0xF;

            switch (field)
            {
                case 0:
                case 1:
                    RLDICL(instruction);
                    break;

                case 2:
                case 3:
                    RLDICR(instruction);
                    break;

                case 4:
                case 5:
                    RLDIC(instruction);
                    break;

                case 6:
                case 7:
                    RLDIMI(instruction);
                    break;

                case 8:
                    RLDCL(instruction);
                    break;

                case 9:
                    RLDCR(instruction);
                    break;

                default:
                    throw new Exception($"Unknown RotateLeft PPU Instruction: {instruction.Raw:X8}");
            }
        }

        private void PrintIR(Dictionary<Value, ulong> constants = null)
        {
            List<IRInstruction> code = _irContext.GetIRCode();

            //if (constants != null) {
            //    Console.WriteLine($"Constants dictionary:");
            //    foreach (var keyValuePair in constants) {
            //        Value value = keyValuePair.Key;
            //        ulong constant = keyValuePair.Value;
            //        Console.WriteLine($"v{value.Id} = 0x{constant:X}");
            //    }
            //}

            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                Value dest = instruction.Destination;
                Value op1 = instruction.Operand1;
                Value op2 = instruction.Operand2;
                Value op3 = instruction.Operand3;

                string destination = dest.IsValid ? $"v{dest.Id}" : string.Empty;
                string operand1 = op1.IsValid ? $"v{op1.Id}" : string.Empty;
                string operand2 = op2.IsValid ? $"v{op2.Id}" : string.Empty;
                string operand3 = op3.IsValid ? $"v{op3.Id}" : string.Empty;
                string constant1 = $"0x{instruction.Immediate1:X}";

                if (constants != null)
                {
                    bool destConst = constants.ContainsKey(dest);
                    bool op1Const = constants.ContainsKey(op1);
                    bool op2Const = constants.ContainsKey(op2);
                    bool op3Const = constants.ContainsKey(op3);

                    if (destConst)
                    {
                        destination = $"__0x{constants[dest]:X}";
                    }

                    if (op1Const)
                    {
                        operand1 = $"__0x{constants[op1]:X}";
                    }

                    if (op2Const)
                    {
                        operand2 = $"__0x{constants[op2]:X}";
                    }

                    if (op3Const)
                    {
                        operand3 = $"__0x{constants[op3]:X}";
                    }
                }

                if (instruction.Opcode == IRInstruction.Op.Ret)
                {
                    Console.WriteLine($"{instruction.Opcode}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.InterpreterFallback)
                {
                    Console.WriteLine($"{instruction.Opcode}: {constant1}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.Constant)
                {
                    string initial = instruction.Destination.Id < _irContext.InitialValueCount ? " # Initial Const" : string.Empty;
                    Console.WriteLine($"{instruction.Opcode} {destination}, {constant1} {initial}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.GPRLoad)
                {
                    Console.WriteLine($"{instruction.Opcode} {destination}, {constant1}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.GPRStore)
                {
                    Console.WriteLine($"{instruction.Opcode} {destination}, {constant1}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.PCStore ||
                    instruction.Opcode == IRInstruction.Op.LRLoad ||
                    instruction.Opcode == IRInstruction.Op.LRStore)
                {
                    Console.WriteLine($"{instruction.Opcode} {destination}");
                    continue;
                }


                Console.WriteLine($"{instruction.Opcode} {dest}, {operand1}, {operand2}");
            }

            Console.WriteLine($"---------------------------------------------");
            //Console.WriteLine($"LinkRegister -> v{irContext.GetLR().Id}");
            //Console.WriteLine($"ExitPC -> v{irContext.ExitPC.Id}");
            //Console.WriteLine($"Total: {code.Count}");

        }
    }
}

