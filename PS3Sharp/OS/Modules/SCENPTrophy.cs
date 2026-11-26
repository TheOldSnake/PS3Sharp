using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class SCENPTrophy
    {
        private readonly MemoryManager _memoryManager;

        internal SCENPTrophy(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SCENpTrophyInit(PPUThread ppu)
        {
            Console.WriteLine($"[SCENPTrophy] sceNpTrophyInit");
            return CellOk;
        }

        internal ulong SCENpTrophyCreateContext(PPUThread ppu)
        {
            GuestVoidPtr context_ptr = ppu.ReadArg<GuestVoidPtr>(Arg0);
            GuestVoidPtr command_id_ptr = ppu.ReadArg<GuestVoidPtr>(Arg1);
            GuestVoidPtr command_sign_ptr = ppu.ReadArg<GuestVoidPtr>(Arg2);
            ulong options = ppu.ReadArg<ulong>(Arg3);

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyCreateContext context_ptr = 0x{context_ptr:X8}");

            uint handle = _memoryManager.GetHandle();
            _memoryManager.Write<BigEndian<uint>>(context_ptr, handle);

            return CellOk;
        }

        internal ulong SCENpTrophyRegisterContext(PPUThread ppu)
        {
            // I am not sure how this works, but this code is baes on RPCS3 and chonkystation 

            uint ctx = ppu.ReadArg<uint>(Arg0);
            uint handle = ppu.ReadArg<uint>(Arg1);
            GuestVoidPtr callback = ppu.ReadArg<GuestVoidPtr>(Arg2);
            uint arg = ppu.ReadArg<GuestVoidPtr>(Arg3);
            uint options = ppu.ReadArg<GuestVoidPtr>(Arg4);

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
            return CellOk;
        }

        internal ulong SCENpTrophyCreateHandle(PPUThread ppu)
        {
            GuestVoidPtr handle_ptr = ppu.ReadArg<GuestVoidPtr>(Arg0);

            uint handle = _memoryManager.GetHandle();
            _memoryManager.Write<BigEndian<uint>>(handle_ptr, handle);

            Console.WriteLine($"[SCENPTrophy] SCENpTrophyCreateHandle handle_ptr = 0x{handle_ptr:X8} handle = 0x{handle:X8}");
            return CellOk;
        }

        internal ulong SCENpTrophyGetRequiredDiskSpace(PPUThread ppu)
        {
            GuestVoidPtr require_space_ptr = ppu.ReadArg<GuestVoidPtr>(Arg2);
            _memoryManager.Write<BigEndian<uint>>(require_space_ptr, 0);
            Console.WriteLine($"[SCENPTrophy] SCENpTrophyGetRequiredDiskSpace");
            return CellOk;
        }

        internal ulong SCENpTrophyGetGameInfo(PPUThread ppu)
        {
            Console.WriteLine($"[SCENPTrophy] sceNpTrophyGetGameInfo");
            return CellOk;
        }

        internal ulong SCENpTrophyGetTrophyUnlockState(PPUThread ppu)
        {
            //This can be stubbed more

            uint ctx = ppu.ReadArg<uint>(Arg0);
            uint handle = ppu.ReadArg<uint>(Arg1);
            GuestPtr<byte> flags_ptr = ppu.ReadArg<GuestPtr<byte>>(Arg2);
            GuestPtr<BigEndian<uint>> count_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg3);

            _memoryManager.Write<byte>(flags_ptr, 0x80);
            _memoryManager.Write<BigEndian<uint>>(count_ptr, 32);

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyGetTrophyUnlockState");
            return CellOk;
        }

        internal ulong SCENpTrophyUnlockTrophy(PPUThread ppu)
        {
            uint ctx = ppu.ReadArg<uint>(Arg0);
            uint handle = ppu.ReadArg<uint>(Arg1);
            int trophyId = ppu.ReadArg<int>(Arg2);
            GuestPtr<BigEndian<int>> platinumId = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg3);

            Console.WriteLine($"[SCENPTrophy] sceNpTrophyUnlockTrophy - ulocked trophy id: {trophyId}");
            return CellOk;
        }
    }
}
