using static PS3Sharp.Cell.PPU.PPU;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions;
using ValueType = PS3Sharp.Cell.PPU.PPURecompiler.IR.IRInstructions.Value.ValueType;

namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRGenerator {
        public class IRContext {
            // Constants 
            public Value Zero { get; }
            public Value One { get; }
            public Value MinusOne { get; }
            public Value CompareResult_Eq { get; }
            public Value CompareResult_Gt { get; }
            public Value CompareResult_Lt { get; }
            public Value SOBit { get; }
            public Value OVBit { get; }
            public Value CABit { get; }

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

            public int InitialValueCount { get; }

            public IRContext(uint address) {
                // Initialize IR context

                // Initialize constants
                Zero = Constant(this, 0);
                One = Constant(this, 1);
                MinusOne = Constant(this, ulong.MaxValue);
                CompareResult_Eq = Constant(this, PPU.ConditionRegister.EQ);
                CompareResult_Gt = Constant(this, PPU.ConditionRegister.GT);
                CompareResult_Lt = Constant(this, PPU.ConditionRegister.LT);
                SOBit = Constant(this, PPU.XERRegister.SOBit);
                OVBit = Constant(this, PPU.XERRegister.OVBit);
                CABit = Constant(this, PPU.XERRegister.CABit);

                // Initialize special registers
                CurrentPC = address;
                ExitPC = CreateValue(Value.ValueType.I64);

                InitialValueCount = ExitPC.Id + 1;

                for (int i = 0; i < 32; i++) {
                    CurrentGPR[i] = Value.Invalid;
                    CurrentFPR[i] = Value.Invalid;
                }
            }

            public Value CreateValue(Value.ValueType type) {
                return new Value(NextValueId++, type);
            }

            public void AddInstruction(IRInstruction instruction, int index = -1) {
                if (index == -1) {
                    Instructions.Add(instruction);
                } else {
                    Instructions.Insert(index, instruction);
                }
            }

            public Value GetGPR(uint index) {

                if (!CurrentGPR[index].IsValid) {
                    CurrentGPR[index] = GPRLoad(this, index);
                }

                return CurrentGPR[index];
            }

            public Value GetFPR(uint index) {

                if (!CurrentFPR[index].IsValid) {
                    CurrentFPR[index] = FPRLoad(this, index);
                }

                return CurrentFPR[index];
            }

            public Value GetCR() {
                if (!CurrentCR.IsValid) {
                    CurrentCR = CRLoad(this);
                }

                return CurrentCR;
            }

            public void SetCR(Value value) {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentCR = value;
            }

            public Value GetCTR() {
                if (!CurrentCTR.IsValid) {
                    CurrentCTR = CTRLoad(this);
                }

                return CurrentCTR;
            }

            public void SetCTR(Value value) {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentCTR = value;
            }

            public Value GetLR() {
                if (!CurrentLR.IsValid) {
                    CurrentLR = LRLoad(this);
                } 

                return CurrentLR;
            }

            public void SetLR(Value value) {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentLR = value;
            }

            public Value GetXER() {
                if (!CurrentXER.IsValid) {
                    CurrentXER = XERLoad(this);    
                }

                return CurrentXER;
            }

            public void SetXER(Value value) {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentXER = value;
            }

            public Value GetVRSave() {
                if (!CurrentVRSave.IsValid) {
                    CurrentVRSave = VRSaveLoad(this);
                }

                return CurrentVRSave;
            }

            public void SetVRSave(Value value) {
                EnsureSameType(value.Type, Value.ValueType.I64);
                CurrentVRSave = value;
            }

            public void SetGPR(uint index, Value value) {
                EnsureSameType(value.Type, ValueType.I64);
                CurrentGPR[index] = value;
            }

            public void SetFPR(uint index, Value value) {
                EnsureSameType(value.Type, ValueType.F64);
                CurrentFPR[index] = value;
            }

            public void SetCA(Value flag) {
                SetXER(WriteBit(this, GetXER(), PPU.XERRegister.CABit, flag));
            }

            public void SetOV(Value flag) {
                SetXER(WriteBit(this, GetXER(), PPU.XERRegister.OVBit, flag));
            }

            public void SetSO(Value flag) {
                SetXER(WriteBit(this, GetXER(), PPU.XERRegister.SOBit, flag));
            }

            public Value GetCA() {
                return ReadBit(this, GetXER(), CABit);
            }

            public Value GetOV() {
                return ReadBit(this, GetXER(), OVBit);
            }

            public Value GetSO() {
                return ReadBit(this, GetXER(), SOBit);
            }

            public void UpdateCR(Value a, Value b, PPU.CRNumber cr , bool signed = true) {
                Value isLess = signed ? SetLessThanSigned(this, a, b) : SetLessThanUnsigned(this, a, b);
                Value isGreater = signed ? SetGreaterThanSigned(this, a, b) : SetGreaterThanUnsigned(this, a, b);

                Value result1 = Select(this, isLess, CompareResult_Lt, CompareResult_Eq);   // result1 = a < b ? lt : eq
                Value final = Select(this, isGreater, CompareResult_Gt, result1);           // final = a > b ? gt : result1

                // Write cr bits
                WriteCRField(cr, final);
            }

            public void WriteCRField(CRNumber cr, Value value) {
                // CR Fields are reversed
                int bit = 28 - ((int)cr * 4);  

                Value bitNumber = Constant(this, (uint)bit);
                Value mask = Constant(this, ~(0xFu << bit));
                Value shiftedValue = ShiftLeft(this, value, bitNumber);

                Value crValue = GetCR();
                crValue = And(this, crValue, mask);
                crValue = Or(this, crValue, shiftedValue);

                SetCR(crValue);
            }

            public Value GetCRField(CRNumber cr) {
                // CR Fields are reversed
                int bit = 28 - ((int)cr * 4);  

                Value bitNumber = Constant(this, (uint)bit);
                Value mask = Constant(this, 0xFu);
                Value shifted = ShiftRight(this, GetCR(), bitNumber);

                return And(this, shifted, mask);
            }

            public void WriteCRBit(uint bit, Value flag) {
                int position = (int)(31 - bit);

                // Mask flag to one bit
                flag = And(this, flag, One);

                Value cr = WriteBit(this, GetCR(), position, flag);
                SetCR(cr);
            }

            public Value GetCRBit(uint bit) {
                int position = (int)(31 - bit);
                return ReadBit(this, GetCR(), position);
            }

            public int GetLastUseIndex(Value value) {
                for (int i = Instructions.Count - 1; i >= 0; i--) {
                    if (Instructions[i].Op != IRInstructions.Op.GPRStore) {
                        Value[] operands = [Instructions[i].Destination, Instructions[i].Operand1, Instructions[i].Operand2, Instructions[i].Operand3];

                        foreach (Value operand in operands) {
                            if (operand.IsValid && operand == value) {
                                return i;
                            }
                        }
                    }
                }

                throw new Exception($"Can't find last write for value: {value}");
            }

            public void WritebackGPRs() {
                // Insert writeback after last instruction that writes to the value
                for (uint i = 0; i < CurrentGPR.Length; i++) {
                    if (CurrentGPR[i].IsValid) {
                        int lastWrite = GetLastUseIndex(CurrentGPR[i]);
                        GPRStore(this, CurrentGPR[i], i, lastWrite + 1);
                    }
                }
            }

            public void WritebackSpecialRegister() {
                // Insert writeback after last instruction that writes to the value
                int lastWrite;

                if (CurrentCR.IsValid) {
                    lastWrite = GetLastUseIndex(CurrentCR);
                    CRStore(this, CurrentCR, lastWrite + 1);
                }

                if (CurrentCTR.IsValid) {
                    lastWrite = GetLastUseIndex(CurrentCTR);
                    CTRStore(this, CurrentCTR, lastWrite + 1);
                }

                if (CurrentLR.IsValid) {
                    lastWrite = GetLastUseIndex(CurrentLR);
                    LRStore(this, CurrentLR, lastWrite + 1);
                }

                if (CurrentXER.IsValid) {
                    lastWrite = GetLastUseIndex(CurrentXER);
                    XERStore(this, CurrentXER, lastWrite + 1);
                }

                if (CurrentVRSave.IsValid) {
                    lastWrite = GetLastUseIndex(CurrentVRSave);
                    VRSaveStore(this, CurrentVRSave, lastWrite + 1);
                }
            }

            public void TerminateBlock() {
                WritebackGPRs();
                // TODO: FPRs

                WritebackSpecialRegister();
                PCStore(this, ExitPC);
                Return(this);
            }

            public List<IRInstruction> GetIRCode() {
                IRInstruction[] copy = Instructions.ToArray();
                return Instructions;
            }

            public List<IRInstruction> CreateIRCopy() {
                IRInstruction[] copy = Instructions.ToArray();
                return new List<IRInstruction>(copy);
            }

            public void SetIRCode(List<IRInstruction> ir) {
                Instructions = ir;
            }
        }
    }
}
