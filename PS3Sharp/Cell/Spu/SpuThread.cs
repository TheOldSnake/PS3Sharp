using PS3Sharp.Cell.Spu.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using PS3Sharp.OS.Syscall;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.Cell.Spu
{
    internal unsafe sealed class SpuThread : IDisposable
    {
        // Unmanaged state
        [StructLayout(LayoutKind.Sequential)]
        internal struct SpuState
        {
            internal uint PC;
            internal SpuRegisterArray GPR;
        }

        internal enum ThreadStatus
        {
            Stopped = 0,
            Running = 1,
        }

        // MFC channels map
        internal enum MfcChannel
        {
            RdEventStat = 0x0,      // Read Event Stat
            WrEventMask = 0x1,      // Write Event Mask
            WrEventAck  = 0x2,      // Write Event Acknowledgment
            RdEventMask  = 0xB,     // Read Event Mask
            RdTagMask = 0xC,        // Read Tag-Group Query Mask
            Lsa = 0x10,             // Local Storage Address
            Eah = 0x11,             // Effective Address High
            Eal = 0x12,             // Effective Address Low or List Address
            Size = 0x13,            // Transfer Size
            TagId = 0x14,           // Tag Identification
            Cmd = 0x15,             // Opcode
            WrTagMask = 0x16,       // Write Tag-Group Query Mask
            WrTagUpdate = 0x17,     // Write Tag Status Update Request
            RdTagStat = 0x18,       // Read Tag-Group Status
            RdAtomicStat = 0x1B,    // Read Atomic Command Status
            WrOutMbox = 0x1C,       // Write Outbound Mailbox
            RdInMbox = 0x1D,        // Read Inbound Mailbox
            WrOutIntrMbox = 0x1E,   // Write Outbound Interrupt Mailbox
        }

        internal enum MfcCommand
        {
            Put = 0x20,          
            Get = 0x40,         
            Getb = 0x41,         
            Putllc = 0xB4,       
            Getllar = 0xD0,      
        }

        // Atomic stat bits for Put/Get lock-line commands
        internal enum AtomicStat : uint
        {
            PutllcSucceeded = 0,
            PutllcFailed = 1,
            PutllucCompleted = 1 << 1,
            GetllarCompleted = 1 << 2,
        }

        internal enum EventStat : uint
        {
            Tg = 1 << 0,    // MFC tag-group status update event
            Sn = 1 << 1,    // MFC list command stall-and-notify event 
            Qv = 1 << 3,    // MFC SPU command queue available event
            Mb = 1 << 4,    // SPU inbound mailbox available event
            Tm = 1 << 5,    // SPU decrementer event
            Me = 1 << 6,    // SPU outbound interrupt mailbox available event
            Le = 1 << 7,    // SPU outbound mailbox available event 
            S2 = 1 << 8,    // SPU signal notification 2 available event 
            S1 = 1 << 9,    // SPU signal notification 1 available event 
            Lr = 1 << 10,   // Lock line reservation lost event
            A = 1 << 11,    // Privileged attention event
            Ms = 1 << 12,   // Multisource synchronization event 
        }

        // Size of SPU cache line in bytes
        internal const int CacheLineSize = 128;
        internal const uint GlobalCacheLineMask = 0xFFFFFF80;
        internal const uint LocalCacheLineMask = GlobalCacheLineMask & LocalStorageMask;

        // Total ports
        internal const int PortCount = 64;

        // Empty port value
        internal const uint FreePort = 0xFFFFFFFF;

        // Port mappings
        private readonly LV2EventQueue?[] _ports = new LV2EventQueue?[PortCount];

        // internal MFC channels
        private uint _eventMask;
        private uint _lsa;                
        private uint _eah;               
        private uint _eal;                
        private uint _transferSize;       
        private uint _tagId;              
        private uint _tagMask;           
        private AtomicStat _atomicStat;   

        private uint _eventStat;
        private uint _lastReservedAddress;
        private readonly ManualResetEventSlim _lostReservationEvent = new(false);

        private readonly BlockingCollection<uint> _outMBox = new(boundedCapacity: 4);
        private readonly BlockingCollection<uint> _inMBox = new(boundedCapacity: 4);

        private readonly LV2SpuThreadGroup? _ownerGroup;

        internal Task? CurrentTask { get; private set; }
        internal string ThreadName { get; }
        internal uint ID { get; }
        internal uint GroupID { get; }
        internal SysSpu.SysSpuImage Image { get; }
        internal SysSpu.SysSpuThreadArgument Argument { get; }
        internal Lock Lock { get; } = new();
        internal CancellationTokenSource LocalCts { get; } = new();
        internal bool ShouldExit => LocalCts.IsCancellationRequested || (_ownerGroup != null && _ownerGroup.GroupCts.IsCancellationRequested);

        // Allocate State
        internal NativeMemoryHandle<SpuState> StateHandle { get; private set; } = new((nuint)sizeof(SpuState));
        internal SpuState* State => StateHandle.NativePointer;

        // Allocate local storage
        internal const int LocalStorageSize = 256 * 1024;
        internal const int LocalStorageMask = LocalStorageSize - 1;
        internal NativeMemoryHandle<byte> LsHandle { get; private set; } = new(LocalStorageSize);
        internal byte* LsPtr => LsHandle.NativePointer;

        internal ThreadStatus Status { get; private set; } = ThreadStatus.Stopped;
        internal MemoryManager MemoryManager { get; }

        internal SpuThread(string name, uint id, uint groupID, SysSpu.SysSpuImage img, SysSpu.SysSpuThreadArgument arg, MemoryManager memoryManager, LV2SpuThreadGroup? ownerGroup)
        {
            MemoryManager = memoryManager;
            ThreadName = name;
            ID = id;
            GroupID = groupID;
            Image = img;
            Argument = arg;
            _ownerGroup = ownerGroup;
        }

        internal void WriteInMbox(uint value) => _inMBox.Add(value);

        internal bool PortIsFree(int port) => _ports[port] == null;

        internal void SetPort(int port, LV2EventQueue equeue)
        {            
            if (!PortIsFree(port))
            {
                throw new Exception($"[Spu] Port is not free");
            }

            _ports[port] = equeue;
        }

        internal void Start()
        {
            unsafe
            {
                *State = default;
                _lostReservationEvent.Reset();

                // Default stack pointer
                State->GPR[1].UInts[3] = 0x3FFF0;

                LoadImage();
                SetArgs();

                State->PC = Image.Entry;
            }

            Status = ThreadStatus.Running;
            CurrentTask = Task.Run(RunSpu);
        }

        internal void LoadImage()
        {
            if (Image.Entry == 0)
            {
                throw new Exception($"[Spu] Image null pointer");
            }

            BigEndianGuestPtr<SysSpu.SysSpuSegment> segmentsPtr = Image.SegmentsPtr;
            uint count = (uint)Image.SegmentsCount;

            ReadOnlySpan<SysSpu.SysSpuSegment> segments = MemoryManager.ReadArray<SysSpu.SysSpuSegment>(segmentsPtr, (int)count);

            for (int i = 0; i < segments.Length; i++)
            {
                LoadSegment(segments[i]);
            }
        }

        private void LoadSegment(SysSpu.SysSpuSegment segment)
        {
            int size = segment.Size;
            uint src = segment.Src;
            uint ls = segment.LSAddress;

            //Console.WriteLine($"[Spu] Loading Spu segment at 0x{src:X8} to local address 0x{ls:X8} type = {segment.Type}");

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

        internal void RunSpu()
        {
            // TODO: Add shutdown path / cancellation source through thread manager

            try
            {
                while (!ShouldExit)
                {
                    SpuInterpreter.Step(this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"Full trace:\n {ex.StackTrace}");
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

        internal SpuInstruction ReadInstruction()
        {
            unsafe
            {
                SpuInstruction instruction = new()
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
           // Console.WriteLine($"ReadChannel {channel} pc = 0x{State->PC:X}");

            switch (channel)
            {
                case MfcChannel.RdEventStat:
                    return ReadEventStat();

                case MfcChannel.RdEventMask:
                    return _eventMask;

                case MfcChannel.RdTagMask:
                    return _tagMask;

                // Stub
                case MfcChannel.RdTagStat:
                    return _tagMask;

                case MfcChannel.RdAtomicStat:
                    return (uint)_atomicStat;

                case MfcChannel.RdInMbox: // Blocking
                    return _inMBox.Take();

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[Spu] Unhandled read from channel: 0x{channelNumber:X} at: 0x{pc:X}");
            }
        }

        internal uint ReadChannelCount(int channelNumber)
        {
            MfcChannel channel = (MfcChannel)channelNumber;
           // Console.WriteLine($"ReadChannelCount {channel} pc = 0x{State->PC:X}");

            switch (channel)
            {
                // Stub
                case MfcChannel.WrTagUpdate:
                    return 1;

                // Not sure how this works but the manual says "It returns 1 if the status is available"
                case MfcChannel.RdTagStat:
                    return 1;

                case MfcChannel.RdInMbox:
                    return (uint)_inMBox.Count;

                // Stub: Always report that there is an available slot in the mailbox
                case MfcChannel.WrOutIntrMbox:
                    return 1;

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[Spu] Unhandled read channel count from channel: 0x{channelNumber:X} at: 0x{pc:X}");
            }
        }

        internal void WriteChannel(int channelNumber, uint value)
        {
            MfcChannel channel = (MfcChannel)channelNumber;
           // Console.WriteLine($"WriteChannel {channel} pc = 0x{State->PC:X}");

            switch (channel)
            {
                case MfcChannel.WrEventMask:
                    _eventMask = value;
                    break;

                case MfcChannel.WrEventAck:
                    _eventStat &= ~value;
                    break;

                case MfcChannel.Lsa:
                    _lsa = value;
                    break;

                case MfcChannel.Eah:
                    _eah = value;
                    break;

                case MfcChannel.Eal:
                    _eal = value;
                    break;

                case MfcChannel.Size:
                    _transferSize = value;
                    break;

                case MfcChannel.TagId:
                    _tagId = value;
                    break;

                case MfcChannel.Cmd:
                    ExecuteCommand(value);
                    break;

                case MfcChannel.WrTagMask:
                    _tagMask = value;
                    break;

                case MfcChannel.WrTagUpdate:
                    //Console.WriteLine($"[Spu] TODO WrTagUpdate");
                    break;

                case MfcChannel.WrOutMbox: // Blocking
                    _outMBox.Add(value);
                    break;

                case MfcChannel.WrOutIntrMbox:
                    WriteOutIntrMbox(value);
                    break;

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[Spu] Unhandled write to channel: 0x{channelNumber:X} at: 0x{pc:X}");
            }
        }

        private void ExecuteCommand(uint commandNumber)
        {
            MfcCommand command = (MfcCommand)commandNumber;
            switch (command)
            {
                case MfcCommand.Put:
                    Put();
                    break;

                case MfcCommand.Get:
                case MfcCommand.Getb:
                    Get();
                    break;

                case MfcCommand.Putllc:
                    Putllc();
                    break;

                case MfcCommand.Getllar:
                    Getllar();
                    break;

                default:
                    uint pc = State->PC - 4;
                    throw new Exception($"[Spu] Unhandled MFC Command: 0x{commandNumber:X} at: 0x{pc:X}");
            }
        }

        private unsafe Span<byte> GetStorageSpan(uint lsa, int size)
        {
            byte* nativeAddress = LsPtr + lsa;
            return new(nativeAddress, size);
        }

        private void Put()
        {
            // transfers the number of bytes specified by the transfer size parameter from the 
            // local storage address of the SPU to the effective address.
            int size = (int)_transferSize;

            unsafe
            {
                Span<byte> src = GetStorageSpan(_lsa, size);
                MemoryManager.CopyBufferToVirtualMemory(src, _eal);
            }

            Console.WriteLine($"[Spu: 0x{ID:X8}] MFC Put: ls = 0x{_lsa:X}, eal = 0x{_eal:X}, size = 0x{_transferSize:X} pc = 0x{State->PC:X}");
        }

        private void Get()
        {
            // Transfers the number of bytes specified by the transfer size parameter from the
            // effective address to the local storage address of the SPU
            int size = (int)_transferSize;

            unsafe
            {
                Span<byte> dst = GetStorageSpan(_lsa, size);
                MemoryManager.ReadBufferFromVirtualMemory(_eal, dst);
            }

            Console.WriteLine($"[Spu: 0x{ID:X8}] MFC Get: ls = 0x{_lsa:X}, eal = 0x{_eal:X}, size = 0x{_transferSize:X} pc = 0x{State->PC:X}");
        }

        private void Putllc()
        {
            // Puts lock line conditional on a reservation
            uint srcAddress = _lsa & LocalCacheLineMask;
            uint destAddress = _eal & GlobalCacheLineMask;
            bool success;

            unsafe
            {
                Span<byte> src = GetStorageSpan(srcAddress, CacheLineSize);
                success = MemoryManager.StoreConditional(destAddress, ID, src);
                _atomicStat = success ? AtomicStat.PutllcSucceeded : AtomicStat.PutllcFailed;
            }

            _lostReservationEvent.Reset();
            _lastReservedAddress = 0;

            Console.WriteLine($"[Spu: 0x{ID:X8}] MFC Putllc: ls = 0x{_lsa:X}, eal = 0x{_eal:X} success: {success} pc = 0x{State->PC:X}");
        }

        private void Getllar()
        {
            // Gets a lock line and creates a reservation
            uint srcAddress = _eal & GlobalCacheLineMask;
            uint destAddress = _lsa & LocalCacheLineMask;

            // Issuing another Getllar while an address is already reserved
            // causes the existing reservation to be lost
            if (_lastReservedAddress != 0 && srcAddress != _lastReservedAddress)
            {
                _eventStat |= (uint)EventStat.Lr;
                MemoryManager.InvalidateReservations(_lastReservedAddress, CacheLineSize);
            }

            _lostReservationEvent.Reset();

            unsafe
            {
                Span<byte> dest = GetStorageSpan(destAddress, CacheLineSize);
                MemoryManager.LoadAndReserve(srcAddress, ID, dest, _lostReservationEvent);
            }

            _lastReservedAddress = srcAddress;
            _atomicStat = AtomicStat.GetllarCompleted;

            Console.WriteLine($"[Spu: 0x{ID:X8}] MFC Getllar: ls = 0x{_lsa:X}, eal = 0x{_eal:X} pc = 0x{State->PC:X}");
        }

        private uint ReadEventStat()
        {
            // A read from the SPU Read Event Status Channel when it has a channel count of 0 results in an SPU stall; 
            // thereby providing a “wait on event” function.

            if ((_eventStat & _eventMask) != 0)
            {
                return _eventStat & _eventMask;
            }

            // Currently we only support waiting on reservation lost event
            uint lr = (uint)EventStat.Lr;

            if (_eventMask != lr)
            {
                throw new Exception($"TODO: wait on mask = 0x{_eventMask:X}");
            }

            // We must have a reserved address to wait on
            if (_lastReservedAddress == 0)
            {
                throw new Exception($"TODO: wait on lr event with no reserved address");
            }

            Console.WriteLine($"[Spu: 0x{ID:X8}] waiting on RdEventStat mask = 0x{_eventMask:X} addr = 0x{_lastReservedAddress:X}");

            _lostReservationEvent.Wait();
            _lostReservationEvent.Reset();

            _eventStat |= lr;
            _lastReservedAddress = 0;

            // TODO: Handle channel count stuff
            return _eventStat & _eventMask;
        }

        private void WriteOutIntrMbox(uint info)
        {
            ulong code = info >> 24;
            ulong port = code & 0x3F;

            LV2EventQueue? equeue = _ports[port];
            ulong data0 = (info & 0xFFFFFF) | (port << 32);

            if (code >= 64)
            {
                throw new NotImplementedException($"[Spu] todo: WriteOutIntrMbox code {port}");
            }

            if (!_outMBox.TryTake(out uint data1))
            {
                throw new Exception($"[Spu] unexpected WrOutIntrMbox on empty OutMbox");
            }

            if (equeue == null)
            {
                throw new Exception($"[Spu] unexpected WrOutIntrMbox on non assigned port");
            }

            LV2EventPort.SysEventData eventData = new()
            {
                Source = SysSpu.SysSpuThreadEventUserKey,
                Data1 = ID,
                Data2 = data0,
                Data3 = data1,
            };

            equeue.Send(eventData);
            _inMBox.Add(CellOk);
            
           // Console.WriteLine($"[Spu] WriteOutIntrMbox -> equeue: 0x{equeue.Handle:X8}");
        }

        private void GroupExit(uint status)
        {
            // Make sure this SPU thread is a member of a group
            if (_ownerGroup == null)
            {
                throw new InvalidOperationException($"[SPU-{ThreadName}] Invalid group exit -- owner group is null");
            }

            _ownerGroup.GroupExit((int)status);
        }

        internal void Stop(int code)
        {
            uint exitStatus;

            switch (code)
            {
                // Group Exit
                case 0x101:
                    if (!_outMBox.TryTake(out exitStatus))
                    {
                        Console.WriteLine($"[SPU-{ThreadName}] Couldn't get exit status from _outMBox");
                    }

                    Console.WriteLine($"[SPU-{ThreadName}] Stop: 0x{code:X} Exit Status: 0x{exitStatus:X}");
                    GroupExit(exitStatus);
                    break;

                // Thread Exit
                case 0x102:
                    if (!_outMBox.TryTake(out exitStatus))
                    {
                        Console.WriteLine($"[SPU-{ThreadName}] Couldn't get exit status from _outMBox");
                    }

                    Console.WriteLine($"[SPU-{ThreadName}] Stop: 0x{code:X} Exit Status: 0x{exitStatus:X}");
                    LocalCts.Cancel();
                    break;
            }
        }

        public void Dispose()
        {
            StateHandle.Dispose();
            LsHandle.Dispose();
        }
    }
}
