using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.Spu;
using PS3Sharp.Common;
using PS3Sharp.Elf;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
namespace PS3Sharp.OS.Syscall
{
    internal class SysSpu
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSpuSegment : IPS3Struct
        {
            internal BigEndian<int> Type;            // Segment type (COPY / FILL / INFO)
            internal BigEndian<uint> LSAddress;      // Addr in LS that this segment will be loaded to
            internal BigEndian<int> Size;

            // Address of the segment in memory, for COPY type segments OR
            // The value to fill the segment with, for FILL type segments OR
            // Padding
            internal BigEndian<uint> Src;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSpuImage : IPS3Struct
        {
            internal BigEndian<uint> Type;
            internal BigEndian<uint> Entry;
            internal BigEndian<uint> SegmentsPtr;
            internal BigEndian<uint> SegmentsCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSpuThreadGroupAttribute : IPS3Struct
        {
            internal BigEndian<uint> NameLength;
            internal BigEndian<uint> NamePtr;
            internal BigEndian<uint> Type;
            internal BigEndian<uint> ContainerID; // Memory container id
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSpuThreadAttribute : IPS3Struct
        {
            internal BigEndian<uint> NamePtr;
            internal BigEndian<uint> NameLength;
            internal BigEndian<uint> Option;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSpuThreadArgument : IPS3Struct
        {
            internal BigEndian<ulong> Arg1;
            internal BigEndian<ulong> Arg2;
            internal BigEndian<ulong> Arg3;
            internal BigEndian<ulong> Arg4;
        }

        internal const uint SysSpuImageProtect = 0;
        internal const uint SysSpuImageDirect = 1;

        internal const uint SysSpuImageTypeUser = 0;
        internal const uint SysSpuImageTypeKernel = 1;

        internal const uint SysSpuThreadEventUser = 1;
        internal const ulong SysSpuThreadEventUserKey = 0xFFFFFFFF53505501;
        internal const uint SysSpuThreadGroupEventRun = 1;
        internal const uint SysSpuThreadGroupEventException = 2;
        internal const uint SysSpuThreadGroupEventSystemModule = 4;
        internal const ulong SysSpuThreadGroupEventRunKey = 0xFFFFFFFF53505500;
        internal const ulong SysSpuThreadGroupEventExceptionKey = 0xFFFFFFFF53505503;
        internal const ulong SysSpuThreadGroupEventSystemModuleKey = 0xFFFFFFFF53505504;

        internal const int SysSpuSegmentTypeCopy = 0x0001;
        internal const int SysSpuSegmentTypeFill = 0x0002;
        internal const int SysSpuSegmentTypeInfo = 0x0004;

        internal const int SysSpuThreadGroupJoinGroupExit = 1;

        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;
        private readonly SysEvent _sysEvent;

        private readonly SpuElfLoader _elfLoader = new();
        private readonly ConcurrentDictionary<uint, LV2SpuThreadGroup> _spuThreadGroupes = new();

        private int _maxUsableSpu = 6;
        private int _maxRawSpu = 0;

        internal SysSpu(ThreadManager threadManager, MemoryManager memoryManager, SysEvent sysEvent)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
            _sysEvent = sysEvent;
        }

        internal ulong SysSpuInitialize(PPUThread ppu)
        {
            int maxUsableSpu = ppu.ReadArg<int>(Arg0);
            int maxRawSpu = ppu.ReadArg<int>(Arg1);

            Console.WriteLine($"[SysSpuInitialize] maxUsableSpu = {maxUsableSpu} maxRawSpu = {maxRawSpu}");

            if (maxRawSpu > 5)
            {
                return CellEInval;
            }

            // Limit maximum usable spu cores to 6
            _maxUsableSpu = int.Min(maxUsableSpu, 6);
            _maxRawSpu = _maxUsableSpu - maxRawSpu;

            return CellOk;
        }

        internal ulong SysSpuImageOpen(PPUThread ppu)
        {
            GuestPtr<SysSpuImage> imagePtr = ppu.ReadArg<GuestPtr<SysSpuImage>>(Arg0);
            GuestVoidPtr pathPtr = ppu.ReadArg<GuestVoidPtr>(Arg1);

            string path = _memoryManager.ReadString(pathPtr);
            (ElfDefinitions.ElfHeader32 header, List<SysSpuSegment> segments) = _elfLoader.GetSpuElfInfo(path, _memoryManager);
            WriteSpuImageInfo(header, segments, imagePtr);

            Console.WriteLine($"[SysSpuImageOpen] Path: {path} entry = 0x{header.e_entry:X8} segment count = {segments.Count}");

            return CellOk;
        }

        internal ulong SysSpuImageImport(PPUThread ppu)
        {
            GuestPtr<SysSpuImage> imagePtr = ppu.ReadArg<GuestPtr<SysSpuImage>>(Arg0);
            uint src = ppu.ReadArg<uint>(Arg1); // Ptr to elf32 image
            uint type = ppu.ReadArg<uint>(Arg2);

            if (type != SysSpuImageProtect && type != SysSpuImageDirect)
            {
                return CellEInval;
            }

            // Load elf32
            (ElfDefinitions.ElfHeader32 header, List<SysSpuSegment> segments) = _elfLoader.GetSpuElfInfo(src, _memoryManager);
            WriteSpuImageInfo(header, segments, imagePtr);

            Console.WriteLine($"[SysSpuImageImport] Type = 0x{type:X} entry = 0x{header.e_entry:X8} segment count = {segments.Count}");

            return CellOk;
        }

        internal ulong SysSpuThreadGroupCreate(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> idPtr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            uint num = ppu.ReadArg<uint>(Arg1);
            int prio = ppu.ReadArg<int>(Arg2);
            GuestPtr<SysSpuThreadGroupAttribute> attribPtr = ppu.ReadArg<GuestPtr<SysSpuThreadGroupAttribute>>(Arg3);

            // Create the thread group
            LV2SpuThreadGroup group = new(_threadManager, _memoryManager, attribPtr, num, prio);
            AddSpuThreadGroup(group);
            _memoryManager.Write<BigEndian<uint>>(idPtr, group.Handle);

            Console.WriteLine($"[SysSpuThreadInitialize] Created Spu group: 0x{group.Handle:X8}");
            return CellOk;
        }

        internal ulong SysSpuThreadInitialize(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> idPtr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            uint groupId = ppu.ReadArg<uint>(Arg1);
            uint spuNum = ppu.ReadArg<uint>(Arg2);     // ?

            GuestPtr<SysSpuImage> imgPtr = ppu.ReadArg<GuestPtr<SysSpuImage>>(Arg3);
            GuestPtr<SysSpuThreadAttribute> attribPtr = ppu.ReadArg<GuestPtr<SysSpuThreadAttribute>>(Arg4);
            GuestPtr<SysSpuThreadArgument> argPtr = ppu.ReadArg<GuestPtr<SysSpuThreadArgument>>(Arg5);

            // there is a lot more checks and errors to be added
            if (!TryGetSpuThreadGroup(groupId, out LV2SpuThreadGroup? threadGroup))
            {
                return CellESrch;
            }

            SysSpuImage img = _memoryManager.Read<SysSpuImage>(imgPtr);
            SysSpuThreadAttribute attrib = _memoryManager.Read<SysSpuThreadAttribute>(attribPtr);
            SysSpuThreadArgument arg = _memoryManager.Read<SysSpuThreadArgument>(argPtr);
            SpuThread spu = threadGroup.CreateSpuThread(img, attrib, arg);

            _memoryManager.Write<BigEndian<uint>>(idPtr, spu.ID);

            Console.WriteLine($"[SysSpuThreadInitialize] Created Spu thread: {spu.ThreadName} - Group: 0x{groupId:X8} entry = 0x{img.Entry:X8}");
            return CellOk;
        }

        internal ulong SysSpuThreadGroupStart(PPUThread ppu)
        {
            uint groupId = ppu.ReadArg<uint>(Arg0);

            if (!TryGetSpuThreadGroup(groupId, out LV2SpuThreadGroup? threadGroup))
            {
                return CellESrch;
            }

            threadGroup.StartGroup();

            Console.WriteLine($"[SysSpuThreadGroupStart] Started Spu group: 0x{groupId:X8}");

            return CellOk;
        }

        internal ulong SysSpuThreadGroupJoin(PPUThread ppu)
        {
            uint groupId = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<int>> causePtr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg1);
            GuestPtr<BigEndian<int>> statusPtr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg2);

            if (TryGetSpuThreadGroup(groupId, out LV2SpuThreadGroup? group))
            {
                group.JoinGroup();
                _memoryManager.Write<BigEndian<int>>(causePtr, SysSpuThreadGroupJoinGroupExit);
                _memoryManager.Write<BigEndian<int>>(statusPtr, group.ExitStatus);

                Console.WriteLine($"[SysSpuThreadGroupJoin] Joined group: 0x{groupId:X8} exit status = 0x{group.ExitStatus:X8}");
            }

            return CellOk;
        }

        internal ulong SysSpuThreadGroupConnectEventAllThreads(PPUThread ppu)
        {
            uint groupId = ppu.ReadArg<uint>(Arg0);
            uint equeueId = ppu.ReadArg<uint>(Arg1);
            ulong req = ppu.ReadArg<ulong>(Arg2);                        // port bits
            GuestPtr<byte> spup_ptr = ppu.ReadArg<GuestPtr<byte>>(Arg3); // chosen port 

            if (!TryGetSpuThreadGroup(groupId, out LV2SpuThreadGroup? threadGroup))
            {
                return CellESrch;
            }

            if (!_sysEvent.TryGetEventQueueByID(equeueId, out LV2EventQueue? equeue))
            {
                return CellESrch;
            }

            if (!threadGroup.TryConnectEventAllThreads(req, equeue, out byte portNumber))
            {
                return CellEIsConn;
            }

            // Write selected port
            _memoryManager.Write(spup_ptr, portNumber);

            Console.WriteLine($"[SysSpuThreadGroupConnectEventAllThreads] Connected Spu group: 0x{groupId:X8} port = {portNumber} equeue: 0x{equeueId:X8}");
            return CellOk;
        }

        internal ulong SysSpuThreadReadLS(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            uint srcPtr = ppu.ReadArg<uint>(Arg1);
            uint destPtr = ppu.ReadArg<uint>(Arg2);
            uint type = ppu.ReadArg<uint>(Arg3);

            if (!_threadManager.TryGetSpuThreadByID(id, out SpuThread? spu))
            {
                return CellESrch;
            }

            switch (type)
            {
                case 1:
                    // 8-bit read
                    byte data8 = spu.ReadStorage<byte>(srcPtr);
                    _memoryManager.Write<BigEndian<ulong>>(destPtr, data8);
                    break;

                case 2:
                    // 16-bit read
                    ushort data16 = spu.ReadStorage<BigEndian<ushort>>(srcPtr);
                    _memoryManager.Write<BigEndian<ulong>>(destPtr, data16);
                    break;

                case 4:
                    // 32-bit read
                    uint data32 = spu.ReadStorage<BigEndian<uint>>(srcPtr);
                    _memoryManager.Write<BigEndian<ulong>>(destPtr, data32);
                    break;

                case 8:
                    // 64-bit read (no need to swap)
                    ulong data64 = spu.ReadStorage<ulong>(srcPtr);
                    _memoryManager.Write(destPtr, data64);
                    break;

                default:
                    throw new Exception($"[SysSpuThreadReadLS] Unhandled read type = {type}");
            }

            return CellOk;
        }

        internal ulong SysSpuThreadGroupConnectEvent(PPUThread ppu)
        {
            uint groupId = ppu.ReadArg<uint>(Arg0);
            uint equeueId = ppu.ReadArg<uint>(Arg1);
            uint eventType = ppu.ReadArg<uint>(Arg2);

            if (!TryGetSpuThreadGroup(groupId, out LV2SpuThreadGroup? group))
            {
                return CellESrch;
            }

            if (!_sysEvent.TryGetEventQueueByID(equeueId, out LV2EventQueue? eventQueue))
            {
                return CellESrch;
            }

            if (!group.SetEventQueue(eventType, eventQueue))
            {
                return CellEInval;
            }

            Console.WriteLine($"[SysSpuThreadGroupConnectEvent] Connected Spu group: 0x{groupId:X8} equeue: 0x{equeueId:X8}");
            return CellOk;
        }

        internal ulong SysSpuThreadWriteSpuMb(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            uint value = ppu.ReadArg<uint>(Arg1);

            if (!_threadManager.TryGetSpuThreadByID(id, out SpuThread? spu))
            {
                return CellESrch;
            }

            // Write value to Spu Inbound Mailbox 
            spu.WriteInMbox(value);

            return CellOk;
        }

        internal ulong SysSpuThreadConnectEvent(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            uint equeueId = ppu.ReadArg<uint>(Arg1);
            uint eventType = ppu.ReadArg<uint>(Arg2);   // todo
            int spup = ppu.ReadArg<int>(Arg3);

            if (!_threadManager.TryGetSpuThreadByID(id, out SpuThread? spu))
            {
                return CellESrch;
            }

            if (!_sysEvent.TryGetEventQueueByID(equeueId, out LV2EventQueue? equeue))
            {
                return CellESrch;
            }

            spu.SetPort(spup, equeue);

            Console.WriteLine($"[SysSpuThreadConnectEvent] Connected Spu: 0x{id:X8} equeue: 0x{equeueId:X8}");
            return CellOk;
        }

        internal bool TryGetSpuThreadGroup(uint handle, [NotNullWhen(true)] out LV2SpuThreadGroup? group) => _spuThreadGroupes.TryGetValue(handle, out group);

        private void AddSpuThreadGroup(LV2SpuThreadGroup group)
        {
            if (!_spuThreadGroupes.TryAdd(group.Handle, group))
            {
                throw new Exception($"[SysSpu] Failed to add Spu group");
            }
        }

        private void RemoveSpuThreadGroup(uint handle)
        {
            if (!_spuThreadGroupes.TryRemove(handle, out _))
            {
                throw new Exception($"[SysSpu] Failed to remove Spu group");
            }
        }

        private void WriteSpuImageInfo(ElfDefinitions.ElfHeader32 header, List<SysSpuSegment> segments, GuestPtr<SysSpuImage> imagePtr)
        {
            // Allocate SysSpuSegment array
            int segmentArraySize = Unsafe.SizeOf<SysSpuSegment>() * 32;
            GuestPtr<SysSpuSegment> segmentArrayPtr = _memoryManager.AllocateVirtualMemory((uint)segmentArraySize).VirtualAddress;

            if (segments.Count > 0)
            {
                // Copy segment array
                Span<SysSpuSegment> segmentsSpan = CollectionsMarshal.AsSpan(segments);
                _memoryManager.WriteArray(segmentArrayPtr, segmentsSpan);
            }

            // Write SysSpuImage data
            SysSpuImage image = new()
            {
                Type = SysSpuImageTypeUser,
                Entry = header.e_entry,
                SegmentsPtr = (uint)segmentArrayPtr,
                SegmentsCount = (uint)segments.Count
            };

            _memoryManager.Write(imagePtr, image);
        }
    }
}
