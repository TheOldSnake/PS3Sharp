using OpenTK.Graphics.ES11;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Modules;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.ElfLoader;
using static PS3Sharp.ELFLoader.ElfLoader;

namespace PS3Sharp {
    public static partial class ElfLoader {

        public struct LoadedPRXSegment { // Custom struct - not from ELF spec
            public ProgramHeader ProgramHeader;
            public uint ActualEntry;
        }

        public static byte[] GetSegmentData(BinaryReader reader, ref ProgramHeader programHeader) {
            ulong offset = programHeader.p_offset;
            ulong size = programHeader.p_filesz;

            reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);
            return reader.ReadBytes((int)size);
        }

        public static bool IsValidELF(ReadOnlySpan<byte> ident) {
            // Ensure ELFMAG is present in the first 4 bytes
            int length = ElfHeader.ELFMAG_ROS.Length;
            ReadOnlySpan<byte> mag = ident.Slice(0, length);
            return mag.SequenceEqual<byte>(ElfHeader.ELFMAG_ROS);
        }

        private static long StringTableLocation;
        public static uint LoadMainElf(string path) {
            BinaryReader reader = new BinaryReader(File.OpenRead(path));

            Console.WriteLine($"ELF Size: {reader.BaseStream.Length}");

            //Read Main Header 
            ElfHeader mainHeader = ReadElfStruct<ElfHeader>(reader, 0);

            ReadOnlySpan<byte> ident = mainHeader.e_ident;
            if (!IsValidELF(ident)) {
                throw new Exception("Invalid ELF");
            }
               
            byte bitSize    = ident[4];     // 1 = 32bit, 2 = 64bit
            byte endianness = ident[5];     // endianness
            bool is64 = bitSize == ElfHeader.ELFCLASS64;

           // Console.WriteLine($"ELF {(is64 ? "64" : "32")}-bit");
           // Console.WriteLine($"Machine: 0x{mainHeader.e_machine:X}");
           // Console.WriteLine($"Endianness: 0x{endianness:X}");
            Console.WriteLine($"Program Headers: 0x{mainHeader.e_phnum:X}");
            Console.WriteLine($"Fake Entry: 0x{mainHeader.e_entry:X}");

            HandleELFLoadableSegments(reader, ref mainHeader);
            HandleTLSSegment(reader, ref mainHeader);
            HandlePRXPARAMSegment(reader, ref mainHeader);

            //---Not needed---
            //StringTableLocation = FindStringTable(fileStream, reader, ref mainHeader);
            //PrintSections(fileStream, reader, ref mainHeader);
            //---------------

            Console.WriteLine($"[ELFLoader] ELF loaded");
            return (uint)mainHeader.e_entry;
        }

        public static List<LoadedPRXSegment> LoadPRXElf(BinaryReader reader) {
            List<LoadedPRXSegment> loadedSegments = new List<LoadedPRXSegment>();

            // Read Main Header 
            ElfHeader mainHeader = ReadElfStruct<ElfHeader>(reader, 0);

            // Ensure it is valid
            ReadOnlySpan<byte> ident = mainHeader.e_ident;
            if (!IsValidELF(ident)) {
                throw new Exception("Invalid ELF");
            }

            // Load the loadable segments

            unsafe {
                ReadOnlySpan<ProgramHeader> programHeaders = GetProgramHeadersArray(reader, ref mainHeader);
                for (int i = 0; i < programHeaders.Length; i++) {
                    ProgramHeader programHeader = programHeaders[i];

                    if (programHeader.p_type == ProgramHeader.PT_LOAD && programHeader.p_memsz > 0) {
                        LoadedPRXSegment loadedSegment;
                        loadedSegment.ProgramHeader = programHeader;
                        loadedSegment.ActualEntry = LoadSegmentPRX(reader, ref programHeader);
                        loadedSegments.Add(loadedSegment);
                    }
                }
            }

            HandleRelocations(reader, ref mainHeader, loadedSegments);
            return loadedSegments;
        }

        public static ReadOnlySpan<ProgramHeader> GetProgramHeadersArray(BinaryReader reader, ref ElfHeader header) {
            // Get the array of program headers in the ELF
            ushort numberOfProgramHeaders = header.e_phnum; // how many program headers
            ushort programHeaderSize = header.e_phentsize;  // size of a single header
            ulong programHeadersOffset = header.e_phoff;    // offset of the array
            return ReadElfSpan<ProgramHeader>(reader, (long)programHeadersOffset, numberOfProgramHeaders);
        }

