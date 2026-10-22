using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal partial class ElfLoader
    {
        // Custom struct - not from ELF spec
        internal struct LoadedPRXSegment
        {
            internal ProgramHeader ProgramHeader;
            internal uint ActualEntry;
        }

        private long _stringTableLocation;

        internal ElfHeader LoadMainElf(BinaryReader reader, MemoryManager memoryManager)
        {
            // Read Main Header 
            ElfHeader mainHeader = ReadElfStruct<ElfHeader>(reader, 0);
            ReadOnlySpan<byte> ident = mainHeader.e_ident;

            if (!IsValidELF(ident))
            {
                throw new Exception("Invalid ELF");
            }

            HandleELFLoadableSegments(reader, ref mainHeader, memoryManager);
            HandleTLSSegment(reader, ref mainHeader, memoryManager);

            Console.WriteLine($"[ELFLoader] ELF loaded!");
            return mainHeader;
        }

        internal List<LoadedPRXSegment> LoadPRXElf(BinaryReader reader, MemoryManager memoryManager)
        {
            List<LoadedPRXSegment> loadedSegments = new();

            // Read Main Header 
            ElfHeader mainHeader = ReadElfStruct<ElfHeader>(reader, 0);

            // Ensure it is valid
            ReadOnlySpan<byte> ident = mainHeader.e_ident;
            if (!IsValidELF(ident))
            {
                throw new Exception("Invalid ELF");
            }

            // Load the loadable segments

            unsafe
            {
                ReadOnlySpan<ProgramHeader> programHeaders = GetProgramHeadersArray(reader, ref mainHeader);
                for (int i = 0; i < programHeaders.Length; i++)
                {
                    ProgramHeader programHeader = programHeaders[i];

                    if (programHeader.p_type == ProgramHeader.PtLoad && programHeader.p_memsz > 0)
                    {
                        LoadedPRXSegment loadedSegment;
                        loadedSegment.ProgramHeader = programHeader;
                        loadedSegment.ActualEntry = LoadSegmentPRX(reader, ref programHeader, memoryManager);
                        loadedSegments.Add(loadedSegment);
                    }
                }
            }

            HandleRelocations(reader, ref mainHeader, loadedSegments, memoryManager);
            return loadedSegments;
        }

        internal ProgramHeader? FindProgram(BinaryReader reader, ref ElfHeader header, uint p_type)
        {
            //Search for requested p_type
            for (uint i = 0; i < header.e_phnum; i++)
            {
                long phOff = (long)(header.e_phoff + (i * header.e_phentsize));
                ProgramHeader programHeader = ReadElfStruct<ProgramHeader>(reader, phOff);
                if (programHeader.p_type == p_type)
                {
                    return programHeader;
                }
            }

            return null;
        }

        internal T ReadElfStruct<T>(BinaryReader reader, long offset) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            Span<byte> buffer = size >= 1024 ? new byte[size] : stackalloc byte[size];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            reader.ReadExactly(buffer);

            // do not return the Span itself as it could have been allocated on the stack
            return MemoryMarshal.Read<T>(buffer);
        }

        private ReadOnlySpan<ProgramHeader> GetProgramHeadersArray(BinaryReader reader, ref ElfHeader header)
        {
            // Get the array of program headers in the ELF
            ushort numberOfProgramHeaders = header.e_phnum; // how many program headers
            ushort programHeaderSize = header.e_phentsize;  // size of a single header
            ulong programHeadersOffset = header.e_phoff;    // offset of the array
            return ReadElfSpan<ProgramHeader>(reader, (long)programHeadersOffset, numberOfProgramHeaders);
        }

        internal void HandleELFLoadableSegments(BinaryReader reader, ref ElfHeader mainHeader, MemoryManager memoryManager)
        {
            ReadOnlySpan<ProgramHeader> programHeaders = GetProgramHeadersArray(reader, ref mainHeader);

            // Load the loadable segments
            for (int i = 0; i < programHeaders.Length; i++)
            {
                ProgramHeader programHeader = programHeaders[i];

                if (programHeader.p_type == ProgramHeader.PtLoad && programHeader.p_memsz > 0)
                {
                    LoadSegment(reader, ref programHeader, memoryManager);
                }
            }
        }

        private uint LoadSegmentPRX(BinaryReader reader, ref ProgramHeader programHeader, MemoryManager memoryManager)
        {
            // Relocatable segments, not loaded at a specific virtual addreess
            byte[] segmentData = GetSegmentData(reader, ref programHeader);
            MemoryManager.MemoryBlock block = memoryManager.AllocateVirtualMemory((uint)(ulong)programHeader.p_memsz);
            memoryManager.CopyBufferToVirtualMemory(segmentData, block.VirtualAddress);
            return block.VirtualAddress;
        }

        private uint LoadSegment(BinaryReader reader, ref ProgramHeader programHeader, MemoryManager memoryManager)
        {
            string pt_name = ProgramHeader.GetName(programHeader);
            uint p_memsz = (uint)(ulong)programHeader.p_memsz;
            uint p_vaddr = (uint)(ulong)programHeader.p_vaddr;

            // Load segment
            byte[] segmentData = GetSegmentData(reader, ref programHeader);
            uint dest;

            if (!memoryManager.IsVirtualAddressAllocated(p_vaddr))
            {
                MemoryManager.MemoryBlock block = memoryManager.AllocateVirtualAddress(p_vaddr, p_memsz);
                dest = block.VirtualAddress;
            }
            else
            {
                dest = p_vaddr;

                // Make sure the block has enough size
                uint programEnd = dest + p_memsz;
                MemoryManager.MemoryBlock block = memoryManager.GetAllocationByVirtualAddress(p_vaddr);
                int extraBytes = (int)(programEnd - block.End);

                if (extraBytes > 0)
                {
                    uint newSize = (uint)(block.Size + extraBytes);
                    block = memoryManager.ResizeBlock(block, newSize);
                }

                Console.WriteLine($"[Loader] Warning: Segment overwrite at vaddr = 0x{p_vaddr:X8} size = 0x{p_memsz:X8} segment: {pt_name}");
            }

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

        private void HandleRelocations(BinaryReader reader, ref ElfHeader header, List<LoadedPRXSegment> loadedSegments, MemoryManager memoryManager)
        {
            for (uint i = 0; i < header.e_phnum; i++)
            {
                long phOff = (long)(header.e_phoff + (i * header.e_phentsize));
                ProgramHeader programHeader = ReadElfStruct<ProgramHeader>(reader, phOff);

                if (programHeader.p_type == ProgramHeader.ScePpurela)
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

        private void HandleTLSSegment(BinaryReader reader, ref ElfHeader header, MemoryManager memoryManager)
        {
            //Handle TLS segment
            ProgramHeader? tlsImage = FindProgram(reader, ref header, ProgramHeader.PtTls);
            if (tlsImage != null)
            {
                ProgramHeader tlsImage_casted = (ProgramHeader)tlsImage;
                uint tlsvaddr = (uint)(tlsImage_casted.p_vaddr);
                uint tlsSize = (uint)(tlsImage_casted.p_memsz);

                //Load directly
                LoadSegment(reader, ref tlsImage_casted, memoryManager);
                memoryManager.SetTLS(tlsvaddr, tlsSize);

                Console.WriteLine("Found TLS");
            }
            else
            {
                //throw new Exception($"Could not find PT_TLS"); //Is it always provided?
            }
        }

        //private void HandlePRXPARAMSegment(BinaryReader reader, ref ElfHeader header, ModulePatcher modulePatcher, PRXManager.PRXManager prxManager, MemoryManager memoryManager)
        //{
        //    //Handle PRX_PARAM (module imports) for main elf
        //    ProgramHeader? prxParam = FindProgram(reader, ref header, ProgramHeader.PRX_PARAM);
        //    if (prxParam != null)
        //    {
        //        modulePatcher.HandlePRXImports(reader, (ProgramHeader)prxParam, memoryManager, prxManager, this);
        //    }
        //    else
        //    {
        //        //throw new Exception($"Could not find PRX_PARAM");
        //    }
        //}

        private void PrintSections(BinaryReader reader, ref ElfHeader header)
        {
            for (uint i = 0; i < header.e_shnum; i++)
            {
                long secOff = (long)(header.e_shoff + (i * header.e_shentsize));
                SectionHeader sec = ReadElfStruct<SectionHeader>(reader, secOff);
                string actualName = _stringTableLocation > 0 ? ResolveSectionName(reader, sec.sh_name) : string.Empty;
                Console.WriteLine(
                    $"Section name: {actualName,-20} -- " +
                    $"Section type: {sec.sh_type,8:X} -- " +
                    $"Section flags: {sec.sh_flags,8:X} " +
                    $"Section size: {sec.sh_size,8:X}"
                );
            }
        }

        private long FindStringTable(BinaryReader reader, ref ElfHeader header)
        {
            long stringTableOffset = -1;
            for (uint i = 0; i < header.e_shnum; i++)
            {
                long secOff = (long)(header.e_shoff + (i * header.e_shentsize));
                SectionHeader sec = ReadElfStruct<SectionHeader>(reader, secOff);
                if (sec.sh_type == SectionHeader.ShtStrtab)
                {     //SHT_STRTAB is the string table
                    stringTableOffset = (long)(ulong)sec.sh_offset;
                    break;
                }
            }

            return stringTableOffset;
        }

        private ReadOnlySpan<T> ReadElfSpan<T>(BinaryReader reader, long offset, uint numberOfElements) where T : unmanaged
        {
            int size = (int)(Unsafe.SizeOf<T>() * numberOfElements);
            Span<byte> buffer = new byte[size];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            reader.ReadExactly(buffer);

            return MemoryMarshal.Cast<byte, T>(buffer);
        }

        private string ResolveSectionName(BinaryReader reader, uint sectionName)
        {
            string name = string.Empty;
            reader.BaseStream.Seek(_stringTableLocation + sectionName, SeekOrigin.Begin);
            byte c = reader.ReadByte();
            while (c != 0)
            {
                name += (char)c;
                c = reader.ReadByte();
            }

            return name;
        }

        private byte[] GetSegmentData(BinaryReader reader, ref ProgramHeader programHeader)
        {
            ulong offset = programHeader.p_offset;
            ulong size = programHeader.p_filesz;

            reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);
            return reader.ReadBytes((int)size);
        }

        private bool IsValidELF(ReadOnlySpan<byte> ident)
        {
            // Ensure ELFMAG is present in the first 4 bytes
            int length = ElfHeader.ELFMAG_ROS.Length;
            ReadOnlySpan<byte> mag = ident.Slice(0, length);
            return mag.SequenceEqual<byte>(ElfHeader.ELFMAG_ROS);
        }

        private void PrintElfInfo(ElfHeader mainHeader)
        {
            ReadOnlySpan<byte> ident = mainHeader.e_ident;
            byte bitSize = ident[4];     // 1 = 32bit, 2 = 64bit
            byte endianness = ident[5];  // endianness
            bool is64 = bitSize == ElfHeader.Elfclass64;

            Console.WriteLine($"ELF {(is64 ? "64" : "32")}-bit");
            Console.WriteLine($"Machine: 0x{mainHeader.e_machine:X}");
            Console.WriteLine($"Endianness: 0x{endianness:X}");
            Console.WriteLine($"Program Headers: 0x{mainHeader.e_phnum:X}");
            Console.WriteLine($"Fake Entry: 0x{mainHeader.e_entry:X}");
        }
    }
}
