using PS3Sharp.Memory_Manager;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public sealed partial class IRGenerator {
        private IRContext _irContext;
        private readonly IROptimizer _irOptimizer = new();

        public IRBlock GenerateIRBlock(uint address) {
            // Console.WriteLine($"Generating IR - address = 0x{address:X8}");

            // Create new context
            _irContext = new IRContext(this);   // todo: remove this dependency
            _irContext.Initialize(address);

            PPUInstruction instruction;
            while (true) {
                 instruction = new PPUInstruction {
                    Raw = MemoryManager.ReadPPUInstruction(_irContext.CurrentPC)
                };

                _irContext.CurrentPC += 4;

                if (instruction.Raw != PPUInterpreter.PPUInterpreter.NOP) {
                    GenerateIR(instruction);
                }

                if (IsBranch(instruction) || IsSyscall(instruction)) {
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

        private void GenerateIR(PPUInstruction instruction) {
            // PPC -> IR Decoder 
            switch (instruction.Op) {
                
                case Op.CMPI:
                    CMPI(instruction);
                    break;

                case Op.CMPLI:
                    CMPLI(instruction);
                    break;

                case Op.BC:
                    BC(instruction);
                    break;

                case Op.Branch:
                    Branch(instruction);
                    break;

                case Op.SC:
                    Syscall(instruction);
                    break;

                case Op.ADDI:
                    ADDI(instruction);
                    break;

                case Op.ADDIS:
                    ADDIS(instruction);
                    break;

                case Op.ADDIC:
                case Op.ADDIC_Record:
                    ADDIC(instruction);
                    break;

                case Op.SUBFIC:
                    SUBFIC(instruction);
                    break;

                case Op.ANDI:
                    ANDI(instruction);
                    break;

                case Op.ANDIS:
                    ANDIS(instruction);
                    break;

                case Op.ORI:
                    ORI(instruction);
                    break;

                case Op.ORIS:
                    ORIS(instruction);
                    break;

                case Op.XORI:
                    XORI(instruction);
                    break;

                case Op.XORIS:
                    XORIS(instruction);
                    break;

                case Op.MULLI:
                    MULLI(instruction);
                    break;

                case Op.RotateLeft:
                    IRRotateLeft(instruction);
                    break;

                case Op.G13:
                    IR_G13(instruction);
                    break;

                case Op.RLWIMI: 
                    RLWIMI(instruction);
                    break;

                case Op.RLWINM: 
                    RLWINM(instruction);
                    break;

                case Op.RLWNM:
                    RLWNM(instruction);
                    break;

                case Op.G1F:
                    IR_G1F(instruction);
                    break;

                case Op.LWZ:
                    LWZ(instruction);
                    break;

                case Op.LWZU:
                    LWZU(instruction);
                    break;

                case Op.LBZ:
                    LBZ(instruction);
                    break;

                case Op.LBZU:
                    LBZU(instruction);
                    break;

                case Op.STW:
                    STW(instruction);
                    break;

                case Op.STWU:
                    STWU(instruction);
                    break;

                case Op.STB:
                    STB(instruction);
                    break;

                case Op.STBU:
                    STBU(instruction);
                    break;

                case Op.LHZ: 
                    LHZ(instruction);
                    break;

                case Op.LHZU: 
                    LHZU(instruction);
                    break;

                case Op.LHA: 
                    LHA(instruction);
                    break;

                case Op.LHAU: 
                    LHAU(instruction);
                    break;

                case Op.STH: 
                    STH(instruction);
                    break;

                case Op.STHU: 
                    STHU(instruction);
                    break;

                case Op.LMW: 
                    LMW(instruction);
                    break;

                case Op.LD: 
                    LD(instruction); 
                    break;

                case Op.STD: 
                    STD(instruction);
                    break;

    
                // Float load/store
                case Op.LFS: 
                case Op.LFSU:  
                case Op.LFD: 
                case Op.LFDU:
                case Op.STFS:
                case Op.STFSU: 
                case Op.STFD:
                case Op.STFDU: 

                default:
                    InterpreterFallback(instruction);
                    break;
            }
        }

        private void IR_G1F(PPUInstruction instruction) {
            switch (instruction.G1F) {

                case G1F.Sync:
                    //Nop
                    break;

                case G1F.CMP:
                    CMP(instruction);
                    break;

                case G1F.CMPL:
                    CMPL(instruction);
                    break;

                case G1F.MFSPR:
                    MFSPR(instruction);
                    break;

                case G1F.MTSPR:
                    MTSPR(instruction);
                    break;

                case G1F.MFCR:
                    MFCR(instruction);
                    break;

                case G1F.MTCRF:
                    MTCRF(instruction);
                    break;

                case G1F.MFTB:
                    MFTB(instruction);
                    break;

                case G1F.ADDE:
                    ADDE(instruction);
                    break;

                case G1F.ADD:
                    ADD(instruction);
                    break;

                case G1F.ADDC:
                    ADDC(instruction);
                    break;

                case G1F.ADDZE:
                    ADDZE(instruction);
                    break;

                case G1F.ADDME:
                    ADDME(instruction);
                    break;

                case G1F.SUBF:
                    SUBF(instruction);
                    break;

                case G1F.SUBFC:
                    SUBFC(instruction);
                    break;

                case G1F.SUBFE:
                    SUBFE(instruction);
                    break;

                case G1F.SUBFZE:
                    SUBFZE(instruction);
                    break;

                case G1F.SUBFME:
                    SUBFME(instruction);
                    break;

                case G1F.AND:
                    AND(instruction);
                    break;

                case G1F.ANDC:
                    ANDC(instruction);
                    break;

                case G1F.OR:
                    OR(instruction);
                    break;

                case G1F.ORC:
                    ORC(instruction);
                    break;

                case G1F.XOR:
                    XOR(instruction);
                    break;

                case G1F.NAND:
                    NAND(instruction);
                    break;

                case G1F.NOR:
                    NOR(instruction);
                    break;

                case G1F.EQV:
                    EQV(instruction);
                    break;

                case G1F.NEG:
                    NEG(instruction);
                    break;

                case G1F.MULLW:
                    MULLW(instruction);
                    break;

                case G1F.MULHW:
                    MULHW(instruction);
                    break;

                case G1F.MULHWU:
                    MULHWU(instruction);
                    break;

                case G1F.MULLD:
                    MULLD(instruction);
                    break;

                case G1F.MULHD:
                    MULHD(instruction);
                    break;

                case G1F.MULHDU:
                    MULHDU(instruction);
                    break;

                case G1F.DIVD:
                    DIVD(instruction);
                    break;

                case G1F.DIVDU:
                    DIVDU(instruction);
                    break;

                case G1F.DIVW:
                    DIVW(instruction);
                    break;

                case G1F.DIVWU:
                    DIVWU(instruction);
                    break;

                case G1F.SLD:
                    SLD(instruction);
                    break;

                case G1F.SRD:
                    SRD(instruction);
                    break;

                case G1F.SLW:
                    SLW(instruction);
                    break;

                case G1F.SRW:
                    SRW(instruction);
                    break;

                case G1F.SRAD:
                    SRAD(instruction);
                    break;

                case G1F.SRADI:
                    SRADI(instruction);
                    break;

                case G1F.SRAW:
                    SRAW(instruction);
                    break;

                case G1F.SRAWI:
                    SRAWI(instruction);
                    break;

                case G1F.EXTSB:
                    EXTSB(instruction);
                    break;

                case G1F.EXTSH:
                    EXTSH(instruction);
                    break;

                case G1F.EXTSW:
                    EXTSW(instruction);
                    break;

                case G1F.CNTLZD:
                    CNTLZD(instruction);
                    break;

                case G1F.CNTLZW:
                    CNTLZW(instruction);
                    break;

                case G1F.STBX:
                    STBX(instruction);
                    break;

                case G1F.STBUX:
                    STBUX(instruction);
                    break;

                case G1F.STHX:
                    STHX(instruction);
                    break;

                case G1F.STHUX:
                    STHUX(instruction);
                    break;

                case G1F.STHBRX:
                    STHBRX(instruction);
                    break;

                case G1F.STWX:
                    STWX(instruction);
                    break;

                case G1F.STWUX:
                    STWUX(instruction);
                    break;

                case G1F.STWBRX:
                    STWBRX(instruction);
                    break;

                case G1F.STWCX:
                    STWCX(instruction);
                    break;

                case G1F.STDX:
                    STDX(instruction);
                    break;

                case G1F.STDUX:
                    STDUX(instruction);
                    break;

                case G1F.STDCX:
                    STDCX(instruction);
                    break;

                case G1F.LBZX:
                    LBZX(instruction);
                    break;

                case G1F.LBZUX:
                    LBZUX(instruction);
                    break;

                case G1F.LHZX:
                    LHZX(instruction);
                    break;

                case G1F.LHZUX:
                    LHZUX(instruction);
                    break;

                case G1F.LHBRX:
                    LHBRX(instruction);
                    break;

                case G1F.LHAX:
                    LHAX(instruction);
                    break;

                case G1F.LHAUX:
                    LHAUX(instruction);
                    break;

                case G1F.LWZX:
                    LWZX(instruction);
                    break;

                case G1F.LWZUX:
                    LWZUX(instruction);
                    break;

                case G1F.LWBRX:
                    LWBRX(instruction);
                    break;

                case G1F.LWAX:
                    LWAX(instruction);
                    break;

                case G1F.LWAUX:
                    LWAUX(instruction);
                    break;

                case G1F.LWARX:
                    LWARX(instruction);
                    break;

                case G1F.LDX:
                    LDX(instruction);
                    break;

                case G1F.LDUX:
                    LDUX(instruction);
                    break;

                case G1F.LDARX:
                    LDARX(instruction);
                    break;

                default:
                    InterpreterFallback(instruction);
                    break;
            }
        }

        private void IR_G13(PPUInstruction instruction) {
            switch (instruction.G13) {
                case G13.ISYNC:
                    // Nop
                    break;

                case G13.MCRF:
                    MCRF(instruction);
                    break;

                case G13.BCLR:
                    BCLR(instruction);
                    break;

                case G13.BCCTR:
                    BCCTR(instruction);
                    break;

                case G13.CROR:
                    CROR(instruction);
                    break;

                case G13.CRORC:
                    CRORC(instruction);
                    break;

                case G13.CRAND:
                    CRAND(instruction);
                    break;

                case G13.CRANDC:
                    CRANDC(instruction);
                    break;


                case G13.CRXOR:
                    CRXOR(instruction);
                    break;

                case G13.CRNAND:
                    CRNAND(instruction);
                    break;

                case G13.CRNOR:
                    CRNOR(instruction);
                    break;

                case G13.CREQV:
                    CREQV(instruction);
                    break;

                default:
                    InterpreterFallback(instruction);
                    break;
            }
        }


        private void IRRotateLeft(PPUInstruction instruction) {
            // Rotate instructions

            // bits 27 - 30 in BE
            uint field = (instruction.Raw >> 1) & 0xF;

            switch (field) {
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

        private void PrintIR(Dictionary<Value, ulong> constants = null) {
            List<IRInstruction> code = _irContext.GetIRCode();

            //if (constants != null) {
            //    Console.WriteLine($"Constants dictionary:");
            //    foreach (var keyValuePair in constants) {
            //        Value value = keyValuePair.Key;
            //        ulong constant = keyValuePair.Value;
            //        Console.WriteLine($"v{value.Id} = 0x{constant:X}");
            //    }
            //}

            for (int i = 0; i < code.Count; i++) {
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

                if (constants != null) {
                    bool destConst = constants.ContainsKey(dest);
                    bool op1Const = constants.ContainsKey(op1);
                    bool op2Const = constants.ContainsKey(op2);
                    bool op3Const = constants.ContainsKey(op3);

                    if (destConst) {
                        destination = $"__0x{constants[dest]:X}";
                    }

                    if (op1Const) {
                        operand1 = $"__0x{constants[op1]:X}";
                    }

                    if (op2Const) {
                        operand2 = $"__0x{constants[op2]:X}";
                    }

                    if (op3Const) {
                        operand3 = $"__0x{constants[op3]:X}";
                    }
                }

                if (instruction.Opcode == IRInstruction.Op.Ret) {
                    Console.WriteLine($"{instruction.Opcode}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.InterpreterFallback) {
                    Console.WriteLine($"{instruction.Opcode}: {constant1}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.Constant) {
                    string initial = instruction.Destination.Id < _irContext.InitialValueCount ? " # Initial Const" : string.Empty;
                    Console.WriteLine($"{instruction.Opcode} {destination}, {constant1} {initial}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.GPRLoad) {
                    Console.WriteLine($"{instruction.Opcode} {destination}, {constant1}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.GPRStore) {
                    Console.WriteLine($"{instruction.Opcode} {destination}, {constant1}");
                    continue;
                }

                if (instruction.Opcode == IRInstruction.Op.PCStore || 
                    instruction.Opcode == IRInstruction.Op.LRLoad || 
                    instruction.Opcode == IRInstruction.Op.LRStore) {
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

