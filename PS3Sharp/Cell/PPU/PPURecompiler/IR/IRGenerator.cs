using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRGenerator {

        public static bool IsBranch(PPUInstruction instruction) {
            Op op = instruction.Op;

            if (op == Op.G13) {
                G13 g13 = instruction.G13;
                return g13 == G13.BCCTR || g13 == G13.BCLR;

            } else {
                return op == Op.Branch || op == Op.BC;
            }
        }

        public static bool IsSyscall(PPUInstruction instruction) {
            Op op = instruction.Op;
            return op == Op.SC;
        }

        public static void EnsureSameType(Value a, Value b) {
            if (a.Type != b.Type) {
                throw new InvalidOperationException($"[IR] Type check failed: {a.Type} - {b.Type}");
            }
        }

        public static void EnsureSameType(Value.ValueType typeA, Value.ValueType typeB) {
            if (typeA != typeB) {
                throw new InvalidOperationException($"[IR] Type check failed: {typeA} - {typeB}");
            }
        }

        public static Value CkdAdd(IRContext ctx, Value a, Value b, out Value carry) { // Add with carry helper
            Value result = Add(ctx, a, b);
            carry = SetLessThanUnsigned(ctx, result, a); // carry = result < a;
            return result;
        }

        public static Value WriteBit(IRContext ctx, Value destination, int bitNumber, Value flag) {
            // Clear bit                                                               
            Value mask = Constant(ctx, ~(1u << bitNumber));
            destination = And(ctx, destination, mask);

            // Set bit to flag
            Value shiftCount = Constant(ctx, (ulong)bitNumber);
            Value shiftedFlag = ShiftLeft(ctx, flag, shiftCount);
            return Or(ctx, destination, shiftedFlag);
        }

        public static Value ReadBit(IRContext ctx, Value source, int bitNumber) {
            Value shiftCount = Constant(ctx, (ulong)bitNumber);
            Value shiftedSource = ShiftRight(ctx, source, shiftCount);
            Value mask = Constant(ctx, 1);
            return And(ctx, shiftedSource, mask);
        }

        public static Value ReadBit(IRContext ctx, Value source, Value bitNumber) {
            Value shiftedSource = ShiftRight(ctx, source, bitNumber);
            return And(ctx, shiftedSource, ctx.One);
        }

        public static IRBlock GenerateIRBlock(uint address) {
            // Console.WriteLine($"Generating IR - address = 0x{address:X8}");
            IRContext irContext = new IRContext(address);

            PPUInstruction instruction;
            while (true) {
                 instruction = new PPUInstruction {
                    Raw = MemoryManager.ReadPPUInstruction(irContext.CurrentPC)
                };

                irContext.CurrentPC += 4;

                if (instruction.Raw != PPUInterpreter.PPUInterpreter.NOP) {
                    GenerateIR(irContext, instruction);
                }

                if (IsBranch(instruction) || IsSyscall(instruction)) {
                    irContext.TerminateBlock();
                    break;
                }
            }

            Dictionary<Value, ulong> constants = IROptimizer.ConstantFold(irContext);
            IROptimizer.CopyPropagation(irContext, constants);

            uint endAddress = irContext.CurrentPC - 4;
            int isSyscallExit = IsSyscall(instruction) ? 1 : 0;

            IRBlock block = new(address, endAddress, irContext.GetIRCode(), constants, isSyscallExit);
            return block;
        }

        private static void GenerateIR(IRContext irContext, PPUInstruction instruction) {
            // PPC -> IR Decoder 
            switch (instruction.Op) {
                
                case Op.CMPI:
                    CMPI(irContext, instruction);
                    break;

                case Op.CMPLI:
                    CMPLI(irContext, instruction);
                    break;

                case Op.BC:
                    BC(irContext, instruction);
                    break;

                case Op.Branch:
                    Branch(irContext, instruction);
                    break;

                case Op.SC:
                    Syscall(irContext, instruction);
                    break;

                case Op.ADDI:
                    ADDI(irContext, instruction);
                    break;

                case Op.ADDIS:
                    ADDIS(irContext, instruction);
                    break;

                case Op.ADDIC:
                case Op.ADDIC_Record:
                    ADDIC(irContext, instruction);
                    break;

                case Op.SUBFIC:
                    SUBFIC(irContext, instruction);
                    break;

                case Op.ANDI:
                    ANDI(irContext, instruction);
                    break;

                case Op.ANDIS:
                    ANDIS(irContext, instruction);
                    break;

                case Op.ORI:
                    ORI(irContext, instruction);
                    break;

                case Op.ORIS:
                    ORIS(irContext, instruction);
                    break;

                case Op.XORI:
                    XORI(irContext, instruction);
                    break;

                case Op.XORIS:
                    XORIS(irContext, instruction);
                    break;

                case Op.MULLI:
                    MULLI(irContext, instruction);
                    break;

                case Op.RotateLeft:
                    IRRotateLeft(irContext, instruction);
                    break;

                case Op.G13:
                    IR_G13(irContext, instruction);
                    break;

                case Op.RLWIMI: 
                    RLWIMI(irContext, instruction);
                    break;

                case Op.RLWINM: 
                    RLWINM(irContext, instruction);
                    break;

                case Op.RLWNM:
                    RLWNM(irContext, instruction);
                    break;

                case Op.G1F:
                    IR_G1F(irContext, instruction);
                    break;

                case Op.LWZ:
                    LWZ(irContext, instruction);
                    break;

                case Op.LWZU:
                    LWZU(irContext, instruction);
                    break;

                case Op.LBZ:
                    LBZ(irContext, instruction);
                    break;

                case Op.LBZU:
                    LBZU(irContext, instruction);
                    break;

                case Op.STW:
                    STW(irContext, instruction);
                    break;

                case Op.STWU:
                    STWU(irContext, instruction);
                    break;

                case Op.STB:
                    STB(irContext, instruction);
                    break;

                case Op.STBU:
                    STBU(irContext, instruction);
                    break;

                case Op.LHZ: 
                    LHZ(irContext, instruction);
                    break;

                case Op.LHZU: 
                    LHZU(irContext, instruction);
                    break;

                case Op.LHA: 
                    LHA(irContext, instruction);
                    break;

                case Op.LHAU: 
                    LHAU(irContext, instruction);
                    break;

                case Op.STH: 
                    STH(irContext, instruction);
                    break;

                case Op.STHU: 
                    STHU(irContext, instruction);
                    break;

                case Op.LMW: 
                    LMW(irContext, instruction);
                    break;

                case Op.LD: 
                    LD(irContext, instruction); 
                    break;

                case Op.STD: 
                    STD(irContext, instruction);
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
                    InterpreterFallback(irContext, instruction);
                    break;
            }
        }

        private static void IR_G1F(IRContext irContext, PPUInstruction instruction) {
            switch (instruction.G1F) {

                case G1F.Sync:
                    //Nop
                    break;

                case G1F.CMP:
                    CMP(irContext, instruction);
                    break;

                case G1F.CMPL:
                    CMPL(irContext, instruction);
                    break;

                case G1F.MFSPR:
                    MFSPR(irContext, instruction);
                    break;

                case G1F.MTSPR:
                    MTSPR(irContext, instruction);
                    break;

                case G1F.MFCR:
                    MFCR(irContext, instruction);
                    break;

                case G1F.MTCRF:
                    MTCRF(irContext, instruction);
                    break;

                case G1F.MFTB:
                    MFTB(irContext, instruction);
                    break;

                case G1F.ADDE:
                    ADDE(irContext, instruction);
                    break;

                case G1F.ADD:
                    ADD(irContext, instruction);
                    break;

                case G1F.ADDC:
                    ADDC(irContext, instruction);
                    break;

                case G1F.ADDZE:
                    ADDZE(irContext, instruction);
                    break;

                case G1F.ADDME:
                    ADDME(irContext, instruction);
                    break;

                case G1F.SUBF:
                    SUBF(irContext, instruction);
                    break;

                case G1F.SUBFC:
                    SUBFC(irContext, instruction);
                    break;

                case G1F.SUBFE:
                    SUBFE(irContext, instruction);
                    break;

                case G1F.SUBFZE:
                    SUBFZE(irContext, instruction);
                    break;

                case G1F.SUBFME:
                    SUBFME(irContext, instruction);
                    break;

                case G1F.AND:
                    AND(irContext, instruction);
                    break;

                case G1F.ANDC:
                    ANDC(irContext, instruction);
                    break;

                case G1F.OR:
                    OR(irContext, instruction);
                    break;

                case G1F.ORC:
                    ORC(irContext, instruction);
                    break;

                case G1F.XOR:
                    XOR(irContext, instruction);
                    break;

                case G1F.NAND:
                    NAND(irContext, instruction);
                    break;

                case G1F.NOR:
                    NOR(irContext, instruction);
                    break;

                case G1F.EQV:
                    EQV(irContext, instruction);
                    break;

                case G1F.NEG:
                    NEG(irContext, instruction);
                    break;

                case G1F.MULLW:
                    MULLW(irContext, instruction);
                    break;

                case G1F.MULHW:
                    MULHW(irContext, instruction);
                    break;

                case G1F.MULHWU:
                    MULHWU(irContext, instruction);
                    break;

                case G1F.MULLD:
                    MULLD(irContext, instruction);
                    break;

                case G1F.MULHD:
                    MULHD(irContext, instruction);
                    break;

                case G1F.MULHDU:
                    MULHDU(irContext, instruction);
                    break;

                case G1F.DIVD:
                    DIVD(irContext, instruction);
                    break;

                case G1F.DIVDU:
                    DIVDU(irContext, instruction);
                    break;

                case G1F.DIVW:
                    DIVW(irContext, instruction);
                    break;

                case G1F.DIVWU:
                    DIVWU(irContext, instruction);
                    break;

                case G1F.SLD:
                    SLD(irContext, instruction);
                    break;

                case G1F.SRD:
                    SRD(irContext, instruction);
                    break;

                case G1F.SLW:
                    SLW(irContext, instruction);
                    break;

                case G1F.SRW:
                    SRW(irContext, instruction);
                    break;

                case G1F.SRAD:
                    SRAD(irContext, instruction);
                    break;

                case G1F.SRADI:
                    SRADI(irContext, instruction);
                    break;

                case G1F.SRAW:
                    SRAW(irContext, instruction);
                    break;

                case G1F.SRAWI:
                    SRAWI(irContext, instruction);
                    break;

                case G1F.EXTSB:
                    EXTSB(irContext, instruction);
                    break;

                case G1F.EXTSH:
                    EXTSH(irContext, instruction);
                    break;

                case G1F.EXTSW:
                    EXTSW(irContext, instruction);
                    break;

                case G1F.CNTLZD:
                    CNTLZD(irContext, instruction);
                    break;

                case G1F.CNTLZW:
                    CNTLZW(irContext, instruction);
                    break;

                case G1F.STBX:
                    STBX(irContext, instruction);
                    break;

                case G1F.STBUX:
                    STBUX(irContext, instruction);
                    break;

                case G1F.STHX:
                    STHX(irContext, instruction);
                    break;

                case G1F.STHUX:
                    STHUX(irContext, instruction);
                    break;

                case G1F.STHBRX:
                    STHBRX(irContext, instruction);
                    break;

                case G1F.STWX:
                    STWX(irContext, instruction);
                    break;

                case G1F.STWUX:
                    STWUX(irContext, instruction);
                    break;

                case G1F.STWBRX:
                    STWBRX(irContext, instruction);
                    break;

                case G1F.STWCX:
                    STWCX(irContext, instruction);
                    break;

                case G1F.STDX:
                    STDX(irContext, instruction);
                    break;

                case G1F.STDUX:
                    STDUX(irContext, instruction);
                    break;

                case G1F.STDCX:
                    STDCX(irContext, instruction);
                    break;

                case G1F.LBZX:
                    LBZX(irContext, instruction);
                    break;

                case G1F.LBZUX:
                    LBZUX(irContext, instruction);
                    break;

                case G1F.LHZX:
                    LHZX(irContext, instruction);
                    break;

                case G1F.LHZUX:
                    LHZUX(irContext, instruction);
                    break;

                case G1F.LHBRX:
                    LHBRX(irContext, instruction);
                    break;

                case G1F.LHAX:
                    LHAX(irContext, instruction);
                    break;

                case G1F.LHAUX:
                    LHAUX(irContext, instruction);
                    break;

                case G1F.LWZX:
                    LWZX(irContext, instruction);
                    break;

                case G1F.LWZUX:
                    LWZUX(irContext, instruction);
                    break;

                case G1F.LWBRX:
                    LWBRX(irContext, instruction);
                    break;

                case G1F.LWAX:
                    LWAX(irContext, instruction);
                    break;

                case G1F.LWAUX:
                    LWAUX(irContext, instruction);
                    break;

                case G1F.LWARX:
                    LWARX(irContext, instruction);
                    break;

                case G1F.LDX:
                    LDX(irContext, instruction);
                    break;

                case G1F.LDUX:
                    LDUX(irContext, instruction);
                    break;

                case G1F.LDARX:
                    LDARX(irContext, instruction);
                    break;

                default:
                    InterpreterFallback(irContext, instruction);
                    break;
            }
        }

        private static void IR_G13(IRContext irContext, PPUInstruction instruction) {
            switch (instruction.G13) {
                case G13.ISYNC:
                    // Nop
                    break;

                case G13.MCRF:
                    MCRF(irContext, instruction);
                    break;

                case G13.BCLR:
                    BCLR(irContext, instruction);
                    break;

                case G13.BCCTR:
                    BCCTR(irContext, instruction);
                    break;

                case G13.CROR:
                    CROR(irContext, instruction);
                    break;

                case G13.CRORC:
                    CRORC(irContext, instruction);
                    break;

                case G13.CRAND:
                    CRAND(irContext, instruction);
                    break;

                case G13.CRANDC:
                    CRANDC(irContext, instruction);
                    break;


                case G13.CRXOR:
                    CRXOR(irContext, instruction);
                    break;

                case G13.CRNAND:
                    CRNAND(irContext, instruction);
                    break;

                case G13.CRNOR:
                    CRNOR(irContext, instruction);
                    break;

                case G13.CREQV:
                    CREQV(irContext, instruction);
                    break;

                default:
                    InterpreterFallback(irContext, instruction);
                    break;
            }
        }


        private static void IRRotateLeft(IRContext irContext, PPUInstruction instruction) {
            // Rotate instructions

            // bits 27 - 30 in BE
            uint field = (instruction.Raw >> 1) & 0xF;

            switch (field) {
                case 0:
                case 1:
                    RLDICL(irContext, instruction);
                    break;

                case 2:
                case 3:
                    RLDICR(irContext, instruction);
                    break;

                case 4:
                case 5:
                    RLDIC(irContext, instruction);
                    break;

                case 6:
                case 7:
                    RLDIMI(irContext, instruction);
                    break;

                case 8:
                    RLDCL(irContext, instruction);
                    break;

                case 9:
                    RLDCR(irContext, instruction);
                    break;

                default:
                    throw new Exception($"Unknown RotateLeft PPU Instruction: {instruction.Raw:X8}");
            }
        }

        private static void PrintIR(IRContext irContext, Dictionary<Value, ulong> constants = null) {
            List<IRInstruction> code = irContext.GetIRCode();

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

                if (instruction.Op == IRInstructions.Op.Ret) {
                    Console.WriteLine($"{instruction.Op}");
                    continue;
                }

                if (instruction.Op == IRInstructions.Op.InterpreterFallback) {
                    Console.WriteLine($"{instruction.Op}: {constant1}");
                    continue;
                }

                if (instruction.Op == IRInstructions.Op.Constant) {
                    string initial = instruction.Destination.Id < irContext.InitialValueCount ? " # Initial Const" : string.Empty;
                    Console.WriteLine($"{instruction.Op} {destination}, {constant1} {initial}");
                    continue;
                }

                if (instruction.Op == IRInstructions.Op.GPRLoad) {
                    Console.WriteLine($"{instruction.Op} {destination}, {constant1}");
                    continue;
                }

                if (instruction.Op == IRInstructions.Op.GPRStore) {
                    Console.WriteLine($"{instruction.Op} {destination}, {constant1}");
                    continue;
                }

                if (instruction.Op == IRInstructions.Op.PCStore || 
                    instruction.Op == IRInstructions.Op.LRLoad || 
                    instruction.Op == IRInstructions.Op.LRStore) {
                    Console.WriteLine($"{instruction.Op} {destination}");
                    continue;
                }


                Console.WriteLine($"{instruction.Op} {dest}, {operand1}, {operand2}");
            }

            Console.WriteLine($"---------------------------------------------");
            //Console.WriteLine($"LinkRegister -> v{irContext.GetLR().Id}");
            //Console.WriteLine($"ExitPC -> v{irContext.ExitPC.Id}");
            //Console.WriteLine($"Total: {code.Count}");

        }
    }
}

