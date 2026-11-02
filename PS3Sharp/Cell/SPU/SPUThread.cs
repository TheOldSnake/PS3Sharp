using PS3Sharp.Cell.SPU.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PS3Sharp.Cell.SPU
{
    internal unsafe sealed class SPUThread : IDisposable
    {
        // Atomic stat bits for Put/Get lock-line commands
        internal const uint PutllcSucceeded = 0;
        internal const uint PutllcFailed = 1;          
        internal const uint PutllucCompleted = 1 << 1;
        internal const uint GetllarCompleted = 1 << 2;

        internal enum ThreadStatus
        {
            Stopped = 0,
            Running = 1,
        }

        // MFC channels
        internal enum MfcChannel
        {
            MfcLsa = 16,
            MfcEah = 17,
            MfcEal = 18,
            MfcSize = 19,
            MfcTagId = 20,
            MfcCmd = 21,
            MfcWrTagMask = 22,
            MfcWrTagUpdate = 23,
            MfcRdTagStat = 24,

            MfcRdAtomicStat = 27,
        }

        internal enum MfcCommand
        {
            Get = 0x40,
            Getllar = 0xD0,
        }

        // Unmanaged state
        [StructLayout(LayoutKind.Sequential)]
        internal struct SPUState
        {
            internal uint PC;
            internal SPURegisterArray GPR;
        }

        private readonly uint[] _ports = new uint[64];

        // MFC
        private uint _lsa;          // Local storage transfer address
        private uint _eah;          // Effective transfer address high
        private uint _eal;          // Effective transfer address low
        private uint _transferSize; // Transfer size
        private uint _tagId;        // Tag id
        private uint _tagMask;      // Tag mask
        private uint _atomicStat;   // Atomic Command Status Channel

        internal Thread NativeThread { get; }
        internal string ThreadName { get; } = string.Empty;
        internal uint ID { get; } = 0xFFFFFFFF;
        internal uint GroupID { get; } = 0xFFFFFFFF;
        internal SysSpu.SysSPUImage Image { get; }
        internal SysSpu.SysSPUThreadArgument Argument { get; }
        internal Lock Lock { get; } = new();

        // Allocate State
        internal static readonly int StateSize = sizeof(SPUState);
        internal NativeMemoryHandle? StateHandle { get; private set; } = new NativeMemoryHandle((nuint)StateSize);
        internal SPUState* State => (SPUState*)StateHandle!.DangerousGetHandle();

        // Allocate local storage
        internal static readonly int LocalStorageSize = 256 * 1024;
        internal NativeMemoryHandle? LsHandle { get; private set; } = new NativeMemoryHandle((nuint)LocalStorageSize);
        internal byte* LsPtr => (byte*)LsHandle!.DangerousGetHandle();

        internal ThreadStatus Status { get; private set; } = ThreadStatus.Stopped;
        internal MemoryManager MemoryManager { get; }

        internal SPUThread(string name, uint id, uint groupID, SysSpu.SysSPUImage img, SysSpu.SysSPUThreadArgument arg, MemoryManager memoryManager)
        {
            NativeThread = new Thread(RunSPU)
            {
                Name = $"Native SPU Thread: {ThreadName}"
            };

            MemoryManager = memoryManager;
            ThreadName = name;
            ID = id;
            GroupID = groupID;
            Image = img;
            Argument = arg;

            for (int i = 0; i < _ports.Length; i++)
            {
                _ports[i] = 0xFFFFFFFF;
            }
        }

        internal bool PortIsFree(int port) => _ports[port] == 0xFFFFFFFF;

        internal void SetPort(int port, uint id)
        {
            if (_ports[port] == 0xFFFFFFFF)
            {
                _ports[port] = id;
            }
            else
            {
                throw new Exception($"[SPU] Port is not free");
            }
        }

        internal uint GetPort(int port)
        {
            return _ports[port];
        }

        internal void Reset()
        {
            unsafe
            {
                NativeMemory.Clear(State, (nuint)StateSize);
                NativeMemory.Clear(LsPtr, (nuint)LocalStorageSize);
                State->GPR[1].UInts[3] = 0x3FFF0;   // Default stack pointer
            }

            Status = ThreadStatus.Stopped;
        }

        internal void Start()
        {
            Status = ThreadStatus.Running;

            LoadImage();
            SetArgs();

            unsafe
            {
                State->PC = Image.Entry;
            }

            NativeThread.Start();
        }

        internal void LoadImage()
        {
            if (Image.Entry == 0)
            {
                throw new Exception($"[SPU] Image null pointer");
            }

            BigEndianGuestPtr<SysSpu.SysSPUSegment> segmentsPtr = Image.SegmentsPtr;
            uint count = (uint)Image.SegmentsCount;

            ReadOnlySpan<SysSpu.SysSPUSegment> segments = MemoryManager.ReadArray<SysSpu.SysSPUSegment>(segmentsPtr, (int)count);

            for (int i = 0; i < segments.Length; i++)
            {
                LoadSegment(segments[i]);
            }
        }

        private void LoadSegment(SysSpu.SysSPUSegment segment)
        {
            int size = segment.Size;
            uint src = segment.Src;
            uint ls = segment.LSAddress;

            Console.WriteLine($"[SPU] Loading SPU segment at 0x{src:X8} to local address 0x{ls:X8} type = {segment.Type}");

            unsafe
            {
                // Copy from src to SPU local storage buffer
                Span<byte> dest = new(LsPtr + ls, size);

                switch (segment.Type)
                {
                    case SysSpu.SysSpuSegmentTypeCopy:
                        MemoryManager.ReadBufferFromVirtualMemory(src, dest);
                        break;

                    case SysSpu.SysSpuSegmentTypeFill:
                        dest.Fill((byte)src);
                        break;
                }
            }
        }

        internal void SetArgs()
        {
            unsafe
            {
                State->GPR[3].ULongs[1] = Argument.Arg1;
                State->GPR[4].ULongs[1] = Argument.Arg2;
                State->GPR[5].ULongs[1] = Argument.Arg3;
                State->GPR[6].ULongs[1] = Argument.Arg4;
            }
        }

        internal void RunSPU()
        {
            // TODO: Add shutdown path / cancellation source through thread manager

            try
            {
                while (true)
                {
                    SPUInterpreter.Step(this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        internal T ReadStorage<T>(uint offset) where T : unmanaged 
        {
            return Unsafe.ReadUnaligned<T>(LsPtr + offset);
        }

        internal void WriteStorage<T>(uint offset, T value) where T : unmanaged 
        {
            Unsafe.WriteUnaligned(LsPtr + offset, value);
        }

        internal SPUInstruction ReadInstruction()
        {
            unsafe
            {
                SPUInstruction instruction = new()
                {
                    Raw = ReadStorage<BigEndian<int>>(State->PC),
                };

                State->PC += 4;
                return instruction;
            }
        }

        internal uint ReadChannel(int channelNumber)
        {
            MfcChannel channel = (MfcChannel)channelNumber;
            switch (channel)
            {
                // Stub
                case MfcChannel.MfcRdTagStat:
                    return 1;

                case MfcChannel.MfcRdAtomicStat:
                    return _atomicStat;

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[SPU] Unhandled read from channel: {channelNumber} at: 0x{pc:X}");
            }
        }

        internal void WriteChannel(int channelNumber, uint value)
        {
            MfcChannel channel = (MfcChannel)channelNumber;
            switch (channel)
            {
                case MfcChannel.MfcLsa:
                    _lsa = value;
                    break;

                case MfcChannel.MfcEah:
                    _eah = value;
                    break;

                case MfcChannel.MfcEal:
                    _eal = value;
                    break;

                case MfcChannel.MfcSize:
                    _transferSize = value;
                    break;

                case MfcChannel.MfcTagId:
                    _tagId = value;
                    break;

                case MfcChannel.MfcCmd:
                    ExecuteCommand(value);
                    break;

                case MfcChannel.MfcWrTagMask:
                    _tagMask = value;
                    break;

                case MfcChannel.MfcWrTagUpdate:
                    Console.WriteLine($"[SPU] TODO WrTagUpdate");
                    break;

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[SPU] Unhandled write to channel: {channelNumber} at: 0x{pc:X}");
            }
        }

        private void ExecuteCommand(uint commandNumber)
        {
            MfcCommand command = (MfcCommand)commandNumber;
            switch (command)
            {
                case MfcCommand.Get:
                    MfcGet();
                    break;

                case MfcCommand.Getllar:
                    MfcGetllar();
                    break;

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[SPU] Unhandled MFC Command: 0x{commandNumber:X} at: 0x{pc:X}");
            }
        }

        private void MfcGet()
        {
            //  Moves data from an effective address within the main storage domain to local storage.
            if (_transferSize != 0)
            {
                if (_eal != 0)
                {
                    unsafe
                    {
                        Console.WriteLine($"[SPU] MFC Get ls[0x{_lsa:X}], memory[0x{_eal:X}], size = 0x{_transferSize}");
                        int size = (int)_transferSize;
                        Span<byte> src = MemoryManager.CreateByteSpan(_eal, size);
                        Span<byte> dst = new(LsPtr + _lsa, size);
                        src.CopyTo(dst);
                    }
                }
                else
                {
                    // Why does this happen?
                    uint pc = State->PC - 4;
                    Console.WriteLine($"[SPU] Warning: MFC Get from nullptr - lsa: 0x{_lsa:X8} size = 0x{_transferSize:X8} pc: 0x{pc:X}");
                }
            }
        }

        private void MfcGetllar()
        {
            // Gets a lock line and creates a reservation
            if (_transferSize > 0)
            {
                if (_eal != 0)
                {
                    unsafe
                    {
                        Console.WriteLine($"[SPU] MFC Getllar ls[0x{_lsa:X}], memory[0x{_eal:X}], size = 0x{_transferSize}");
                        throw new Exception("Todo");
                    }
                }
                else
                {
                    // Why does this happen?
                    uint pc = State->PC - 4;
                    Console.WriteLine($"[SPU] Warning: MFC Getllar from nullptr - lsa: 0x{_lsa:X8} size = 0x{_transferSize:X8} pc: 0x{pc:X}");
                }

                _atomicStat = GetllarCompleted;
            }
        }

        public void Dispose()
        {
            StateHandle?.Dispose();
            StateHandle = null;

            LsHandle?.Dispose();
            LsHandle = null;
        }
    }
}
