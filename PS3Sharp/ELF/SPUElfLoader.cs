using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using System.Buffers;
using static PS3Sharp.Elf.ElfDefinitions;

namespace PS3Sharp.Elf
{
    internal class SPUElfLoader : ElfLoader
    {
        internal (ElfHeader32 header, List<SysSPU.SysSPUSegment> segments) LoadSPUElf(GuestVoidPtr elfPtr, MemoryManager memoryManager)
        {
            // Note: SPU elf is 32-bit BE

            // Read Main Header 
            ElfHeader32 mainHeader = memoryManager.Read<ElfHeader32>(elfPtr);

            if (!mainHeader.IsValid)
            {
                throw new Exception("Invalid SPU ELF");
            }

            List<SysSPU.SysSPUSegment> segments = LoadSegments(elfPtr, memoryManager, ref mainHeader);

            return (mainHeader, segments);
        }

        private static List<SysSPU.SysSPUSegment> LoadSegments(GuestVoidPtr elfPtr, MemoryManager memoryManager, ref ElfHeader32 mainHeader)
        {
            // Load the loadable segments
            List<SysSPU.SysSPUSegment> segments = [];
            ReadOnlySpan<ProgramHeader32> programHeaders = GetProgramHeadersArray(memoryManager, elfPtr, ref mainHeader);

            for (int i = 0; i < programHeaders.Length; i++)
            {
                ProgramHeader32 programHeader = programHeaders[i];

                if (programHeader.IsLoadable)
                {
                    LoadSegment(elfPtr, segments, ref programHeader, memoryManager);
                }
            }

            return segments;
        }

        private static void LoadSegment(uint elfPtr, List<SysSPU.SysSPUSegment> segments, ref ProgramHeader32 programHeader, MemoryManager memoryManager)
        {
            uint offset = programHeader.p_offset;   // offset with respect to elf image
            uint fileSize = programHeader.p_filesz;
            uint memSize = programHeader.p_memsz;
            uint dest = programHeader.p_vaddr;
            uint srcAddress = elfPtr + offset;

            if (!memoryManager.IsVirtualAddressAllocated(dest))
            {
                memoryManager.AllocateVirtualAddress(dest, memSize);
            }

            // Copy segment to destination
            memoryManager.CopyVirtualMemory(srcAddress, dest, (int)fileSize);

            // Create SysSPUSegment
            SysSPU.SysSPUSegment segment = CreateSPUSegment(elfPtr, ref programHeader, isFill: false);
            segments.Add(segment);

            if (memSize > fileSize)
            {
                uint clearDest = dest + fileSize;
                uint clearBytes = memSize - fileSize;
                memoryManager.ClearMemory(clearDest, (int)clearBytes);

                // Create SysSPUSegment for the filler
                SysSPU.SysSPUSegment fillSegment = CreateSPUSegment(elfPtr, ref programHeader, isFill: true);
                segments.Add(fillSegment);
            }
        }

        private static SysSPU.SysSPUSegment CreateSPUSegment(uint elfPtr, ref ProgramHeader32 programHeader, bool isFill)
        {
            uint offset = programHeader.p_offset;   // With respect to elf image
            uint dataAddress = elfPtr + offset;

            uint dest = programHeader.p_vaddr;
            uint fileSize = programHeader.p_filesz;
            uint memSize = programHeader.p_memsz;

            int type;
            uint ls;
            uint src;
            int size;

            if (isFill)
            {
                type = SysSPU.SysSpuSegmentTypeFill;
                ls = dest + fileSize;
                src = 0;
                size = (int)(memSize - fileSize);
            }
            else
            {
                type = SysSPU.SysSpuSegmentTypeCopy;
                ls = dest;
                src = dataAddress;
                size = (int)memSize;
            }

            SysSPU.SysSPUSegment segment = new()
            {
                Type = type,
                LSAddress = ls,
                Src = src,
                Size = size
            };

            return segment;
        }

        private static ReadOnlySpan<ProgramHeader32> GetProgramHeadersArray(MemoryManager memory, uint elfBase, ref ElfHeader32 header)
        {
            // Get the array of ELF program headers from memory
            ushort numberOfProgramHeaders = header.e_phnum; // how many program headers
            ushort programHeaderSize = header.e_phentsize;  // size of a single header
            uint programHeadersOffset = header.e_phoff;     // offset of the array
            return memory.ReadArray<ProgramHeader32>(elfBase + programHeadersOffset, numberOfProgramHeaders);
        }
    }
}