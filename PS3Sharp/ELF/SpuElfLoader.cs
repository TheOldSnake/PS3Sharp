using PS3Sharp.Common;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using static PS3Sharp.Elf.ElfDefinitions;

namespace PS3Sharp.Elf
{
    internal class SpuElfLoader : ElfLoader
    {
        internal (ElfHeader32 header, List<SysSpu.SysSpuSegment> segments) GetSpuElfInfo(string elfGuestPath, MemoryManager memoryManager)
        {
            // Read the elf image from the file
            string elfHostPath = FileSystemManager.GetHostPath(elfGuestPath);
            byte[] elf = File.ReadAllBytes(elfHostPath);

            // Allocate and copy the image
            MemoryManager.MemoryBlock memoryBlock = memoryManager.Allocator.AllocateVirtualMemory((uint)elf.Length);
            GuestVoidPtr elfPtr = memoryBlock.VirtualAddress;
            memoryManager.CopyBufferToVirtualMemory(elf, elfPtr);

            return GetSpuElfInfo(elfPtr, memoryManager);
        }

        internal (ElfHeader32 header, List<SysSpu.SysSpuSegment> segments) GetSpuElfInfo(GuestVoidPtr elfPtr, MemoryManager memoryManager)
        {
            // Note: SPU elf is 32-bit BE

            // Read Main Header 
            ElfHeader32 mainHeader = memoryManager.Read<ElfHeader32>(elfPtr);

            if (!mainHeader.IsValid)
            {
                throw new Exception("Invalid SPU ELF");
            }

            List<SysSpu.SysSpuSegment> segments = CreateSegmentList(elfPtr, memoryManager, ref mainHeader);

            return (mainHeader, segments);
        }

        private static List<SysSpu.SysSpuSegment> CreateSegmentList(GuestVoidPtr elfPtr, MemoryManager memoryManager, ref ElfHeader32 mainHeader)
        {
            List<SysSpu.SysSpuSegment> segments = [];
            ReadOnlySpan<ProgramHeader32> programHeaders = GetProgramHeadersArray(memoryManager, elfPtr, ref mainHeader);

            for (int i = 0; i < programHeaders.Length; i++)
            {
                ProgramHeader32 programHeader = programHeaders[i];

                if (programHeader.IsLoadable)
                {
                    AddSpuSegment(elfPtr, segments, ref programHeader, memoryManager);
                }
            }

            return segments;
        }

        private static void AddSpuSegment(uint elfPtr, List<SysSpu.SysSpuSegment> segments, ref ProgramHeader32 programHeader, MemoryManager memoryManager)
        {
            uint fileSize = programHeader.p_filesz;
            uint memSize = programHeader.p_memsz;

            // Create SysSPUSegment
            SysSpu.SysSpuSegment segment = CreateSpuSegment(elfPtr, ref programHeader, isFill: false);
            segments.Add(segment);

            if (memSize > fileSize)
            {
                // Create SysSpuSegment for the filler
                SysSpu.SysSpuSegment fillSegment = CreateSpuSegment(elfPtr, ref programHeader, isFill: true);
                segments.Add(fillSegment);
            }
        }

        private static SysSpu.SysSpuSegment CreateSpuSegment(uint elfPtr, ref ProgramHeader32 programHeader, bool isFill)
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
                type = SysSpu.SysSpuSegmentTypeFill;
                ls = dest + fileSize;
                src = 0;
                size = (int)(memSize - fileSize);
            }
            else
            {
                type = SysSpu.SysSpuSegmentTypeCopy;
                ls = dest;
                src = dataAddress;
                size = (int)memSize;
            }

            SysSpu.SysSpuSegment segment = new()
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