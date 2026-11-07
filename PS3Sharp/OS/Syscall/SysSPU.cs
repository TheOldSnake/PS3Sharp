using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.Spu;
using PS3Sharp.Common;
using PS3Sharp.Elf;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using SkiaSharp;
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

        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;
        private readonly SysEvent _sysEvent;

        private readonly SpuElfLoader _elfLoader = new();
        private readonly ConcurrentDictionary<uint, LV2SpuThreadGroup> _spuThreadGroupes = new();

        internal SysSpu(ThreadManager threadManager, MemoryManager memoryManager, SysEvent sysEvent)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
            _sysEvent = sysEvent;
        }

        internal ulong SysSpuInitialize(PPUThread ppu)
        {
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
            (ElfDefinitions.ElfHeader32 header, List<SysSpuSegment> segments) = _elfLoader.LoadSPUElf(src, _memoryManager);

            // Allocate SysSPUSegment array
            int segmentArraySize = Unsafe.SizeOf<SysSpu.SysSpuSegment>() * 32;
            GuestPtr<SysSpu.SysSpuSegment> segmentArrayPtr = _memoryManager.AllocateVirtualMemory((uint)segmentArraySize).VirtualAddress;

            if (segments.Count > 0)
            {
                // Copy segment array
                Span<SysSpuSegment> segmentsSpan = CollectionsMarshal.AsSpan(segments);
                _memoryManager.WriteArray(segmentArrayPtr, segmentsSpan);
            }

            // Write SysSPUImage data
            SysSpuImage image = new()
            {
                Type = type,
                Entry = header.e_entry,
                SegmentsPtr = (uint)segmentArrayPtr,
                SegmentsCount = (uint)segments.Count
            };

            _memoryManager.Write(imagePtr, image);

            Console.WriteLine($"[SysSPU] sys_spu_image_import: Type = 0x{type:X} entry = 0x{header.e_entry:X8} segment count = {segments.Count}");

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

            Console.WriteLine($"[SysSPUThreadInitialize] Created Spu group: 0x{group.Handle:X8}");
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

            Console.WriteLine($"[SysSPUThreadInitialize] Created Spu thread: {spu.ThreadName} - Group: 0x{groupId:X8} entry = 0x{img.Entry:X8}");
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

            Console.WriteLine($"[SysSPUThreadGroupStart] Started Spu group: 0x{groupId:X8}");
            //throw new Exception("todo");

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
            }

            // TODO

            _memoryManager.Write<BigEndian<int>>(causePtr, 0);
            _memoryManager.Write<BigEndian<int>>(statusPtr, 0);
            return CellOk;
        }

        internal ulong SysSpuThreadGroupConnectEventAllThreads(PPUThread ppu)
        {
            uint groupId = ppu.ReadArg<uint>(Arg0);
            uint equeue_id = ppu.ReadArg<uint>(Arg1);
            ulong req = ppu.ReadArg<ulong>(Arg2);                        // port bits
            GuestPtr<byte> spup_ptr = ppu.ReadArg<GuestPtr<byte>>(Arg3); // chosen port 

            if (!TryGetSpuThreadGroup(groupId, out LV2SpuThreadGroup? threadGroup))
            {
                return CellESrch;
            }

            if (!threadGroup.TryConnectEventAllThreads(req, equeue_id, out byte portNumber))
            {
                return CellEIsConn;
            }

            // Write selected port
            _memoryManager.Write(spup_ptr, portNumber);

            Console.WriteLine($"[SysSPUThreadGroupConnectEventAllThreads] Connected Spu group: 0x{groupId:X8} port = {portNumber} equeue: 0x{equeue_id:X8}");
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

            // Clear 64-bit
            _memoryManager.Write<ulong>(destPtr, 0);

            switch (type)
            {
                case 1:
                    // 8-bit read
                    ReadLs<byte>();
                    break;

                case 2:
                    // 16-bit read
                    ReadLs<ushort>();
                    break;

                case 4:
                    // 32-bit read
                    ReadLs<uint>();
                    break;

                case 8:
                    // 64-bit read
                    ReadLs<ulong>();
                    break;

                default:
                    throw new Exception($"[SysSpuThreadReadLS] Unhandled read type = {type}");
            }

            void ReadLs<T>() where T : unmanaged
            {
                T value = spu.ReadStorage<T>(srcPtr);
                _memoryManager.Write<T>(destPtr, value);
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

            return CellOk;
        }

        internal ulong SysSPUThreadWriteSPUMB(PPUThread thread)
        {
            throw new NotImplementedException("todo: SysSPUThreadWriteSPUMB");
        }

        internal bool TryGetSpuThreadGroup(uint handle, [NotNullWhen(true)] out LV2SpuThreadGroup? group) => _spuThreadGroupes.TryGetValue(handle, out group);

        private void AddSpuThreadGroup(LV2SpuThreadGroup group)
        {
            if (!_spuThreadGroupes.TryAdd(group.Handle, group))
            {
                throw new Exception($"[SysSPU] Failed to add Spu group");
            }
        }

        private void RemoveSpuThreadGroup(uint handle)
        {
            if (!_spuThreadGroupes.TryRemove(handle, out _))
            {
                throw new Exception($"[SysSPU] Failed to remove Spu group");
            }
        }
    }
}
