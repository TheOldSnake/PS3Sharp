using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.SPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
namespace PS3Sharp.OS.Syscall
{
    internal static class SysSPU
    {
        internal const uint SYS_SPU_THREAD_EVENT_USER = 1;

        internal const ulong SYS_SPU_THREAD_EVENT_USER_KEY = 0xFFFFFFFF53505501;

        internal const uint SYS_SPU_THREAD_GROUP_EVENT_RUN = 1;
        internal const uint SYS_SPU_THREAD_GROUP_EVENT_EXCEPTION = 2;
        internal const uint SYS_SPU_THREAD_GROUP_EVENT_SYSTEM_MODULE = 4;

        internal const ulong SYS_SPU_THREAD_GROUP_EVENT_RUN_KEY = 0xFFFFFFFF53505500;
        internal const ulong SYS_SPU_THREAD_GROUP_EVENT_EXCEPTION_KEY = 0xFFFFFFFF53505503;
        internal const ulong SYS_SPU_THREAD_GROUP_EVENT_SYSTEM_MODULE_KEY = 0xFFFFFFFF53505504;

        internal const int SYS_SPU_SEGMENT_TYPE_COPY = 0x0001;
        internal const int SYS_SPU_SEGMENT_TYPE_FILL = 0x0002;
        internal const int SYS_SPU_SEGMENT_TYPE_INFO = 0x0004;


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

        private static readonly ConcurrentDictionary<uint, LV2SPUThreadGroup> _SPUThreadGroupes = new();

        internal static void AddSPUThreadGroup(LV2SPUThreadGroup group)
        {
            if (!_SPUThreadGroupes.TryAdd(group.Handle, group))
            {
                throw new Exception($"[SysSPU] Failed to add SPU group");
            }
        }

        internal static void RemoveSPUThreadGroup(uint handle)
        {
            if (!_SPUThreadGroupes.TryRemove(handle, out _))
            {
                throw new Exception($"[SysSPU] Failed to remove SPU group");
            }
        }

        internal static bool GetSPUThreadGroup(uint handle, out LV2SPUThreadGroup group)
        {
            return _SPUThreadGroupes.TryGetValue(handle, out group);
        }

        internal static ulong SysSPUInitialize(PPU ppu)
        {
            return CELL_OK;
        }


        // TODO
        internal static ulong SysSPUThreadGroupCreate(PPU ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            uint num = ppu.ReadArg<uint>(ARG1);
            int prio = ppu.ReadArg<int>(ARG2);
            GuestPtr<SysSPUThreadGroupAttribute> attr_ptr = ppu.ReadArg<GuestPtr<SysSPUThreadGroupAttribute>>(ARG3);

            // Create the thread group
            LV2SPUThreadGroup group = new(attr_ptr, num, prio);
            AddSPUThreadGroup(group);
            MemoryManager.Write<uint>(id_ptr, group.Handle);

            Console.WriteLine($"[SysSPUThreadInitialize] Created SPU group: 0x{group.Handle:X8}");
            return CELL_OK;
        }

        internal static ulong SysSPUThreadInitialize(PPU ppu)
        {
            GuestPtr<uint> id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            uint group_id = ppu.ReadArg<uint>(ARG1);
            uint spu_num = ppu.ReadArg<uint>(ARG2);     // ?

            GuestPtr<SysSPUImage> img_ptr = ppu.ReadArg<GuestPtr<SysSPUImage>>(ARG3);
            GuestPtr<SysSPUThreadAttribute> attr_ptr = ppu.ReadArg<GuestPtr<SysSPUThreadAttribute>>(ARG4);
            GuestPtr<SysSPUThreadArgument> arg_ptr = ppu.ReadArg<GuestPtr<SysSPUThreadArgument>>(ARG5);

            // there is a lot more checks and errors to be added
            if (!GetSPUThreadGroup(group_id, out LV2SPUThreadGroup threadGroup))
            {
                return CELL_ESRCH;
            }

            uint count = (uint)(threadGroup.SPUThreads.Count & 0xFF);
            uint id = (count << 24) | (group_id & 0xFFFFFF);

            SysSPUImage img = img_ptr.Read();
            SysSPUThreadAttribute attribute = attr_ptr.Read();
            SysSPUThreadArgument arg = arg_ptr.Read();

            GuestVoidPtr name_ptr = attribute.NamePtr;
            string thread_name = name_ptr.IsNullPtr ? "Unnamed-SPU" : MemoryManager.ReadString(name_ptr);

            SPU spu = new(thread_name, id, group_id, img, arg);
            threadGroup.AddThread(spu);

            MemoryManager.Write<uint>(id_ptr, id);

            Console.WriteLine($"[SysSPUThreadInitialize] Created SPU thread: {thread_name} - Group: 0x{group_id:X8}");
            return CELL_OK;
        }

        internal static ulong SysSPUThreadGroupStart(PPU ppu)
        {
            uint group_id = ppu.ReadArg<uint>(ARG0);

            if (!GetSPUThreadGroup(group_id, out LV2SPUThreadGroup threadGroup))
            {
                return CELL_ESRCH;
            }

            threadGroup.StartGroup();

            Console.WriteLine($"[SysSPUThreadGroupStart] Started SPU group: 0x{group_id:X8}");
            throw new Exception("todo");
            return CELL_OK;
        }

        internal static ulong SysSPUThreadGroupConnectEventAllThreads(PPU ppu)
        {
            uint group_id = ppu.ReadArg<uint>(ARG0);
            uint equeue_id = ppu.ReadArg<uint>(ARG1);
            ulong req = ppu.ReadArg<ulong>(ARG2);                        // port bits
            GuestPtr<byte> spup_ptr = ppu.ReadArg<GuestPtr<byte>>(ARG3); // chosen port 

            if (!GetSPUThreadGroup(group_id, out LV2SPUThreadGroup threadGroup))
            {
                return CELL_ESRCH;
            }


            // Find a port that is free on all SPU threads 
            List<SPU> threads = threadGroup.SPUThreads;
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
                return CELL_EISCONN;
            }

            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].SetPort(port, equeue_id);
            }

            MemoryManager.Write<byte>(spup_ptr, port);

            Console.WriteLine($"[SysSPUThreadGroupConnectEventAllThreads] Connected SPU group: 0x{group_id:X8} port = {port}");
            return CELL_OK;
        }
    }
}
