using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysPPUThread
    {
        private readonly string[] IgnoredThreads = [
           // Minecraft -- these PPU threads expect the SPU to be functional, will crash otherwise 
           //"MinecraftSpursHdlr0",
           //"_libjpgdecSpursHdlr0",
           //"_libpngdecSpursHdlr0", 
        ];

        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;

        internal SysPPUThread(ThreadManager threadManager, MemoryManager memoryManager)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
        }

        internal ulong SysPPUThreadGetID(PPUThread ppu)
        {
            GuestPtr<BigEndian<ulong>> id_ptr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg0);
            _memoryManager.Write<BigEndian<ulong>>(id_ptr, ppu.ThreadID);

            //Console.WriteLine($"[sys_ppu_thread] sys_ppu_thread_get_id: 0x{ppu.ThreadID:X8}");
            return CellOk;
        }

        internal ulong SysPPUThreadGetPriority(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<int>> prio_ptr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg1);

            //Console.WriteLine($"[sys_ppu_thread] sys_ppu_thread_get_priority ID = 0x{id:X8}");
            PPUThread? thread = ppu.ThreadManager.GetPPUThreadByID(id);
            _memoryManager.Write<BigEndian<int>>(prio_ptr, thread.ThreadPriority);
            return CellOk;
        }

        internal ulong SysPPUThreadSetPriority(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            int priority = ppu.ReadArg<int>(Arg1);

            //Console.WriteLine($"[sys_ppu_thread] sys_ppu_thread_set_priority ID = 0x{id:X8}");
            PPUThread? thread = ppu.ThreadManager.GetPPUThreadByID(id);
            thread?.SetThreadPriority(priority);

            return CellOk;
        }

        internal ulong SysPPUThreadCreate(PPUThread ppu)
        {
            //int sys_ppu_thread_create(sys_ppu_thread_t *thread_id, & void (*entry)(uint64_t), uint64_t arg, 0, int prio, size_t stacksize, uint64_t flags, const char *threadname); 

            //Read arguments
            GuestPtr<BigEndian<ulong>> id_ptr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg0);
            GuestVoidPtr entry = ppu.ReadArg<GuestVoidPtr>(Arg1);
            ulong arg = ppu.ReadArg<ulong>(Arg2);
            int prio = ppu.ReadArg<int>(Arg3);   //todo
            uint stacksize = ppu.ReadArg<uint>(Arg4);
            ulong flags = ppu.ReadArg<ulong>(Arg5); //todo
            GuestVoidPtr name_ptr = ppu.ReadArg<GuestVoidPtr>(Arg6);
            string name = name_ptr.IsNullPtr ? "Unnamed-PPU" : _memoryManager.ReadString(name_ptr);

            // Super hack
            // Ideally we should not do this
            if (IgnoredThreads.Contains(name))
            {
                return CellOk;
            }

            //Create a new PPU thread
            PPUThread ppu_thread = _threadManager.CreatePPUThread(name, entry, stacksize, arg, ppu.SyscallDispatcher);
            ppu_thread.StartThread();

            _memoryManager.Write<BigEndian<ulong>>(id_ptr, ppu_thread.ThreadID);
            Console.WriteLine($"[SysPPUThread] sys_ppu_thread_create: {name} thread_id = 0x{ppu_thread.ThreadID:X8} entry_pc = {entry:X8}");

            return CellOk;
        }

        internal ulong SysPPUThreadExit(PPUThread ppu)
        {
            int exit_code = ppu.ReadArg<int>(Arg0);
            Console.WriteLine($"[SysPPUThread] sys_ppu_thread_exit: {ppu.ThreadName} thread_id = 0x{ppu.ThreadID:X8} exit_code = {exit_code}");
            ppu.StopThread();
            ppu.SetExitCode(exit_code);

            // if the main ppu thread exits we shutdown the emulator
            if (ppu.IsMainThread)
            {
                _threadManager.RequestShutdown();
            }

            return CellOk;
        }

        internal ulong SysPPUThreadYield(PPUThread ppu)
        {
            Thread.Yield();
            return CellOk;
        }

        internal ulong SysPPUThreadJoin(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<ulong>> exit_code_ptr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg1);

            int exit_code = ppu.ThreadManager.JoinThreadByID(id);
            _memoryManager.Write<BigEndian<ulong>>(exit_code_ptr, (ulong)exit_code);

            Console.WriteLine($"[SysPPUThread] sys_ppu_thread_join: caller = {ppu.ThreadName} joins: 0x{id:X8}");
            return CellOk;
        }

        internal ulong SysPPUThreadOnce(PPUThread ppu)
        {
            // TODO
            // Minecraft needs this !

            Console.WriteLine($"[SysPPUThread] SysPPUThreadOnce");

            GuestPtr<BigEndian<uint>> once_ctrl_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestVoidPtr ptr_to_pc = ppu.ReadArg<GuestVoidPtr>(Arg1);

            if (_memoryManager.Read<BigEndian<uint>>(once_ctrl_ptr) == 0)
            {

                //uint pc = MemoryManager.Read<uint>(ptr_to_pc);
                //ReadOnlySpan<ulong> args = [0,0,0,0,0];

                PPUInterpreter.ExecuteFunction(ppu, ptr_to_pc, toc: true, []);

                _memoryManager.Write<BigEndian<uint>>(once_ctrl_ptr, 1);        //SYS_PPU_THREAD_DONE_INIT
            }

            return CellOk;
        }


        internal struct ThreadStackInfo : IPS3Struct
        {
            internal BigEndian<uint> addr;
            internal BigEndian<uint> size;
        }

        internal ulong SysPPUThreadGetStackInfo(PPUThread ppu)
        {
            Console.WriteLine($"[SysPPUThread] SysPPUThreadGetStackInfo");

            GuestPtr<ThreadStackInfo> info_ptr = ppu.ReadArg<GuestPtr<ThreadStackInfo>>(Arg0);

            ThreadStackInfo info = new()
            {
                addr = ppu.Stack,
                size = ppu.StackSize
            };

            _memoryManager.Write(info_ptr, info);

            return CellOk;
        }
    }
}
