using OpenTK.Graphics.ES20;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.SPU;
using PS3Sharp.Common;
using PS3Sharp.Elf;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
namespace PS3Sharp.OS.Syscall
{
    internal class SysSPU
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSPUSegment : IPS3Struct
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
        internal struct SysSPUImage : IPS3Struct
        {
            internal BigEndian<uint> Type;
            internal BigEndian<uint> Entry;
            internal BigEndian<uint> SegmentsPtr;
            internal BigEndian<uint> SegmentsCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSPUThreadGroupAttribute : IPS3Struct
        {
            internal BigEndian<uint> NameLength;
            internal BigEndian<uint> NamePtr;
            internal BigEndian<uint> Type;
            internal BigEndian<uint> ContainerID; // Memory container id
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSPUThreadAttribute : IPS3Struct
        {
            internal BigEndian<uint> NamePtr;
            internal BigEndian<uint> NameLength;
            internal BigEndian<uint> Option;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SysSPUThreadArgument : IPS3Struct
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
        private readonly SPUElfLoader _elfLoader = new();
        private readonly ConcurrentDictionary<uint, LV2SPUThreadGroup> _spuThreadGroupes = new();

        internal SysSPU(ThreadManager threadManager, MemoryManager memoryManager)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
        }

        private void AddSPUThreadGroup(LV2SPUThreadGroup group)
        {
            if (!_spuThreadGroupes.TryAdd(group.Handle, group))
            {
                throw new Exception($"[SysSPU] Failed to add SPU group");
            }
        }

        private void RemoveSPUThreadGroup(uint handle)
        {
            if (!_spuThreadGroupes.TryRemove(handle, out _))
            {
                throw new Exception($"[SysSPU] Failed to remove SPU group");
            }
        }

        internal bool GetSPUThreadGroup(uint handle, out LV2SPUThreadGroup group)
        {
            return _spuThreadGroupes.TryGetValue(handle, out group);
        }

        internal ulong SysSPUInitialize(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong SysSPUImageImport(PPUThread ppu)
        {
            GuestPtr<SysSPUImage> imagePtr = ppu.ReadArg<GuestPtr<SysSPUImage>>(Arg0);
            uint src = ppu.ReadArg<uint>(Arg1); // Ptr to elf32 image
            uint type = ppu.ReadArg<uint>(Arg2);

            if (type != SysSpuImageProtect && type != SysSpuImageDirect)
            {
                return CellEInval;
            }

            // Load elf32
            (ElfDefinitions.ElfHeader32 header, List<SysSPUSegment> segments) = _elfLoader.LoadSPUElf(src, _memoryManager);

            // Allocate SysSPUSegment array
            int segmentArraySize = Unsafe.SizeOf<SysSPU.SysSPUSegment>() * 32;
            GuestPtr<SysSPU.SysSPUSegment> segmentArrayPtr = _memoryManager.AllocateVirtualMemory((uint)segmentArraySize).VirtualAddress;

            if (segments.Count > 0)
            {
                // Copy segment array
                Span<SysSPUSegment> segmentsSpan = CollectionsMarshal.AsSpan(segments);
                _memoryManager.WriteArray(segmentArrayPtr, segmentsSpan);
            }

            // Write SysSPUImage data
            BigEndian<uint> actualEntry = _memoryManager.Read<BigEndian<uint>>(header.e_entry);
            SysSPUImage image = new()
            {
                Type = type,
                Entry = actualEntry,
                SegmentsPtr = (uint)segmentArrayPtr,
                SegmentsCount = (uint)segments.Count
            };

            _memoryManager.Write(imagePtr, image);

            Console.WriteLine($"[SysPrxForUser] sys_spu_image_import: Type = 0x{type:X} entry = 0x{actualEntry:X8} segment count = {segments.Count}");

            return CellOk;
        }

        // TODO
        internal ulong SysSPUThreadGroupCreate(PPUThread ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(Arg0);
            uint num = ppu.ReadArg<uint>(Arg1);
            int prio = ppu.ReadArg<int>(Arg2);
            GuestPtr<SysSPUThreadGroupAttribute> attr_ptr = ppu.ReadArg<GuestPtr<SysSPUThreadGroupAttribute>>(Arg3);

            // Create the thread group
            LV2SPUThreadGroup group = new(attr_ptr, num, prio, _memoryManager);
            AddSPUThreadGroup(group);
            _memoryManager.Write<BigEndian<uint>>(id_ptr, group.Handle);

            Console.WriteLine($"[SysSPUThreadInitialize] Created SPU group: 0x{group.Handle:X8}");
            return CellOk;
        }

        internal ulong SysSPUThreadInitialize(PPUThread ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(Arg0);
            uint group_id = ppu.ReadArg<uint>(Arg1);
            uint spu_num = ppu.ReadArg<uint>(Arg2);     // ?

            GuestPtr<SysSPUImage> img_ptr = ppu.ReadArg<GuestPtr<SysSPUImage>>(Arg3);
            GuestPtr<SysSPUThreadAttribute> attr_ptr = ppu.ReadArg<GuestPtr<SysSPUThreadAttribute>>(Arg4);
            GuestPtr<SysSPUThreadArgument> arg_ptr = ppu.ReadArg<GuestPtr<SysSPUThreadArgument>>(Arg5);

            // there is a lot more checks and errors to be added
            if (!GetSPUThreadGroup(group_id, out LV2SPUThreadGroup threadGroup))
            {
                return CellESrch;
            }

            uint count = (uint)(threadGroup.SPUThreads.Count & 0xFF);
            uint id = (count << 24) | (group_id & 0xFFFFFF);

            SysSPUImage img = _memoryManager.Read<SysSPUImage>(img_ptr);
            SysSPUThreadAttribute attribute = _memoryManager.Read<SysSPUThreadAttribute>(attr_ptr);
            SysSPUThreadArgument arg = _memoryManager.Read<SysSPUThreadArgument>(arg_ptr);

            GuestVoidPtr name_ptr = attribute.NamePtr;
            string thread_name = name_ptr.IsNullPtr ? "Unnamed-SPU" : _memoryManager.ReadString(name_ptr);

            SPUThread spu = _threadManager.CreateSPUThread(thread_name, id, group_id, img, arg);
            threadGroup.AddThread(spu);

            _memoryManager.Write<BigEndian<uint>>(id_ptr, id);

            Console.WriteLine($"[SysSPUThreadInitialize] Created SPU thread: {thread_name} - Group: 0x{group_id:X8} entry = 0x{img.Entry:X8}");
            return CellOk;
        }

        internal ulong SysSPUThreadGroupStart(PPUThread ppu)
        {
            uint group_id = ppu.ReadArg<uint>(Arg0);

            if (!GetSPUThreadGroup(group_id, out LV2SPUThreadGroup threadGroup))
            {
                return CellESrch;
            }

            threadGroup.StartGroup();

            Console.WriteLine($"[SysSPUThreadGroupStart] Started SPU group: 0x{group_id:X8}");
            throw new Exception("todo");
            return CellOk;
        }

        internal ulong SysSPUThreadGroupConnectEventAllThreads(PPUThread ppu)
        {
            uint group_id = ppu.ReadArg<uint>(Arg0);
            uint equeue_id = ppu.ReadArg<uint>(Arg1);
            ulong req = ppu.ReadArg<ulong>(Arg2);                        // port bits
            GuestPtr<byte> spup_ptr = ppu.ReadArg<GuestPtr<byte>>(Arg3); // chosen port 

            if (!GetSPUThreadGroup(group_id, out LV2SPUThreadGroup threadGroup))
            {
                return CellESrch;
            }

            // Find a port that is free on all SPU threads 
            List<SPUThread> threads = threadGroup.SPUThreads;
            byte port;
            bool foundFreePort = false;

            for (port = 0; port < 64; port++)
            {

                // Skip 
                if (((req >> port) & 1) == 0)
                {
                    continue;
                }

                // Check all threads      
                foundFreePort = true;

                for (int i = 0; i < threads.Count; i++)
                {

                    if (!threads[i].PortIsFree(port))
                    {
                        foundFreePort = false;
                        break;
                    }
                }

                if (foundFreePort)
                {
                    break;
                }
            }


            if (!foundFreePort)
            {
                return CellEIsConn;
            }

            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].SetPort(port, equeue_id);
            }

            _memoryManager.Write<byte>(spup_ptr, port);

            Console.WriteLine($"[SysSPUThreadGroupConnectEventAllThreads] Connected SPU group: 0x{group_id:X8} port = {port}");
            return CellOk;
        }

        internal ulong SysSPUThreadReadLS(PPUThread thread)
        {
            throw new NotImplementedException("todo: SysSPUThreadReadLS");
        }

        internal ulong SysSPUThreadWriteSPUMB(PPUThread thread)
        {
            throw new NotImplementedException("todo: SysSPUThreadWriteSPUMB");
        }

        internal ulong SysSpuThreadGroupConnectEventAllThreads(PPUThread thread)
        {
            throw new NotImplementedException("todo: SysSpuThreadGroupConnectEventAllThreads");
        }
    }
}
