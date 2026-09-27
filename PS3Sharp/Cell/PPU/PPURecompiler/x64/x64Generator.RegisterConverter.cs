using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace PS3Sharp.Cell.PPU.PPURecompiler.x64
{
    internal sealed partial class X64Generator
    {
        private AssemblerRegister32 Reg64ToReg32(AssemblerRegister64 reg)
        {
            switch (reg.Value)
            {
                case Register.RAX: return eax;
                case Register.RCX: return ecx;
                case Register.RDX: return edx;
                case Register.RBX: return ebx;
                case Register.RSI: return esi;
                case Register.RDI: return edi;
                case Register.RBP: return ebp;
                case Register.RSP: return esp;
                case Register.R8: return r8d;
                case Register.R9: return r9d;
                case Register.R10: return r10d;
                case Register.R11: return r11d;
                case Register.R12: return r12d;
                case Register.R13: return r13d;
                case Register.R14: return r14d;
                case Register.R15: return r15d;

                default: throw new Exception($"[x64 Generator] Invalid register: {reg.Value}");
            }
        }

        private AssemblerRegister8 Reg64ToReg8(AssemblerRegister64 reg)
        {
            switch (reg.Value)
            {
                case Register.RAX: return al;
                case Register.RCX: return cl;
                case Register.RDX: return dl;
                case Register.RBX: return bl;
                case Register.RSI: return sil;
                case Register.RDI: return dil;
                case Register.RBP: return bpl;
                case Register.RSP: return spl;
                case Register.R8: return r8b;
                case Register.R9: return r9b;
                case Register.R10: return r10b;
                case Register.R11: return r11b;
                case Register.R12: return r12b;
                case Register.R13: return r13b;
                case Register.R14: return r14b;
                case Register.R15: return r15b;

                default: throw new Exception($"[x64 Generator] Invalid register: {reg.Value}");
            }
        }

        private AssemblerRegister16 Reg64ToReg16(AssemblerRegister64 reg)
        {
            switch (reg.Value)
            {
                case Register.RAX: return ax;
                case Register.RCX: return cx;
                case Register.RDX: return dx;
                case Register.RBX: return bx;
                case Register.RSI: return si;
                case Register.RDI: return di;
                case Register.RBP: return bp;
                case Register.RSP: return sp;
                case Register.R8: return r8w;
                case Register.R9: return r9w;
                case Register.R10: return r10w;
                case Register.R11: return r11w;
                case Register.R12: return r12w;
                case Register.R13: return r13w;
                case Register.R14: return r14w;
                case Register.R15: return r15w;

                default: throw new Exception($"[x64 Generator] Invalid register: {reg.Value}");
            }
        }



        private AssemblerRegister64 Reg32ToReg64(AssemblerRegister32 reg)
        {
            switch (reg.Value)
            {
                case Register.EAX: return rax;
                case Register.ECX: return rcx;
                case Register.EDX: return rdx;
                case Register.EBX: return rbx;
                case Register.ESI: return rsi;
                case Register.EDI: return rdi;
                case Register.EBP: return rbp;
                case Register.ESP: return rsp;
                case Register.R8D: return r8;
                case Register.R9D: return r9;
                case Register.R10D: return r10;
                case Register.R11D: return r11;
                case Register.R12D: return r12;
                case Register.R13D: return r13;
                case Register.R14D: return r14;
                case Register.R15D: return r15;

                default: throw new ArgumentOutOfRangeException(nameof(reg), reg, null);
            }
        }
    }
}
