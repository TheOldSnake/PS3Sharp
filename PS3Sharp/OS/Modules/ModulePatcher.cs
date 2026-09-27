using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.NID;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.ElfLoader;

namespace PS3Sharp.OS.Modules
{
    internal static class ModulePatcher
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct PRXParam : IPS3Struct
        {
            internal BigEndian<uint> size;
            internal BigEndian<uint> magic;
            internal BigEndian<uint> version;
            internal BigEndian<uint> pad0;
            internal BigEndian<uint> libentstart;
            internal BigEndian<uint> libentend;
            internal BigEndian<uint> libstubstart;
            internal BigEndian<uint> libstubend;
            internal BigEndian<ushort> ver;
            internal BigEndian<ushort> pad1;
            internal BigEndian<uint> pad2;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PRXStubHeader : IPS3Struct
        {
            internal byte s_size;
            internal byte s_unk0;
            internal BigEndian<ushort> s_version;
            internal BigEndian<ushort> s_unk1;
            internal BigEndian<ushort> s_imports;
            internal BigEndian<uint> s_unk2;
            internal BigEndian<uint> s_unk3;
            internal BigEndian<uint> s_modulename;
            internal BigEndian<uint> s_nid;
            internal BigEndian<uint> s_text;
            internal BigEndian<uint> s_unk4;
            internal BigEndian<uint> s_unk5;
            internal BigEndian<uint> s_unk6;
            internal BigEndian<uint> s_unk7;
        }

        internal static List<ModuleStubInfo> Stubs = [];
        internal const ushort ModuleCallSysID = 0x7C7C;

        internal static void HandlePRXImports(BinaryReader reader, ProgramHeader prxProgram)
        { // for main elf
            reader.BaseStream.Seek((long)(ulong)prxProgram.p_offset, SeekOrigin.Begin);
            PRXParam prx = ReadPRXParam(reader);
            uint sizeOfStubHeader = (uint)Unsafe.SizeOf<PRXStubHeader>();
            uint spanLength = (prx.libstubend - prx.libstubstart) / sizeOfStubHeader;

            unsafe
            {
                ReadOnlySpan<PRXStubHeader> moduleStubs = MemoryManager.CreateSpan<PRXStubHeader>(prx.libstubstart, (int)spanLength);
                ImportModules(moduleStubs);
            }
        }

        internal static unsafe void ImportModules(ReadOnlySpan<PRXStubHeader> moduleStubs)
        {
            int length = moduleStubs.Length;

            unsafe
            {
                for (int i = 0; i < length; i++)
                {
                    PRXStubHeader moduleStub = moduleStubs[i];
                    string moduleName = MemoryManager.ReadString(moduleStub.s_modulename);
                    Console.WriteLine($"Module Import: {moduleName}");

                    Span<BigEndian<uint>> nids = MemoryManager.CreateSpan<BigEndian<uint>>(moduleStub.s_nid, moduleStub.s_imports);
                    Span<BigEndian<uint>> stubCodeAddresses = MemoryManager.CreateSpan<BigEndian<uint>>(moduleStub.s_text, moduleStub.s_imports);
                    ImportFunctions(moduleName, nids, stubCodeAddresses);
                }
            }

            //Import the modules which we can LLE
            PRXManager.PRXManager.LoadPRXLibraries();
        }

        internal static unsafe void ImportFunctions(string moduleName, Span<BigEndian<uint>> nids, Span<BigEndian<uint>> stubCodeAddresses)
        {
            if (nids.Length != stubCodeAddresses.Length)
            {
                throw new Exception();
            }

            int length = nids.Length;

            unsafe
            {
                for (int i = 0; i < length; i++)
                {
                    uint nid = nids[i];
                    uint stubCodeAddress = stubCodeAddresses[i];

                    string functionName = NIDManager.NIDList.GetValueOrDefault(nid, "unknown_function");
                    PatchModuleCallHLE(moduleName, functionName, nid, stubCodeAddress);

                    Console.WriteLine("Main ELF Function Import: {0,-30} stub at 0x{1,8:X8}", functionName, stubCodeAddress);
                }
            }
        }

        internal static void PatchModuleCallHLE(string moduleName, string functionName, uint nid, uint stubCodeAddress)
        {
            ModuleStubInfo stubInfo = new(moduleName, functionName, nid, stubCodeAddress);
            stubInfo.PatchStartAddress = WriteHLEStub(stubCodeAddress);
            Stubs.Add(stubInfo);
        }

        private static uint WriteHLEStub(uint stubAddress)
        {
            //The module call does some small setup (like writing r2 to the stack) then does:
            //lwz r0,0x0(r12)
            //lwz r2,0x4(r12)
            //mtspr CTR,r0
            //bctr or bctrl 
            //if the jump is bctr, we need to add our blr

            //We need to run the setup code first, so we write our patch at the lwz that overwrites r0 or r2
            //this is because the caller might try to restore r2 and if the setup code did not run, it will read garbage/zeroes

            //Find lwz r2
            int count = 0;
            PPUInstruction instruction = new() { Raw = MemoryManager.Read<uint>(stubAddress) };
            while (true)
            {
                if (instruction.Op == Op.LWZ && instruction.Rt == 2)
                {
                    break;
                }

                stubAddress += 4;
                count++;
                instruction.Raw = MemoryManager.Read<uint>(stubAddress);

                if (count > 64)
                { //or any big number
                    throw new Exception($"Can't find lwz r2");
                }
            }

            //Check which type the branch is
            bool is_bctr = false;
            instruction.Raw = MemoryManager.Read<uint>(stubAddress + 8);
            if (instruction.Op == Op.G13 && instruction.G13 == G13.BCCTR)
            {
                is_bctr = (instruction.Raw & 1) == 0;
            }
            else
            {
                throw new Exception($"Can't find bctr/bctrl");
            }


            //Write the patch
            const uint li = 0x39600000 | ModuleCallSysID;    //li r11, ModuleCallSysID
            const uint sc = 0x44000002;                      //syscall
            const uint blr = 0x4e800020;                      //return
            const uint nop = 0x60000000;                      //nop

            MemoryManager.Write<uint>(stubAddress + 0, li);                 //instead of lwz r2
            MemoryManager.Write<uint>(stubAddress + 4, sc);                 //instead of mtspr CTR,r0
            MemoryManager.Write<uint>(stubAddress + 8, is_bctr ? blr : nop);  //instead of bctr / bctrl 

            //Return the address where the patch starts
            return stubAddress;
        }

        internal static ModuleStubInfo GetModuleInfo(uint stubAddress)
        { //takes the address of li r11, ModuleCallSysID
            for (int i = 0; i < Stubs.Count; i++)
            {
                if (Stubs[i].PatchStartAddress == stubAddress)
                {
                    return Stubs[i];
                }
            }

            throw new Exception($"Cannot find module at stub {stubAddress:X8}");
        }

        private static PRXParam ReadPRXParam(BinaryReader reader)
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<PRXParam>()];
            reader.ReadExactly(buffer);
            return MemoryMarshal.Read<PRXParam>(buffer);
        }

        internal static void PrintPRXParam(PRXParam p)
        {
            Console.WriteLine("PRXParam:");
            Console.WriteLine($"size         = 0x{p.size:X8}");
            Console.WriteLine($"magic        = 0x{p.magic:X8}");
            Console.WriteLine($"version      = 0x{p.version:X8}");
            Console.WriteLine($"pad0         = 0x{p.pad0:X8}");
            Console.WriteLine($"libentstart  = 0x{p.libentstart:X8}");
            Console.WriteLine($"libentend    = 0x{p.libentend:X8}");
            Console.WriteLine($"libstubstart = 0x{p.libstubstart:X8}");
            Console.WriteLine($"libstubend   = 0x{p.libstubend:X8}");
            Console.WriteLine($"ver          = 0x{p.ver:X4}");
            Console.WriteLine($"pad1         = 0x{p.pad1:X4}");
            Console.WriteLine($"pad2         = 0x{p.pad2:X8}");
        }
    }
}
