using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Xml.Linq;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    public static class SysPPUThread
    {
        private static readonly string[] IgnoredThreads = [ 
            // Minecraft -- these PPU threads expect the SPU to be functional, will crash otherwise 
            //"MinecraftSpursHdlr0",
            //"_libjpgdecSpursHdlr0",
            //"_libpngdecSpursHdlr0", 
        ];

        public static ulong SysPPUThreadGetID(PPU ppu)
        {
            GuestPtr<ulong> id_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG0);
            MemoryManager.Write<ulong>(id_ptr, ppu.ThreadID);

            //Console.WriteLine($"[sys_ppu_thread] sys_ppu_thread_get_id: 0x{ppu.ThreadID:X8}");
            return CELL_OK;
        }

        public static ulong SysPPUThreadGetPriority(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<int> prio_ptr = ppu.ReadArg<GuestPtr<int>>(ARG1);

            //Console.WriteLine($"[sys_ppu_thread] sys_ppu_thread_get_priority ID = 0x{id:X8}");
            PPU? thread = ThreadManager.GetThreadByID(id);
            MemoryManager.Write<int>(prio_ptr, thread.ThreadPriority);
            return CELL_OK;
        }

        public static ulong SysPPUThreadSetPriority(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            int priority = ppu.ReadArg<int>(ARG1);

            //Console.WriteLine($"[sys_ppu_thread] sys_ppu_thread_set_priority ID = 0x{id:X8}");
            PPU? thread = ThreadManager.GetThreadByID(id);
            thread?.SetThreadPriority(priority);

            return CELL_OK;
        }

        public static ulong SysPPUThreadCreate(PPU ppu)
        {
            //int sys_ppu_thread_create(sys_ppu_thread_t *thread_id, & void (*entry)(uint64_t), uint64_t arg, 0, int prio, size_t stacksize, uint64_t flags, const char *threadname); 

            //Read arguments
            GuestPtr<ulong> id_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG0);
            GuestVoidPtr entry = ppu.ReadArg<GuestVoidPtr>(ARG1);
            ulong arg = ppu.ReadArg<ulong>(ARG2);
            int prio = ppu.ReadArg<int>(ARG3);   //todo
            uint stacksize = ppu.ReadArg<uint>(ARG4);
            ulong flags = ppu.ReadArg<ulong>(ARG5); //todo
            GuestVoidPtr name_ptr = ppu.ReadArg<GuestVoidPtr>(ARG6);
            string name = name_ptr.IsNullPtr ? "Unnamed-PPU" : MemoryManager.ReadString(name_ptr);

            // Super hack
            // Ideally we should not do this
            if (IgnoredThreads.Contains(name))
            {
                return CELL_OK;
            }

            //Create a new PPU thread
            PPU new_ppu_thread = new PPU(name, entry, arg, stacksize);

            ThreadManager.CreateNativePPUThread(new_ppu_thread);
            ThreadManager.RunPPUThread(new_ppu_thread);

            MemoryManager.Write<ulong>(id_ptr, new_ppu_thread.ThreadID);
            Console.WriteLine($"[SysPPUThread] sys_ppu_thread_create: {name} thread_id = 0x{new_ppu_thread.ThreadID:X8} entry_pc = {entry:X8}");

            return CELL_OK;
        }

        public static ulong SysPPUThreadExit(PPU ppu)
        {
            int exit_code = ppu.ReadArg<int>(ARG0);
            Console.WriteLine($"[SysPPUThread] sys_ppu_thread_exit: {ppu.ThreadName} thread_id = 0x{ppu.ThreadID:X8} exit_code = {exit_code}");
            ppu.StopThread();

            return CELL_OK;
        }

        public static ulong SysPPUThreadYield(PPU ppu)
        {
            Thread.Yield();
            return CELL_OK;
        }

        public static ulong SysPPUThreadJoin(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<ulong> exit_code_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG1);

            int exit_code = ThreadManager.JoinThreadByID(id);
            MemoryManager.Write<ulong>(exit_code_ptr, (ulong)exit_code);

            Console.WriteLine($"[SysPPUThread] sys_ppu_thread_join: caller = {ppu.ThreadName} joins: 0x{id:X8}");
            return CELL_OK;
        }

        public static ulong SysPPUThreadOnce(PPU ppu)
        {
            // TODO
            // Minecraft needs this !

            Console.WriteLine($"[SysPPUThread] SysPPUThreadOnce");

            GuestPtr<uint> once_ctrl_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestVoidPtr ptr_to_pc = ppu.ReadArg<GuestVoidPtr>(ARG1);

            if (MemoryManager.Read<uint>(once_ctrl_ptr) == 0)
            {

                //uint pc = MemoryManager.Read<uint>(ptr_to_pc);
                //ReadOnlySpan<ulong> args = [0,0,0,0,0];

                PPUInterpreter.ExecuteFunction(ppu, ptr_to_pc, toc: true, []);

                MemoryManager.Write<uint>(once_ctrl_ptr, 1);        //SYS_PPU_THREAD_DONE_INIT
            }

            return CELL_OK;
        }


        public struct ThreadStackInfo : IPS3Struct
        {
            public BigEndian<uint> addr;
            public BigEndian<uint> size;
        }

        public static ulong SysPPUThreadGetStackInfo(PPU ppu)
        {
            Console.WriteLine($"[SysPPUThread] SysPPUThreadGetStackInfo");

            GuestPtr<ThreadStackInfo> info_ptr = ppu.ReadArg<GuestPtr<ThreadStackInfo>>(ARG0);

            unsafe
            {
                ThreadStackInfo* info = info_ptr.AsNativePtr();
                info->addr = ppu.Stack;
                info->size = ppu.StackSize;
            }

            return CELL_OK;
        }
    }
}