        public static void HandleELFLoadableSegments(BinaryReader reader, ref ElfHeader mainHeader) {
            ReadOnlySpan<ProgramHeader> programHeaders = GetProgramHeadersArray(reader, ref mainHeader);

            // Load the loadable segments
            for (int i = 0; i < programHeaders.Length; i++) {
                ProgramHeader programHeader = programHeaders[i];

                if (programHeader.p_type == ProgramHeader.PT_LOAD && programHeader.p_memsz > 0) {
                    LoadSegment(reader, ref programHeader);
                }            
            }
        }

        private static uint LoadSegmentPRX(BinaryReader reader, ref ProgramHeader programHeader) {
            // Relocatable segments, not loaded at a specific virtual addreess
            byte[] segmentData = GetSegmentData(reader, ref programHeader);
            MemoryManager.MemoryBlock block = MemoryManager.AllocateVirtualMemory((uint)(ulong)programHeader.p_memsz);
            MemoryManager.CopyBufferToVirtualMemory(segmentData, block.VirtualAddress);
            return block.VirtualAddress;
        }

        private static uint LoadSegment(BinaryReader reader, ref ProgramHeader programHeader) {
            string pt_name = ProgramHeader.GetName(programHeader);

            //Load segment
            byte[] segmentData = GetSegmentData(reader, ref programHeader);
            uint dest;

            if (!MemoryManager.IsVirtualAddressAllocated((uint)programHeader.p_vaddr)) {
                MemoryManager.MemoryBlock block = MemoryManager.AllocateVirtualAddress((uint)programHeader.p_vaddr, (uint)programHeader.p_memsz);
                dest = block.VirtualAddress;
            } else {
                dest = (uint)programHeader.p_vaddr;
                
                // Make sure the block has enough size
                uint programEnd = (uint)(dest + programHeader.p_memsz);
                MemoryManager.MemoryBlock block = MemoryManager.GetAllocationByVirtualAddress((uint)programHeader.p_vaddr);
                int extraBytes = (int)(programEnd - block.End);

                if (extraBytes > 0) {
                    uint newSize = (uint)(block.Size + extraBytes);
                    block = MemoryManager.ResizeBlock(block, newSize);
                }

                Console.WriteLine($"[Loader] Warning: Segment overwrite at vaddr = 0x{programHeader.p_vaddr:X8} size = 0x{programHeader.p_memsz:X8} segment: {pt_name}");
            }

            MemoryManager.CopyBufferToVirtualMemory(segmentData, dest);

            if ((ulong)segmentData.Length < programHeader.p_memsz) {
                uint zeroesCount = (uint)programHeader.p_memsz - (uint)segmentData.Length;
                MemoryManager.ClearMemory(dest + (uint)segmentData.Length, zeroesCount);
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

        private static void HandleRelocations(BinaryReader reader, ref ElfHeader header, List<LoadedPRXSegment> loadedSegments) {
            for (uint i = 0; i < header.e_phnum; i++) {
                long phOff = (long)(header.e_phoff + (i * header.e_phentsize));
                ProgramHeader programHeader = ReadElfStruct<ProgramHeader>(reader, phOff);

                if (programHeader.p_type == ProgramHeader.SCE_PPURELA) {
                    byte[] segmentData = GetSegmentData(reader, ref programHeader);
                    ReadOnlySpan<PRXRelocation> relocations = MemoryMarshal.Cast<byte, PRXRelocation>(segmentData);

                    for (int j = 0; j < relocations.Length; j++) {
                        PRXRelocation relocation = relocations[j];

                        // essencially we calculate the address of data_segment_base + ptr, and we write it at address_segment_base + offs

                        uint data_segment_idx = relocation.data_idx == 0xFF ? 0 : (uint)relocation.data_idx; 
                        uint address_segment_idx = relocation.addr_idx;


                        uint data = (uint)(loadedSegments[(int)data_segment_idx].ActualEntry + relocation.ptr);
                        uint address = (uint)(loadedSegments[(int)address_segment_idx].ActualEntry + relocation.offs);

                        uint type = relocation.type;

                        Console.WriteLine($"[ELFLoader] Relocation type = {type} at [0x{address:X8}] data = 0x{data:X8} -- ptr = 0x{relocation.ptr:X8} offs = 0x{relocation.offs:X8}");

                        switch (type) {
                            case 1:
                                MemoryManager.Write<uint>(address, data);
                                break;

                            case 4:
                                MemoryManager.Write<ushort>(address, (ushort)data);
                                break;

                            case 5:
                                MemoryManager.Write<ushort>(address, (ushort)(data >> 16));
                                break;

                            case 6:
                                data = (ushort)(data >> 16) + ((data >> 15) & 1);
                                MemoryManager.Write<ushort>(address, (ushort)data);
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

        private static void HandleTLSSegment(BinaryReader reader, ref ElfHeader header) {
            //Handle TLS segment
            ProgramHeader? tlsImage = FindProgram(reader, ref header, ProgramHeader.PT_TLS);
            if (tlsImage != null) {
                ProgramHeader tlsImage_casted = (ProgramHeader)tlsImage;
                uint tlsvaddr = (uint)(tlsImage_casted.p_vaddr);
                uint tlsSize = (uint)(tlsImage_casted.p_memsz);

                //Load directly
                LoadSegment(reader, ref tlsImage_casted);
                MemoryManager.SetTLS(tlsvaddr, tlsSize);

                Console.WriteLine("Found TLS");
            } else {
                //throw new Exception($"Could not find PT_TLS"); //Is it always provided?
            }
        }

        private static void HandlePRXPARAMSegment(BinaryReader reader, ref ElfHeader header) {
            //Handle PRX_PARAM (module imports) for main elf
            ProgramHeader? prxParam = FindProgram(reader, ref header, ProgramHeader.PRX_PARAM);
            if (prxParam != null) {
                ModulePatcher.HandlePRXImports(reader, (ProgramHeader)prxParam);
            } else {
                //throw new Exception($"Could not find PRX_PARAM");
            }
        }

        private static ProgramHeader? FindProgram(BinaryReader reader, ref ElfHeader header, uint p_type) {
            //Search for requested p_type
            for (uint i = 0; i < header.e_phnum; i++) {
                long phOff = (long)(header.e_phoff + (i * header.e_phentsize));
                ProgramHeader programHeader = ReadElfStruct<ProgramHeader>(reader, phOff);
                if (programHeader.p_type == p_type) {
                    return programHeader;
                }
            }

            return null;
        }

        private static void PrintSections(BinaryReader reader, ref ElfHeader header) {
            for (uint i = 0; i < header.e_shnum; i++) {
                long secOff = (long)(header.e_shoff + (i * header.e_shentsize));
                SectionHeader sec = ReadElfStruct<SectionHeader>(reader, secOff);
                string actualName = StringTableLocation > 0 ? ResolveSectionName(reader, sec.sh_name) : string.Empty;
                Console.WriteLine(
                    $"Section name: {actualName,-20} -- " +
                    $"Section type: {sec.sh_type,8:X} -- " +
                    $"Section flags: {sec.sh_flags,8:X} " +
                    $"Section size: {sec.sh_size,8:X}"
                );
            }
        }

        private static long FindStringTable(BinaryReader reader, ref ElfHeader header) {
            long stringTableOffset = -1;
            for (uint i = 0; i < header.e_shnum; i++) {
                long secOff = (long)(header.e_shoff + (i * header.e_shentsize));
                SectionHeader sec = ReadElfStruct<SectionHeader>(reader, secOff);
                if (sec.sh_type == SectionHeader.SHT_STRTAB) {     //SHT_STRTAB is the string table
                    stringTableOffset = (long)(ulong)sec.sh_offset;
                    break;
                }
            }

            return stringTableOffset;
        }

        public static T ReadElfStruct<T>(BinaryReader reader, long offset) where T: unmanaged {
            int size = Unsafe.SizeOf<T>();
            Span<byte> buffer = size >= 1024 ? new byte[size] : stackalloc byte[size];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            reader.ReadExactly(buffer);

            // do not return the Span itself as it could have been allocated on the stack
            return MemoryMarshal.Read<T>(buffer); 
        }

        public static ReadOnlySpan<T> ReadElfSpan<T>(BinaryReader reader, long offset, uint numberOfElements) where T: unmanaged {
            int size = (int)(Unsafe.SizeOf<T>() * numberOfElements);
            Span<byte> buffer = new byte[size];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            reader.ReadExactly(buffer);

            return MemoryMarshal.Cast<byte, T>(buffer);
        }

        public static string ResolveSectionName(BinaryReader reader, uint sectionName) {
            string name = string.Empty;
            reader.BaseStream.Seek(StringTableLocation + sectionName, SeekOrigin.Begin);
            byte c = reader.ReadByte();
            while (c != 0) {
                name += (char)c;
                c = reader.ReadByte();
            }

            return name;
        }
    }
}
