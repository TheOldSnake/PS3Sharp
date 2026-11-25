using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PS3Sharp.Elf.ElfDefinitions;

namespace PS3Sharp.Elf
{
    internal abstract class ElfLoader
    {
        internal static uint GetTotalRequiredSize(ReadOnlySpan<ProgramHeader64> programHeaders)
        {
            ulong sum = 0;

            foreach (ProgramHeader64 header in programHeaders)
            {
                ulong size = header.p_memsz;
                sum += size;
            }

            if (sum > uint.MaxValue)
            {
                throw new OverflowException($"Segments total size exceeded uint.MaxValue");
            }

            return (uint)sum;
        }

        internal static uint FindMinimumAddress(ReadOnlySpan<ProgramHeader64> programHeaders)
        {
            ulong min = programHeaders[0].p_vaddr;

            foreach (ProgramHeader64 header in programHeaders)
            {
                if (header.p_vaddr < min)
                {
                    min = header.p_vaddr;
                }
            }

            return (uint)min;
        }

        internal static uint FindMaximumAlignment(ReadOnlySpan<ProgramHeader64> programHeaders)
        {
            ulong maxAlign = programHeaders[0].p_align;

            foreach (ProgramHeader64 header in programHeaders)
            {
                if (header.p_align > maxAlign)
                {
                    maxAlign = header.p_align;
                }
            }

            return (uint)maxAlign;
        }

        internal static bool TryFindProgram(BinaryReader reader, ref ElfHeader64 header, uint p_type, out ProgramHeader64 programHeader)
        {
            // Get all program headers
            ReadOnlySpan<ProgramHeader64> programs = GetProgramHeadersArray(reader, ref header);

            // Search for requested p_type
            foreach (ProgramHeader64 program in programs)
            {
                if (program.p_type == p_type)
                {
                    programHeader = program;
                    return true;
                }
            }

            programHeader = default;
            return false;
        }

        internal static T ReadElfStruct<T>(BinaryReader reader, long offset) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            Span<byte> buffer = size >= 1024 ? new byte[size] : stackalloc byte[size];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            reader.ReadExactly(buffer);

            // do not return the Span itself as it could have been allocated on the stack
            return MemoryMarshal.Read<T>(buffer);
        }

        protected static ReadOnlySpan<ProgramHeader64> GetProgramHeadersArray(BinaryReader reader, ref ElfHeader64 header)
        {
            // Get the array of program headers in the ELF
            ushort numberOfProgramHeaders = header.e_phnum; // how many program headers
            ushort programHeaderSize = header.e_phentsize;  // size of a single header
            ulong programHeadersOffset = header.e_phoff;    // offset of the array
            return ReadElfSpan<ProgramHeader64>(reader, (long)programHeadersOffset, numberOfProgramHeaders);
        }

        protected static byte[] GetSegmentData(BinaryReader reader, ref ProgramHeader64 programHeader)
        {
            ulong offset = programHeader.p_offset;
            ulong size = programHeader.p_filesz;

            reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);
            return reader.ReadBytes((int)size);
        }

        private static void PrintSections(BinaryReader reader, ref ElfHeader64 header, long stringTableLocation)
        {
            for (uint i = 0; i < header.e_shnum; i++)
            {
                long secOff = (long)(header.e_shoff + (i * header.e_shentsize));
                SectionHeader sec = ReadElfStruct<SectionHeader>(reader, secOff);
                string actualName = stringTableLocation > 0 ? ResolveSectionName(reader, sec.sh_name, stringTableLocation) : string.Empty;
                Console.WriteLine(
                    $"Section name: {actualName,-20} -- " +
                    $"Section type: {sec.sh_type,8:X} -- " +
                    $"Section flags: {sec.sh_flags,8:X} " +
                    $"Section size: {sec.sh_size,8:X}"
                );
            }
        }

        private static long FindStringTable(BinaryReader reader, ref ElfHeader64 header)
        {
            long stringTableOffset = -1;
            for (uint i = 0; i < header.e_shnum; i++)
            {
                long secOff = (long)(header.e_shoff + (i * header.e_shentsize));
                SectionHeader sec = ReadElfStruct<SectionHeader>(reader, secOff);
                if (sec.sh_type == ShtStrtab)
                {     //SHT_STRTAB is the string table
                    stringTableOffset = (long)(ulong)sec.sh_offset;
                    break;
                }
            }

            return stringTableOffset;
        }

        private static ReadOnlySpan<T> ReadElfSpan<T>(BinaryReader reader, long offset, uint numberOfElements) where T : unmanaged
        {
            int size = (int)(Unsafe.SizeOf<T>() * numberOfElements);
            Span<byte> buffer = new byte[size];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            reader.ReadExactly(buffer);

            return MemoryMarshal.Cast<byte, T>(buffer);
        }

        private static string ResolveSectionName(BinaryReader reader, uint sectionName, long stringTableLocation)
        {
            string name = string.Empty;
            reader.BaseStream.Seek(stringTableLocation + sectionName, SeekOrigin.Begin);
            byte c = reader.ReadByte();
            while (c != 0)
            {
                name += (char)c;
                c = reader.ReadByte();
            }

            return name;
        }
    }
}