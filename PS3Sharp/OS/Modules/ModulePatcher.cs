using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Elf;
using PS3Sharp.Memory;
using PS3Sharp.OS.NID;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Elf.ElfDefinitions;
using static PS3Sharp.Elf.ElfLoader;

namespace PS3Sharp.OS.Modules
{
    internal class ModulePatcher
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct PRXParam : IPS3Struct
        {
            internal BigEndian<uint> Size;
            internal BigEndian<uint> Magic;
            internal BigEndian<uint> Version;
            internal BigEndian<uint> Pad0;
            internal BigEndian<uint> LibEntStart;
            internal BigEndian<uint> LibEntEnd;
            internal BigEndian<uint> LibStubStart;
            internal BigEndian<uint> LibsSubEnd;
            internal BigEndian<ushort> Ver;
            internal BigEndian<ushort> Pad1;
            internal BigEndian<uint> Pad2;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PRXStubHeader : IPS3Struct
        {
            internal byte SSize;
            internal byte SUnk0;
            internal BigEndian<ushort> SVersion;
            internal BigEndian<ushort> SUnk1;
            internal BigEndian<ushort> SImports;
            internal BigEndian<uint> SUnk2;
            internal BigEndian<uint> SUnk3;
            internal BigEndian<uint> SModuleName;
            internal BigEndian<uint> SNid;
            internal BigEndian<uint> SText;
            internal BigEndian<uint> SUnk4;
            internal BigEndian<uint> SUnk5;
            internal BigEndian<uint> SUnk6;
            internal BigEndian<uint> SUnk7;
        }

        internal const ushort ModuleCallSysId = 0x7C7C;

        private readonly List<ModuleStubInfo> Stubs = [];

        // For main elf
        internal void HandleMainElfImports(BinaryReader reader, ElfHeader64 mainHeader, MemoryManager memoryManager, ElfLoader elfLoader)
        {
            if (TryFindProgram(reader, ref mainHeader, PrxParam, out ProgramHeader64 prxProgram))
            {
                ulong offs = prxProgram.p_offset;
                PRXParam prx = ReadElfStruct<PRXParam>(reader, (long)offs);

                uint sizeOfStubHeader = (uint)Unsafe.SizeOf<PRXStubHeader>();
                uint spanLength = (prx.LibsSubEnd - prx.LibStubStart) / sizeOfStubHeader;

                ReadOnlySpan<PRXStubHeader> moduleStubs = memoryManager.ReadArray<PRXStubHeader>(prx.LibStubStart, (int)spanLength);
                ImportModules(moduleStubs, memoryManager);
            }
            else
            {
                Console.WriteLine($"[ModulePatcher] Can't find PRX_PARAM program");
            }
        }

        internal void ImportModules(ReadOnlySpan<PRXStubHeader> moduleStubs, MemoryManager memoryManager)
        {
            int length = moduleStubs.Length;

            for (int i = 0; i < length; i++)
            {
                PRXStubHeader moduleStub = moduleStubs[i];
                string moduleName = memoryManager.ReadString(moduleStub.SModuleName);
                Console.WriteLine($"Module Import: {moduleName}");

                Span<BigEndian<uint>> nids = memoryManager.ReadArray<BigEndian<uint>>(moduleStub.SNid, moduleStub.SImports);
                Span<BigEndian<uint>> stubCodeAddresses = memoryManager.ReadArray<BigEndian<uint>>(moduleStub.SText, moduleStub.SImports);
                ImportFunctions(moduleName, nids, stubCodeAddresses, memoryManager);
            }
        }

        internal void ImportFunctions(string moduleName, Span<BigEndian<uint>> nids, Span<BigEndian<uint>> stubCodeAddresses, MemoryManager memoryManager)
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
                    PatchModuleCallHLE(moduleName, functionName, nid, stubCodeAddress, memoryManager);

                    Console.WriteLine("Main ELF Function Import: {0,-30} stub at 0x{1,8:X8}", functionName, stubCodeAddress);
                }
            }
        }

        internal void PatchModuleCallHLE(string moduleName, string functionName, uint nid, uint stubCodeAddress, MemoryManager memoryManager)
        {
            ModuleStubInfo stubInfo = new(moduleName, functionName, nid, stubCodeAddress);
            stubInfo.PatchStartAddress = WriteHLEStub(stubCodeAddress, memoryManager);
            Stubs.Add(stubInfo);
        }

        private uint WriteHLEStub(uint stubAddress, MemoryManager memoryManager)
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
            PPUInstruction instruction = new() { Raw = memoryManager.Read<BigEndian<uint>>(stubAddress) };
            while (true)
            {
                if (instruction.Op == Op.LWZ && instruction.Rt == 2)
                {
                    break;
                }

                stubAddress += 4;
                count++;
                instruction.Raw = memoryManager.Read<BigEndian<uint>>(stubAddress);

                if (count > 64)
                {
                    throw new Exception($"Can't find lwz r2");
                }
            }

            //Check which type the branch is
            bool isBctr = false;
            instruction.Raw = memoryManager.Read<BigEndian<uint>>(stubAddress + 8);
            if (instruction.Op == Op.G13 && instruction.G13 == G13.BCCTR)
            {
                isBctr = (instruction.Raw & 1) == 0;
            }
            else
            {
                throw new Exception($"Can't find bctr/bctrl");
            }

            //Write the patch
            const uint Li = 0x39600000 | ModuleCallSysId;    //li r11, ModuleCallSysID
            const uint Sc = 0x44000002;                      //syscall
            const uint Blr = 0x4e800020;                     //return
            const uint Nop = 0x60000000;                     //nop

            memoryManager.Write<BigEndian<uint>>(stubAddress + 0, Li);                 //instead of lwz r2
            memoryManager.Write<BigEndian<uint>>(stubAddress + 4, Sc);                 //instead of mtspr CTR,r0
            memoryManager.Write<BigEndian<uint>>(stubAddress + 8, isBctr ? Blr : Nop);  //instead of bctr / bctrl 

            //Return the address where the patch starts
            return stubAddress;
        }

        internal ModuleStubInfo GetModuleInfo(uint stubAddress)
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

        private PRXParam ReadPRXParam(BinaryReader reader)
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<PRXParam>()];
            reader.ReadExactly(buffer);
            return MemoryMarshal.Read<PRXParam>(buffer);
        }

        internal void PrintPRXParam(PRXParam p)
        {
            Console.WriteLine("PRXParam:");
            Console.WriteLine($"size         = 0x{p.Size:X8}");
            Console.WriteLine($"magic        = 0x{p.Magic:X8}");
            Console.WriteLine($"version      = 0x{p.Version:X8}");
            Console.WriteLine($"pad0         = 0x{p.Pad0:X8}");
            Console.WriteLine($"libentstart  = 0x{p.LibEntStart:X8}");
            Console.WriteLine($"libentend    = 0x{p.LibEntEnd:X8}");
            Console.WriteLine($"libstubstart = 0x{p.LibStubStart:X8}");
            Console.WriteLine($"libstubend   = 0x{p.LibsSubEnd:X8}");
            Console.WriteLine($"ver          = 0x{p.Ver:X4}");
            Console.WriteLine($"pad1         = 0x{p.Pad1:X4}");
            Console.WriteLine($"pad2         = 0x{p.Pad2:X8}");
        }
    }
}
