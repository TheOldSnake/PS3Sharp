using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal struct ModuleStubInfo
    {
        //This is not one of the PS3 structs, I made it just to make identifying/patching the module function easier
        internal readonly string ModuleName;
        internal readonly string FunctionName;
        internal readonly uint NID;
        internal readonly uint StubAddress; //The address of the beginning of the block
        internal uint PatchStartAddress;    //The address of the patched code

        internal ModuleStubInfo(string moduleName, string functionName, uint nid, uint stubAddress)
        {
            ModuleName = moduleName;
            FunctionName = functionName;
            NID = nid;
            StubAddress = stubAddress;
        }
    }

    internal static partial class ModuleHandler
    {
        internal static void HandleModuleCall(PPU ppu)
        {
            uint stubAddress = ppu.GetPC() - 8; //the instruction before syscall
            ModuleStubInfo info = ModulePatcher.GetModuleInfo(stubAddress);

            // Check if function is imported

            if (PRXManager.PRXManager.ExportedFunctions.TryGetValue(info.NID, out uint ptr))
            {
                uint pc = MemoryManager.Read<uint>(ptr);
                uint arg0 = ppu.ReadArg<uint>(ARG0);
                uint arg1 = ppu.ReadArg<uint>(ARG1);
                uint arg2 = ppu.ReadArg<uint>(ARG2);
                uint lr = ppu.GetLR();

                // Console.WriteLine($"[{ppu.ThreadName}] LLE function: {info.FunctionName} arg0 = 0x{arg0:X8} arg1 = 0x{arg1:X8} arg2 = 0x{arg2:X8} lr = 0x{lr:X8} pc: 0x{pc:X8}");

                ulong returnedValue = PPUInterpreter.ExecuteFunction(ppu, ptr, toc: true, []);

                // Console.WriteLine($"[{ppu.ThreadName}] LLE function: {info.FunctionName} returned: 0x{returnedValue:X8}");

                return;
            }

            //if (!info.FunctionName.Contains("lwmutex")) {
            //    Console.WriteLine($"[{ppu.ThreadName}] HLE module function: {info.FunctionName} nid = 0x{info.NID:X8} arg0 = 0x{ppu.ReadArg<uint>(ARG0):X8} LR = 0x{ppu.GetLR():X8}");
            //}

            ulong result;
            if (_handlers.TryGetValue(info.NID, out Func<PPU, ulong>? handler))
            {
                result = handler(ppu);
            }
            else
            {
                result = ModuleStub(info, ppu);
            }

            ppu.Return(result);
        }

        internal static ulong ModuleStub(ModuleStubInfo info, PPU ppu, ulong ret = CELL_OK)
        {
            // Console.WriteLine($"[{ppu.ThreadName}] unimplemented {info.ModuleName} -> {info.FunctionName} lr = {ppu.GetLR():X8}");
            if (info.FunctionName.Contains("cellSpurs"))
            {
                return ret;
            }

            ///throw new NotImplementedException();
            return ret;
        }

        internal static ulong FunctionStub(PPU ppu)
        {
            return CELL_OK;
        }
    }
}
