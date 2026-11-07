namespace PS3Sharp.Cell.Spu.Interpreter
{
    internal partial class SpuInterpreter
    {
        internal const int PreferredSlot = 3;

        internal static void Step(SpuThread spu)
        {
            SpuInstruction instruction = spu.ReadInstruction();
            HandleInstruction(spu, instruction);
        }

        internal static void HandleInstruction(SpuThread spu, SpuInstruction instruction)
        {
            GroupRR17(spu, instruction);
        }

        internal static void GroupRR17(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RR17Op)
            {
                case RR17.Lnop:
                    // Lnop: No Operation (Load)
                    break;

                case RR17.Sync:
                    // Sync: Synchronize
                    break;

                case RR17.DSync:
                    // DSync: Synchronize Data
                    break;

                case RR17.Rdch:
                    Rdch(spu, instruction);
                    break;

                case RR17.Rdchcnt:
                    Rdchcnt(spu, instruction);
                    break;

                case RR17.Sf:
                    Sf(spu, instruction);
                    break;

                case RR17.Or:
                    Or(spu, instruction);
                    break;

                case RR17.Bg:
                    Bg(spu, instruction);
                    break;

                case RR17.Nor:
                    Nor(spu, instruction);
                    break;

                case RR17.Rotm:
                    Rotm(spu, instruction);
                    break;

                case RR17.Shl:
                    Shl(spu, instruction);
                    break;

                case RR17.Roti:
                    Roti(spu, instruction);
                    break;

                case RR17.Rotmi:
                    Rotmi(spu, instruction);
                    break;

                case RR17.Shli:
                    Shli(spu, instruction);
                    break;

                case RR17.A:
                    A(spu, instruction);
                    break;

                case RR17.And:
                    And(spu, instruction);
                    break;

                case RR17.Cg:
                    Cg(spu, instruction);
                    break;

                case RR17.Wrch:
                    Wrch(spu, instruction);
                    break;

                case RR17.Biz:
                    Biz(spu, instruction);
                    break;

                case RR17.Binz:
                    Binz(spu, instruction);
                    break;

                case RR17.Stqx:
                    Stqx(spu, instruction);
                    break;

                case RR17.Bi:
                    Bi(spu, instruction);
                    break;

                case RR17.Bisl:
                    Bisl(spu, instruction);
                    break;

                case RR17.Hbr:
                    // Hbr: Hint for Branch (r-form)
                    break;

                case RR17.Gb:
                    Gb(spu, instruction);
                    break;

                case RR17.Gbb:
                    Gbb(spu, instruction);
                    break;

                case RR17.Fsm:
                    Fsm(spu, instruction);
                    break;

                case RR17.Fsmb:
                    Fsmb(spu, instruction);
                    break;

                case RR17.Lqx:
                    Lqx(spu, instruction);
                    break;

                case RR17.Cbx:
                    Cbx(spu, instruction);
                    break;

                case RR17.Cwx:
                    Cwx(spu, instruction);
                    break;

                case RR17.Rotqby:
                    Rotqby(spu, instruction);
                    break;

                case RR17.Cbd:
                    Cbd(spu, instruction);
                    break;

                case RR17.Chd:
                    Chd(spu, instruction);
                    break;

                case RR17.Cwd:
                    Cwd(spu, instruction);
                    break;

                case RR17.Cdd:
                    Cdd(spu, instruction);
                    break;

                case RR17.Rotqbii:
                    Rotqbii(spu, instruction);
                    break;

                case RR17.Rotqbyi:
                    Rotqbyi(spu, instruction);
                    break;

                case RR17.Rotqmbyi:
                    Rotqmbyi(spu, instruction);
                    break;

                case RR17.Shlqbyi:
                    Shlqbyi(spu, instruction);
                    break;

                case RR17.Nop:
                    // Nop: No Operation (Execute)
                    break;

                case RR17.Clz:
                    Clz(spu, instruction);
                    break;

                case RR17.Xshw:
                    Xshw(spu, instruction);
                    break;

                case RR17.Cntb:
                    Cntb(spu, instruction);
                    break;

                case RR17.Xsbh:
                    Xsbh(spu, instruction);
                    break;

                case RR17.Andc:
                    Andc(spu, instruction);
                    break;

                case RR17.Clgth:
                    Clgth(spu, instruction);
                    break;

                case RR17.Clgtb:
                    Clgtb(spu, instruction);
                    break;

                case RR17.Addx:
                    Addx(spu, instruction);
                    break;

                case RR17.Sfx:
                    Sfx(spu, instruction);
                    break;

                case RR17.Ceq:
                    Ceq(spu, instruction);
                    break;

                case RR17.Ceqh:
                    Ceqh(spu, instruction);
                    break;

                case RR17.Ceqb:
                    Ceqb(spu, instruction);
                    break;

                default:
                    GroupRI16(spu, instruction);
                    break;
            }
        }

        internal static void GroupRI16(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RI16Op)
            {
                case RI16.Brz:
                    Brz(spu, instruction);
                    break;

                case RI16.Stqa:
                    Stqa(spu, instruction);
                    break;

                case RI16.Brnz:
                    Brnz(spu, instruction);
                    break;

                case RI16.Brhz:
                    Brhz(spu, instruction);
                    break;

                case RI16.Brhnz:
                    Brhnz(spu, instruction);
                    break;

                case RI16.Stqr:
                    Stqr(spu, instruction);
                    break;

                case RI16.Br:
                    Br(spu, instruction);
                    break;

                case RI16.Fsmbi:
                    Fsmbi(spu, instruction);
                    break;

                case RI16.Brsl:
                    Brsl(spu, instruction);
                    break;

                case RI16.Lqa:
                    Lqa(spu, instruction);
                    break;

                case RI16.Lqr:
                    Lqr(spu, instruction);
                    break;

                case RI16.Il:
                    Il(spu, instruction);
                    break;

                case RI16.Ilhu:
                    Ilhu(spu, instruction);
                    break;

                case RI16.Ilh:
                    Ilh(spu, instruction);
                    break;

                case RI16.Iohl:
                    Iohl(spu, instruction);
                    break;

                default:
                    GroupRI10(spu, instruction);
                    break;
            }
        }

        internal static void GroupRI10(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RI10Op)
            {
                case RI10.Ori:
                    Ori(spu, instruction);
                    break;

                case RI10.Orhi:
                    Orhi(spu, instruction);
                    break;

                case RI10.Sfi:
                    Sfi(spu, instruction);
                    break;

                case RI10.Andi:
                    Andi(spu, instruction);
                    break;

                case RI10.Andhi:
                    Andhi(spu, instruction);
                    break;

                case RI10.Andbi:
                    Andbi(spu, instruction);
                    break;

                case RI10.Ai:
                    Ai(spu, instruction);
                    break;

                case RI10.Stqd:
                    Stqd(spu, instruction);
                    break;

                case RI10.Lqd:
                    Lqd(spu, instruction);
                    break;

                case RI10.Xori:
                    Xori(spu, instruction);
                    break;

                case RI10.Cgti:
                    Cgti(spu, instruction);
                    break;

                case RI10.Cgtbi:
                    Cgtbi(spu, instruction);
                    break;

                case RI10.Clgti:
                    Clgti(spu, instruction);
                    break;

                case RI10.Clgtbi:
                    Clgtbi(spu, instruction);
                    break;

                case RI10.Hlgti:
                    Hlgti(spu, instruction);
                    break;

                case RI10.Ceqi:
                    Ceqi(spu, instruction);
                    break;

                case RI10.Ceqhi:
                    Ceqhi(spu, instruction);
                    break;

                case RI10.Ceqbi:
                    Ceqbi(spu, instruction);
                    break;

                case RI10.Heqi:
                    Heqi(spu, instruction);
                    break;

                default:
                    GroupRI18(spu, instruction);
                    break;
            }
        }

        internal static void GroupRI18(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RI18Op)
            {
                case RI18.Hbrr:
                    // HBRR: Hint for Branch Relative
                    break;

                case RI18.Ila: 
                    Ila(spu, instruction);
                    break;

                default:
                    GroupRRROp(spu, instruction);
                    break;
            }
        }

        internal static void GroupRRROp(SpuThread spu, SpuInstruction instruction)
        {
            switch (instruction.RRROp)
            {
                case RRR.Selb:
                    Selb(spu, instruction);
                    break;

                case RRR.ShufB:
                    ShufB(spu, instruction);
                    break;

                default:
                    unsafe
                    {
                        string op = GetInstructionOpcodes(instruction);
                        uint address = spu.State->PC - 4;
                        throw new NotImplementedException($"[SPUInterpreter][{spu.ThreadName}] Unhandled instruction at 0x{address:X} - {op}");
                    }
            }
        }

        private static string GetInstructionOpcodes(SpuInstruction instruction)
        {
            int raw = instruction.Raw;
            int rr17 = (int)instruction.RR17Op;
            int ri16 = (int)instruction.RI16Op;
            int ri10 = (int)instruction.RI10Op;
            int ri18 = (int)instruction.RI18Op;
            int rrr = (int)instruction.RRROp;

            string info = $"Instruction Raw = 0x{raw:X8}\n";
            info += $"RR17Op = 0x{rr17:X8} - ";
            info += $"RI16Op = 0x{ri16:X8} - ";
            info += $"RI10Op = 0x{ri10:X8} - ";
            info += $"RI18Op = 0x{ri18:X8} - ";
            info += $"RRR = 0x{rrr:X8}";

            return info;
        }
    }
}
