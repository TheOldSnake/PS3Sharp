using static PS3Sharp.Cell.Ppu.PpuThread;

namespace PS3Sharp.Cell.Ppu.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal sealed class IRContext
        {
            private IRGenerator irGenerator;    // todo: remove this dependency

            // Value generation
            private int _nextValueId = 1;

            // IR being constructed
            private List<IRInstruction> _instructions = [];

            // Current state
            private readonly Value[] _currentGPR = new Value[32];
            private readonly Value[] _currentFPR = new Value[32];
            private Value _currentCR = Value.Invalid;
            private Value _currentCTR = Value.Invalid;
            private Value _currentLR = Value.Invalid;
            private Value _currentXER = Value.Invalid;
            private Value _currentVRSave = Value.Invalid;

            // IR Constants 
            internal Value Zero { get; set; }
            internal Value One { get; set; }
            internal Value MinusOne { get; set; }
            internal Value CompareResult_Eq { get; set; }
            internal Value CompareResult_Gt { get; set; }
            internal Value CompareResult_Lt { get; set; }
            internal Value SOBit { get; set; }
            internal Value OVBit { get; set; }
            internal Value CABit { get; set; }

            internal uint CurrentPC { get; set; }
            internal Value ExitPC { get; set; } = Value.Invalid;
            internal int InitialValueCount { get; private set; }

            internal IRContext(IRGenerator generator)
            {
                irGenerator = generator;
            }

            internal void Initialize(uint address)
            {
                // Initialize IR context

                // Initialize Constants
                Zero = irGenerator.Constant(0);
                One = irGenerator.Constant(1);
                MinusOne = irGenerator.Constant(ulong.MaxValue);
                CompareResult_Eq = irGenerator.Constant(PpuThread.ConditionRegister.Eq);
                CompareResult_Gt = irGenerator.Constant(PpuThread.ConditionRegister.Gt);
                CompareResult_Lt = irGenerator.Constant(PpuThread.ConditionRegister.Lt);
                SOBit = irGenerator.Constant(PpuThread.XerRegister.SOBit);
                OVBit = irGenerator.Constant(PpuThread.XerRegister.OVBit);
                CABit = irGenerator.Constant(PpuThread.XerRegister.CABit);

                // Initialize special registers
                CurrentPC = address;
                ExitPC = CreateValue(Value.ValueType.I64);

                InitialValueCount = ExitPC.Id + 1;

                for (int i = 0; i < 32; i++)
                {
                    _currentGPR[i] = Value.Invalid;
                    _currentFPR[i] = Value.Invalid;
                }
            }

            internal Value CreateValue(Value.ValueType type)
            {
                return new Value(_nextValueId++, type);
            }

            internal void AddInstruction(IRInstruction instruction, int index = -1)
            {
                if (index == -1)
                {
                    _instructions.Add(instruction);
                }
                else
                {
                    _instructions.Insert(index, instruction);
                }
            }

            internal Value GetGPR(uint index)
            {

                if (!_currentGPR[index].IsValid)
                {
                    _currentGPR[index] = irGenerator.GPRLoad(index);
                }

                return _currentGPR[index];
            }

            internal Value GetFPR(uint index)
            {

                if (!_currentFPR[index].IsValid)
                {
                    _currentFPR[index] = irGenerator.FPRLoad(index);
                }

                return _currentFPR[index];
            }

            internal Value GetCR()
            {
                if (!_currentCR.IsValid)
                {
                    _currentCR = irGenerator.CRLoad();
                }

                return _currentCR;
            }

            internal void SetCR(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                _currentCR = value;
            }

            internal Value GetCTR()
            {
                if (!_currentCTR.IsValid)
                {
                    _currentCTR = irGenerator.CTRLoad();
                }

                return _currentCTR;
            }

            internal void SetCTR(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                _currentCTR = value;
            }

            internal Value GetLR()
            {
                if (!_currentLR.IsValid)
                {
                    _currentLR = irGenerator.LRLoad();
                }

                return _currentLR;
            }

            internal void SetLR(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                _currentLR = value;
            }

            internal Value GetXER()
            {
                if (!_currentXER.IsValid)
                {
                    _currentXER = irGenerator.XERLoad();
                }

                return _currentXER;
            }

            internal void SetXER(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                _currentXER = value;
            }

            internal Value GetVRSave()
            {
                if (!_currentVRSave.IsValid)
                {
                    _currentVRSave = irGenerator.VRSaveLoad();
                }

                return _currentVRSave;
            }

            internal void SetVRSave(Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                _currentVRSave = value;
            }

            internal void SetGPR(uint index, Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.I64);
                _currentGPR[index] = value;
            }

            internal void SetFPR(uint index, Value value)
            {
                EnsureSameType(value.Type, Value.ValueType.F64);
                _currentFPR[index] = value;
            }

            internal void SetCA(Value flag)
            {
                SetXER(irGenerator.WriteBit(GetXER(), PpuThread.XerRegister.CABit, flag));
            }

            internal void SetOV(Value flag)
            {
                SetXER(irGenerator.WriteBit(GetXER(), PpuThread.XerRegister.OVBit, flag));
            }

            internal void SetSO(Value flag)
            {
                SetXER(irGenerator.WriteBit(GetXER(), PpuThread.XerRegister.SOBit, flag));
            }

            internal Value GetCA()
            {
                return irGenerator.ReadBit(GetXER(), CABit);
            }

            internal Value GetOV()
            {
                return irGenerator.ReadBit(GetXER(), OVBit);
            }

            internal Value GetSO()
            {
                return irGenerator.ReadBit(GetXER(), SOBit);
            }

            internal void UpdateCR(Value a, Value b, PpuThread.CrNumber cr, bool signed = true)
            {
                Value isLess = signed ? irGenerator.SetLessThanSigned(a, b) : irGenerator.SetLessThanUnsigned(a, b);
                Value isGreater = signed ? irGenerator.SetGreaterThanSigned(a, b) : irGenerator.SetGreaterThanUnsigned(a, b);

                Value result1 = irGenerator.Select(isLess, CompareResult_Lt, CompareResult_Eq);   // result1 = a < b ? lt : eq
                Value final = irGenerator.Select(isGreater, CompareResult_Gt, result1);           // final = a > b ? gt : result1

                // Write cr bits
                WriteCRField(cr, final);
            }

            internal void WriteCRField(CrNumber cr, Value value)
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

            internal Value GetCRField(CrNumber cr)
            {
                // CR Fields are reversed
                int bit = 28 - ((int)cr * 4);

                Value bitNumber = irGenerator.Constant((uint)bit);
                Value mask = irGenerator.Constant(0xFu);
                Value shifted = irGenerator.ShiftRight(GetCR(), bitNumber);

                return irGenerator.And(shifted, mask);
            }

            internal void WriteCRBit(uint bit, Value flag)
            {
                int position = (int)(31 - bit);

                // Mask flag to one bit
                flag = irGenerator.And(flag, One);

                Value cr = irGenerator.WriteBit(GetCR(), position, flag);
                SetCR(cr);
            }

            internal Value GetCRBit(uint bit)
            {
                int position = (int)(31 - bit);
                return irGenerator.ReadBit(GetCR(), position);
            }

            internal int GetLastUseIndex(Value value)
            {
                for (int i = _instructions.Count - 1; i >= 0; i--)
                {
                    if (_instructions[i].Opcode != IRInstruction.Op.GPRStore)
                    {
                        Value[] operands = [_instructions[i].Destination, _instructions[i].Operand1, _instructions[i].Operand2, _instructions[i].Operand3];

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

            internal void WritebackGPRs()
            {
                // Insert writeback after last instruction that writes to the value
                for (uint i = 0; i < _currentGPR.Length; i++)
                {
                    if (_currentGPR[i].IsValid)
                    {
                        int lastWrite = GetLastUseIndex(_currentGPR[i]);
                        irGenerator.GPRStore(_currentGPR[i], i, lastWrite + 1);
                    }
                }
            }

            internal void WritebackFPRs()
            {
                // Insert writeback after last instruction that writes to the value
                for (uint i = 0; i < _currentFPR.Length; i++)
                {
                    if (_currentFPR[i].IsValid)
                    {
                        int lastWrite = GetLastUseIndex(_currentFPR[i]);
                        irGenerator.FPRStore(_currentFPR[i], i, lastWrite + 1);
                    }
                }
            }

            internal void WritebackSpecialRegister()
            {
                // Insert writeback after last instruction that writes to the value
                int lastWrite;

                if (_currentCR.IsValid)
                {
                    lastWrite = GetLastUseIndex(_currentCR);
                    irGenerator.CRStore(_currentCR, lastWrite + 1);
                }

                if (_currentCTR.IsValid)
                {
                    lastWrite = GetLastUseIndex(_currentCTR);
                    irGenerator.CTRStore(_currentCTR, lastWrite + 1);
                }

                if (_currentLR.IsValid)
                {
                    lastWrite = GetLastUseIndex(_currentLR);
                    irGenerator.LRStore(_currentLR, lastWrite + 1);
                }

                if (_currentXER.IsValid)
                {
                    lastWrite = GetLastUseIndex(_currentXER);
                    irGenerator.XERStore(_currentXER, lastWrite + 1);
                }

                if (_currentVRSave.IsValid)
                {
                    lastWrite = GetLastUseIndex(_currentVRSave);
                    irGenerator.VRSaveStore(_currentVRSave, lastWrite + 1);
                }
            }

            internal void TerminateBlock()
            {
                WritebackGPRs();
                WritebackFPRs();
                WritebackSpecialRegister();
                irGenerator.PCStore(ExitPC);
                irGenerator.Return();
            }

            internal List<IRInstruction> GetIRCode()
            {
                IRInstruction[] copy = _instructions.ToArray();
                return _instructions;
            }

            internal List<IRInstruction> CreateIRCopy()
            {
                IRInstruction[] copy = _instructions.ToArray();
                return new List<IRInstruction>(copy);
            }

            internal void SetIRCode(List<IRInstruction> ir)
            {
                _instructions = ir;
            }
        }
    }
}