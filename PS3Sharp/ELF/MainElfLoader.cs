using PS3Sharp.Memory;
using static PS3Sharp.Elf.ElfDefinitions;

namespace PS3Sharp.Elf
{
    internal sealed class MainElfLoader : ElfLoader
    {
        internal ElfHeader64 LoadMainElf(BinaryReader reader, MemoryManager memoryManager)
        {
            // Read Main Header 
            ElfHeader64 mainHeader = ReadElfStruct<ElfHeader64>(reader, 0);

            if (!mainHeader.IsValid)
            {
                throw new Exception("Invalid ELF");
            }

            LoadSegments(reader, ref mainHeader, memoryManager);
            HandleTLSSegment(reader, ref mainHeader, memoryManager);

            Console.WriteLine($"[ELFLoader] ELF loaded!");
            return mainHeader;
        }

        private static void LoadSegments(BinaryReader reader, ref ElfHeader64 mainHeader, MemoryManager memoryManager)
        {
            // Load the loadable segments
            ReadOnlySpan<ProgramHeader64> programHeaders = GetProgramHeadersArray(reader, ref mainHeader);

            for (int i = 0; i < programHeaders.Length; i++)
            {
                ProgramHeader64 programHeader = programHeaders[i];

                if (programHeader.IsLoadable)
                {
                    LoadSegment(reader, ref programHeader, memoryManager);
                }
            }
        }

        private static uint LoadSegment(BinaryReader reader, ref ProgramHeader64 programHeader, MemoryManager memoryManager)
        {
            string pt_name = programHeader.TypeName;
            uint p_memsz = (uint)(ulong)programHeader.p_memsz;
            uint p_vaddr = (uint)(ulong)programHeader.p_vaddr;

            // Load segment
            byte[] segmentData = GetSegmentData(reader, ref programHeader);
            MemoryManager.MemoryBlock block = memoryManager.Allocator.AllocateVirtualAddress(p_vaddr, p_memsz);
            uint dest = block.VirtualAddress;

            memoryManager.CopyBufferToVirtualMemory(segmentData, dest);

            if (segmentData.Length < p_memsz)
            {
                int zeroesCount = (int)p_memsz - segmentData.Length;
                memoryManager.ClearMemory(dest + (uint)segmentData.Length, zeroesCount);
            }

            Console.WriteLine(
                $"Loaded {pt_name,-8} segment: " +
                $"Offset = 0x{programHeader.p_offset:X8}, " +
                $"VirtAddr = 0x{programHeader.p_vaddr:X8}, " +
                $"FileSize = 0x{programHeader.p_filesz:X8}, " +
                $"MemSize = 0x{programHeader.p_memsz:X8}, " +
                $"Flags = 0x{programHeader.p_flags:X}");

            return dest;
        }

        private static void HandleTLSSegment(BinaryReader reader, ref ElfHeader64 header, MemoryManager memoryManager)
        {
            if (TryFindProgram(reader, ref header, PtTls, out ProgramHeader64 tlsImage))
            {
                ulong tlsvaddr = tlsImage.p_vaddr;
                ulong tlsSize = tlsImage.p_memsz;

                // Load directly
                LoadSegment(reader, ref tlsImage, memoryManager);
                memoryManager.SetTLS((uint)tlsvaddr, (uint)tlsSize);

                Console.WriteLine("[MainElfLoader] Found TLS");
            }
            else
            {
                Console.WriteLine($"[MainElfLoader] Could not find PT_TLS"); // Is it always provided?
            }
        }
    }
}
