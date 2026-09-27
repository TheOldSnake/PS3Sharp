using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class SCENPTrophy
    {
        public static ulong SCENpTrophyInit(PPU ppu)
        {
            Console.WriteLine($"[SCENPTrophy] sceNpTrophyInit");
            return CELL_OK;
        }

        public static ulong SCENpTrophyCreateContext(PPU ppu)
        {
            GuestVoidPtr context_ptr = ppu.ReadArg<GuestVoidPtr>(ARG0);
            GuestVoidPtr command_id_ptr = ppu.ReadArg<GuestVoidPtr>(ARG1);
            GuestVoidPtr command_sign_ptr = ppu.ReadArg<GuestVoidPtr>(ARG2);
            ulong options = ppu.ReadArg<ulong>(ARG3);

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyCreateContext context_ptr = 0x{context_ptr:X8}");

            uint handle = MemoryManager.GetHandle();
            MemoryManager.Write<uint>(context_ptr, handle);

            return CELL_OK;
        }

        public static ulong SCENpTrophyRegisterContext(PPU ppu)
        {
            // I am not sure how this works, but this code is baes on RPCS3 and chonkystation 

            uint ctx = ppu.ReadArg<uint>(ARG0);
            uint handle = ppu.ReadArg<uint>(ARG1);
            GuestVoidPtr callback = ppu.ReadArg<GuestVoidPtr>(ARG2);
            uint arg = ppu.ReadArg<GuestVoidPtr>(ARG3);
            uint options = ppu.ReadArg<GuestVoidPtr>(ARG4);

            Span<ulong> args = stackalloc ulong[5];
            //uint callback_pc = MemoryManager.Read<uint>(callback);

            //R3 and R7 don't change
            args[0] = ctx;
            args[4] = arg;

            // We need to call this address with different arguments, different number of times

            RunCallback(1, 3, 0, args, callback);    // Install the trophy data?
            RunCallback(4, 5, 3, args, callback);    // SCE_NP_TROPHY_STATUS_PROCESSING_SETUP
            RunCallback(32, 6, 31, args, callback);    // SCE_NP_TROPHY_STATUS_PROCESSING_PROGRESS
            RunCallback(6, 7, 5, args, callback);    // SCE_NP_TROPHY_STATUS_PROCESSING_FINALIZE
            RunCallback(1, 8, 0, args, callback);    // SCE_NP_TROPHY_STATUS_PROCESSING_COMPLETE

            void RunCallback(ulong iterations, uint arg1, uint arg3, Span<ulong> args, uint callback)
            {
                args[1] = arg1; //R4
                args[3] = arg3; //R6

                for (ulong i = 0; i < iterations; i++)
                {
                    args[2] = i; //R5 = i
                    PPUInterpreter.ExecuteFunction(ppu, callback, toc: false, args); // toc needed ?
                }
            }

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyRegisterContext");
            return CELL_OK;
        }

        public static ulong SCENpTrophyCreateHandle(PPU ppu)
        {
            GuestVoidPtr handle_ptr = ppu.ReadArg<GuestVoidPtr>(ARG0);

            uint handle = MemoryManager.GetHandle();
            MemoryManager.Write<uint>(handle_ptr, handle);

            Console.WriteLine($"[SCENPTrophy] SCENpTrophyCreateHandle handle_ptr = 0x{handle_ptr:X8} handle = 0x{handle:X8}");
            return CELL_OK;
        }

        public static ulong SCENpTrophyGetRequiredDiskSpace(PPU ppu)
        {
            GuestVoidPtr require_space_ptr = ppu.ReadArg<GuestVoidPtr>(ARG2);
            MemoryManager.Write<uint>(require_space_ptr, 0);
            Console.WriteLine($"[SCENPTrophy] SCENpTrophyGetRequiredDiskSpace");
            return CELL_OK;
        }

        public static ulong SCENpTrophyGetGameInfo(PPU ppu)
        {
            Console.WriteLine($"[SCENPTrophy] sceNpTrophyGetGameInfo");
            return CELL_OK;
        }

        public static ulong SCENpTrophyGetTrophyUnlockState(PPU ppu)
        {
            //This can be stubbed more

            uint ctx = ppu.ReadArg<uint>(ARG0);
            uint handle = ppu.ReadArg<uint>(ARG1);
            GuestPtr<byte> flags_ptr = ppu.ReadArg<GuestPtr<byte>>(ARG2);
            GuestPtr<uint> count_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG3);

            MemoryManager.Write<byte>(flags_ptr, 0x80);
            MemoryManager.Write<uint>(count_ptr, 32);

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyGetTrophyUnlockState");
            return CELL_OK;
        }

        public static ulong SCENpTrophyUnlockTrophy(PPU ppu)
        {
            uint ctx = ppu.ReadArg<uint>(ARG0);
            uint handle = ppu.ReadArg<uint>(ARG1);
            int trophyId = ppu.ReadArg<int>(ARG2);
            GuestPtr<int> platinumId = ppu.ReadArg<GuestPtr<int>>(ARG3);

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyUnlockTrophy - ulocked trophy id: {trophyId}");
            return CELL_OK;
        }
    }
}
