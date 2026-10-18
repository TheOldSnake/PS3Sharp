using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Memory.MemoryManager;

namespace PS3Sharp.OS.Modules
{
    internal class SysPrxForUser
    {
        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;

        internal SysPrxForUser(ThreadManager threadManager, MemoryManager memoryManager)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
        }

        internal ulong SysInitializeTLS(PPUThread ppu)
        {
            ulong thread_id = ppu.ReadArg<ulong>(ARG0);
            uint tls_seg_addr = ppu.ReadArg<uint>(ARG1);
            uint tls_seg_size = ppu.ReadArg<uint>(ARG2);
            uint tls_mem_size = ppu.ReadArg<uint>(ARG3);

            Console.WriteLine($"[SysPrxForUser] sys_initialize_tls thread_id = {thread_id:X8} tls_seg_addr = {tls_seg_addr:X8} tls_seg_size = {tls_seg_size:X8} tls_mem_size = {tls_mem_size:X8}");
            uint tlsAddress = ppu.ReadArg<uint>(13); // R13 is tls
            if (tlsAddress != 0)
            {
                return CELL_OK;
            }
            else
            {
                //In theory this should not happen because we initialize tls when we create the thread
                throw new Exception($"TLS was not initialized?");
            }
        }

        internal ulong SysProcessExit(PPUThread ppu)
        {
            int exitCode = ppu.ReadArg<int>(ARG0);
            ppu.StopThread();

            Console.WriteLine($"[SysPrxForUser] sys_process_exit - exit code = {exitCode} caller thread: [{ppu.ThreadName}]");

            if (exitCode > 0)
            {
                Console.WriteLine($"[SysPrxForUser] sys_process_exit call stack:");
                PPUInterpreter.PrintCallstack(ppu);
            }

            ppu.SetExitCode(exitCode);
            _threadManager.RequestShutdown();

            return CELL_OK;
        }

        internal ulong SysSpinlockInitialize(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_spinlock_initialize");
            return CELL_OK;
        }

        internal ulong SysSpinlockUnlock(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_spinlock_unlock");
            return CELL_OK;
        }

        internal ulong SysSpinlockLock(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_spinlock_lock");
            return CELL_OK;
        }

        internal ulong SysMmapperAllocateMemory(PPUThread ppu)
        {
            //Allocates physical memory, and returns a handle

            ulong size = ppu.ReadArg<ulong>(ARG0);
            ulong flags = ppu.ReadArg<ulong>(ARG1);
            GuestVoidPtr addressPtr = ppu.ReadArg<GuestVoidPtr>(ARG2);

            if (size > 0)
            {
                MemoryBlock block = _memoryManager.AllocatePhysicalMemory((uint)size);

                //Write the handle at the alloc_addr*
                _memoryManager.Write<BigEndian<uint>>(addressPtr.Address, (uint)block.Handle);
                Console.WriteLine($"[SysPrxForUser] sys_mmapper_allocate_memory handle = {block.Handle}");
            }
            else
            {
                Console.WriteLine($"[SysPrxForUser] sys_mmapper_allocate_memory size == 0");
            }

            return CELL_OK;
        }

        internal ulong SysMmapperFreeMemory(PPUThread ppu)
        {
            //Fress memory by handle
            uint handle = ppu.ReadArg<uint>(ARG0);

            //Not sure if handle 0 is NULL?
            if (handle > 0)
            {
                _memoryManager.FreeByHandle(handle);
            }

            Console.WriteLine($"[SysPrxForUser] sys_mmapper_free_memory handle = {handle}");
            return CELL_OK;
        }

        internal ulong SysMmapperUnmapMemory(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_mmapper_unmap_memory");
            return CELL_OK;
        }

        internal ulong _Sys_Process_atexitspawn(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] _sys_process_atexitspawn");
            return CELL_OK;
        }

        internal ulong _Sys_Process_at_Exitspawn(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] _sys_process_at_Exitspawn");
            return CELL_OK;
        }

        internal ulong Sys_game_process_exitspawn2(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_game_process_exitspawn2");
            return CELL_OK;
        }

        internal ulong Sys_Prx_Exitspawn_With_Level(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_prx_exitspawn_with_level");
            return CELL_OK;
        }

        internal ulong Sys_SPU_Image_Import(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[SysPrxForUser] sys_spu_image_import");
            return CELL_OK;
        }

        internal ulong _Sys_Memset(PPUThread ppu)
        {
            GuestVoidPtr dest = ppu.ReadArg<GuestVoidPtr>(ARG0);
            int ch = ppu.ReadArg<int>(ARG1);
            int size = ppu.ReadArg<int>(ARG2);

            _memoryManager.ClearMemory(dest, size, (byte)ch);

            return dest;
        }

        internal ulong _Sys_Strncpy(PPUThread ppu)
        {
            uint dest = ppu.ReadArg<uint>(ARG0);
            uint src = ppu.ReadArg<uint>(ARG1);
            uint length = ppu.ReadArg<uint>(ARG2);

            if (dest == 0 || src == 0)
                return 0;

            uint originalDest = dest;

            uint i;
            for (i = 0; i < length; i++)
            {
                byte data = _memoryManager.Read<byte>(src);
                src++;

                _memoryManager.Write(dest, data);
                dest++;

                if (data == 0)
                {
                    i++;
                    break;
                }
            }

            while (i < length)
            {
                _memoryManager.Write(dest, 0);
                dest++;
                i++;
            }

            return originalDest;
        }
    }
}
