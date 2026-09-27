using NAudio.SoundFont;
using System.Net;
using static PS3Sharp.Cell.PPU.PPU;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR
{
    public sealed partial class IRGenerator
    {
        public sealed class IRContext
        {
            // Constants 
            public Value Zero { get; set; }
            public Value One { get; set; }
            public Value MinusOne { get; set; }
            public Value CompareResult_Eq { get; set; }
            public Value CompareResult_Gt { get; set; }
            public Value CompareResult_Lt { get; set; }
            public Value SOBit { get; set; }
            public Value OVBit { get; set; }
            public Value CABit { get; set; }

            // IR being constructed
            private List<IRInstruction> Instructions { get; set; } = new();

            // Current guest state
            public uint CurrentPC { get; set; }
            public Value ExitPC { get; set; } = Value.Invalid;
            private Value[] CurrentGPR { get; } = new Value[32];
            private Value[] CurrentFPR { get; } = new Value[32];

            private Value CurrentCR { get; set; } = Value.Invalid;
            private Value CurrentCTR { get; set; } = Value.Invalid;
            private Value CurrentLR { get; set; } = Value.Invalid;
            private Value CurrentXER { get; set; } = Value.Invalid;
            private Value CurrentVRSave { get; set; } = Value.Invalid;

            // Value generation
            private int NextValueId { get; set; } = 1;

            public int InitialValueCount { get; set; }

            private IRGenerator irGenerator;    // todo: remove this dependency

            public IRContext(IRGenerator generator)
            {
                irGenerator = generator;
            }

            public void Initialize(uint address)
            {
                // Initialize IR context

                // Initialize Constants
                Zero = irGenerator.Constant(0);
                One = irGenerator.Constant(1);
                MinusOne = irGenerator.Constant(ulong.MaxValue);
                CompareResult_Eq = irGenerator.Constant(PPU.ConditionRegister.EQ);
                CompareResult_Gt = irGenerator.Constant(PPU.ConditionRegister.GT);
                CompareResult_Lt = irGenerator.Constant(PPU.ConditionRegister.LT);
                SOBit = irGenerator.Constant(PPU.XERRegister.SOBit);
                OVBit = irGenerator.Constant(PPU.XERRegister.OVBit);
                CABit = irGenerator.Constant(PPU.XERRegister.CABit);

                // Initialize special registers
                CurrentPC = address;
                ExitPC = CreateValue(Value.ValueType.I64);

                InitialValueCount = ExitPC.Id + 1;

                for (int i = 0; i < 32; i++)
                {
                    CurrentGPR[i] = Value.Invalid;
                    CurrentFPR[i] = Value.Invalid;
                }
            }

            public Value CreateValue(Value.ValueType type)
            {
                return new Value(NextValueId++, type);
            }

            public void AddInstruction(IRInstruction instruction, int index = -1)
            {
                if (index == -1)
                {
                    Instructions.Add(instruction);
                }
                else
                {
                    Instructions.Insert(index, instruction);
                }
            }

            public Value GetGPR(uint index)
            {

                if (!CurrentGPR[index].IsValid)
                {
                    CurrentGPR[index] = irGenerator.GPRLoad(index);
                }

                return CurrentGPR[index];
            }

            public Value GetFPR(uint index)
            {

                if (!CurrentFPR[index].IsValid)
                {
                    CurrentFPR[index] = irGenerator.FPRLoad(index);
                }

                return CurrentFPR[index];
            }

            public Value GetCR()
            {
                if (!CurrentCR.IsValid)
                {
                    CurrentCR = irGenerator.CRLoad();
                }

                return CurrentCR;
            }

            public void SetCR(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentCR = value;
            }

            public Value GetCTR()
            {
                if (!CurrentCTR.IsValid)
                {
                    CurrentCTR = irGenerator.CTRLoad();
                }

                return CurrentCTR;
            }

            public void SetCTR(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentCTR = value;
            }

            public Value GetLR()
            {
                if (!CurrentLR.IsValid)
                {
                    CurrentLR = irGenerator.LRLoad();
                }

                return CurrentLR;
            }

            public void SetLR(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentLR = value;
            }

            public Value GetXER()
            {
                if (!CurrentXER.IsValid)
                {
                    CurrentXER = irGenerator.XERLoad();
                }

                return CurrentXER;
            }

            public void SetXER(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentXER = value;
            }

            public Value GetVRSave()
            {
                if (!CurrentVRSave.IsValid)
                {
                    CurrentVRSave = irGenerator.VRSaveLoad();
                }

                return CurrentVRSave;
            }

            public void SetVRSave(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentVRSave = value;
            }

            public void SetGPR(uint index, Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentGPR[index] = value;
            }

            public void SetFPR(uint index, Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.F64);
                CurrentFPR[index] = value;
            }

            public void SetCA(Value flag)
            {
                SetXER(irGenerator.WriteBit(GetXER(), PPU.XERRegister.CABit, flag));
            }

            public void SetOV(Value flag)
            {
                SetXER(irGenerator.WriteBit(GetXER(), PPU.XERRegister.OVBit, flag));
            }

            public void SetSO(Value flag)
            {
                SetXER(irGenerator.WriteBit(GetXER(), PPU.XERRegister.SOBit, flag));
            }

            public Value GetCA()
            {
                return irGenerator.ReadBit(GetXER(), CABit);
            }

            public Value GetOV()
            {
                return irGenerator.ReadBit(GetXER(), OVBit);
            }

            public Value GetSO()
            {
                return irGenerator.ReadBit(GetXER(), SOBit);
            }

            public void UpdateCR(Value a, Value b, PPU.CRNumber cr, bool signed = true)
            {
                Value isLess = signed ? irGenerator.SetLessThanSigned(a, b) : irGenerator.SetLessThanUnsigned(a, b);
                Value isGreater = signed ? irGenerator.SetGreaterThanSigned(a, b) : irGenerator.SetGreaterThanUnsigned(a, b);

                Value result1 = irGenerator.Select(isLess, CompareResult_Lt, CompareResult_Eq);   // result1 = a < b ? lt : eq
                Value final = irGenerator.Select(isGreater, CompareResult_Gt, result1);           // final = a > b ? gt : result1

                // Write cr bits
                WriteCRField(cr, final);
            }

            public void WriteCRField(CRNumber cr, Value value)
            {
                // CR Fields are reversed
                int bit = 28 - ((int)cr * 4);

                Value bitNumber = irGenerator.Constant((uint)bit);
                Value mask = irGenerator.Constant(~(0xFu << bit));
                Value shiftedValue = irGenerator.ShiftLeft(value, bitNumber);

                Value crValue = GetCR();
                crValue = irGenerator.And(crValue, mask);
                crValue = irGenerator.Or(crValue, shiftedValue);

                SetCR(crValue);
            }

            public Value GetCRField(CRNumber cr)
            {
                // CR Fields are reversed
                int bit = 28 - ((int)cr * 4);

                Value bitNumber = irGenerator.Constant((uint)bit);
                Value mask = irGenerator.Constant(0xFu);
                Value shifted = irGenerator.ShiftRight(GetCR(), bitNumber);

                return irGenerator.And(shifted, mask);
            }

            public void WriteCRBit(uint bit, Value flag)
            {
                int position = (int)(31 - bit);

                // Mask flag to one bit
                flag = irGenerator.And(flag, One);

                Value cr = irGenerator.WriteBit(GetCR(), position, flag);
                SetCR(cr);
            }

            public Value GetCRBit(uint bit)
            {
                int position = (int)(31 - bit);
                return irGenerator.ReadBit(GetCR(), position);
            }

            public int GetLastUseIndex(Value value)
            {
                for (int i = Instructions.Count - 1; i >= 0; i--)
                {
                    if (Instructions[i].Opcode != IRInstruction.Op.GPRStore)
                    {
                        Value[] operands = [Instructions[i].Destination, Instructions[i].Operand1, Instructions[i].Operand2, Instructions[i].Operand3];

                        foreach (Value operand in operands)
                        {
                            if (operand.IsValid && operand == value)
                            {
                                return i;
                            }
                        }
                    }
                }

                throw new Exception($"Can't find last write for value: {value}");
            }

            public void WritebackGPRs()
            {
                // Insert writeback after last instruction that writes to the value
                for (uint i = 0; i < CurrentGPR.Length; i++)
                {
                    if (CurrentGPR[i].IsValid)
                    {
                        int lastWrite = GetLastUseIndex(CurrentGPR[i]);
                        irGenerator.GPRStore(CurrentGPR[i], i, lastWrite + 1);
                    }
                }
            }

            public void WritebackFPRs()
            {
                // Insert writeback after last instruction that writes to the value
                for (uint i = 0; i < CurrentFPR.Length; i++)
                {
                    if (CurrentFPR[i].IsValid)
                    {
                        int lastWrite = GetLastUseIndex(CurrentFPR[i]);
                        irGenerator.FPRStore(CurrentFPR[i], i, lastWrite + 1);
                    }
                }
            }

            public void WritebackSpecialRegister()
            {
                // Insert writeback after last instruction that writes to the value
                int lastWrite;

                if (CurrentCR.IsValid)
                {
                    lastWrite = GetLastUseIndex(CurrentCR);
                    irGenerator.CRStore(CurrentCR, lastWrite + 1);
                }

                if (CurrentCTR.IsValid)
                {
                    lastWrite = GetLastUseIndex(CurrentCTR);
                    irGenerator.CTRStore(CurrentCTR, lastWrite + 1);
                }

                if (CurrentLR.IsValid)
                {
                    lastWrite = GetLastUseIndex(CurrentLR);
                    irGenerator.LRStore(CurrentLR, lastWrite + 1);
                }

                if (CurrentXER.IsValid)
                {
                    lastWrite = GetLastUseIndex(CurrentXER);
                    irGenerator.XERStore(CurrentXER, lastWrite + 1);
                }

                if (CurrentVRSave.IsValid)
                {
                    lastWrite = GetLastUseIndex(CurrentVRSave);
                    irGenerator.VRSaveStore(CurrentVRSave, lastWrite + 1);
                }
            }

            public void TerminateBlock()
            {
                WritebackGPRs();
                WritebackFPRs();
                WritebackSpecialRegister();
                irGenerator.PCStore(ExitPC);
                irGenerator.Return();
            }

            public List<IRInstruction> GetIRCode()
            {
                IRInstruction[] copy = Instructions.ToArray();
                return Instructions;
            }

            public List<IRInstruction> CreateIRCopy()
            {
                IRInstruction[] copy = Instructions.ToArray();
                return new List<IRInstruction>(copy);
            }

            public void SetIRCode(List<IRInstruction> ir)
            {
                Instructions = ir;
            }
        }
    }
}
