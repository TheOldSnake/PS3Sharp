using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Buffers;
using System.Runtime.InteropServices;
using static PS3Sharp.Elf.ElfDefinitions;

namespace PS3Sharp.Elf
{
    internal class PRXElfLoader : ElfLoader
    {
        // Custom struct - not from ELF spec
        internal struct LoadedPRXSegment
        {
            internal ProgramHeader64 ProgramHeader;
            internal uint ActualEntry;
        }

        internal struct PRXRelocation
        {
            internal BigEndian<ulong> offs;
            internal BigEndian<ushort> unk;
            internal byte data_idx;
            internal byte addr_idx;
            internal BigEndian<uint> type;
            internal BigEndian<ulong> ptr;
        }

        internal List<LoadedPRXSegment> LoadPRXElf(BinaryReader reader, MemoryManager memoryManager)
        {      
            // Read Main Header 
            ElfHeader64 mainHeader = ReadElfStruct<ElfHeader64>(reader, 0);

            if (!mainHeader.IsValid)
            {
                throw new Exception("Invalid ELF");
            }

            List<LoadedPRXSegment> loadedSegments = LoadSegments(reader, memoryManager, ref mainHeader);
            HandleRelocations(reader, ref mainHeader, loadedSegments, memoryManager);

            return loadedSegments;
        }

        private List<LoadedPRXSegment> LoadSegments(BinaryReader reader, MemoryManager memoryManager, ref ElfHeader64 mainHeader)
        {
            // Load the loadable segments
            ReadOnlySpan<ProgramHeader64> programHeaders = GetProgramHeadersArray(reader, ref mainHeader);
            List<LoadedPRXSegment> loadedSegments = [];

            for (int i = 0; i < programHeaders.Length; i++)
            {
                ProgramHeader64 programHeader = programHeaders[i];

                if (programHeader.IsLoadable)
                {
                    LoadedPRXSegment loadedSegment;
                    loadedSegment.ProgramHeader = programHeader;
                    loadedSegment.ActualEntry = LoadSegment(reader, ref programHeader, memoryManager);
                    loadedSegments.Add(loadedSegment);
                }
            }

            return loadedSegments;
        }

        private static uint LoadSegment(BinaryReader reader, ref ProgramHeader64 programHeader, MemoryManager memoryManager)
        {
            // Relocatable segments, not loaded at a specific virtual addreess
            byte[] segmentData = GetSegmentData(reader, ref programHeader);
            MemoryManager.MemoryBlock block = memoryManager.Allocator.AllocateVirtualMemory((uint)(ulong)programHeader.p_memsz);
            memoryManager.CopyBufferToVirtualMemory(segmentData, block.VirtualAddress);
            return block.VirtualAddress;
        }

        private static void HandleRelocations(BinaryReader reader, ref ElfHeader64 header, List<LoadedPRXSegment> loadedSegments, MemoryManager memoryManager)
        {
            for (uint i = 0; i < header.e_phnum; i++)
            {
                long phOff = (long)(header.e_phoff + (i * header.e_phentsize));
                ProgramHeader64 programHeader = ReadElfStruct<ProgramHeader64>(reader, phOff);

                if (programHeader.p_type == ScePpurela)
                {
                    byte[] segmentData = GetSegmentData(reader, ref programHeader);
                    ReadOnlySpan<PRXRelocation> relocations = MemoryMarshal.Cast<byte, PRXRelocation>(segmentData);

                    for (int j = 0; j < relocations.Length; j++)
                    {
                        PRXRelocation relocation = relocations[j];

                        // essencially we calculate the address of data_segment_base + ptr, and we write it at address_segment_base + offs

                        uint data_segment_idx = relocation.data_idx == 0xFF ? 0 : (uint)relocation.data_idx;
                        uint address_segment_idx = relocation.addr_idx;


                        uint data = (uint)(loadedSegments[(int)data_segment_idx].ActualEntry + relocation.ptr);
                        uint address = (uint)(loadedSegments[(int)address_segment_idx].ActualEntry + relocation.offs);

                        uint type = relocation.type;

                        Console.WriteLine($"[ELFLoader] Relocation type = {type} at [0x{address:X8}] data = 0x{data:X8} -- ptr = 0x{relocation.ptr:X8} offs = 0x{relocation.offs:X8}");

                        ushort temp;

                        switch (type)
                        {
                            case 1:
                                memoryManager.Write<BigEndian<uint>>(address, data);
                                break;

                            case 4:
                                temp = (ushort)data;
                                memoryManager.Write<BigEndian<ushort>>(address, temp);
                                break;

                            case 5:
                                temp = (ushort)(data >> 16);
                                memoryManager.Write<BigEndian<ushort>>(address, temp);
                                break;

                            case 6:
                                temp = (ushort)((data >> 16) + ((data >> 15) & 1));
                                memoryManager.Write<BigEndian<ushort>>(address, temp);
                                break;

                            default:
                                // todo
                                Console.WriteLine($"[ELFLoader] unhandled relocation type: {type}");
                                break;
                        }

                        //Console.WriteLine($"[ELFLoader] mem[0x{address:X8}] = 0x{MemoryManager.Read<uint>(address):X8}");
                    }
                }
            }
        }
    }
}
