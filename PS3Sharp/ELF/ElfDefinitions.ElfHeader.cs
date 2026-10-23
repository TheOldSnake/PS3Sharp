using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Elf
{
    internal static partial class ElfDefinitions
    {
        // e_ident constants
        internal const int EIdent = 16;

        // ELFMAG bytes - these must appear at the beginning of every valid ELF file
        internal const byte ElfMag0 = 0x7F;
        internal const byte ElfMag1 = (byte)'E';
        internal const byte ElfMag2 = (byte)'L';
        internal const byte ElfMag3 = (byte)'F';

        // ELFMAG as a ROS
        internal static ReadOnlySpan<byte> ElfMagRos => [ElfMag0, ElfMag1, ElfMag2, ElfMag3];

        internal const byte ElfClass32 = 1;
        internal const byte ElfClass64 = 2;

        internal const byte ElfData2Lsb = 1;
        internal const byte ElfData2Msb = 2;

        internal const byte EvCurrent = 1;

        // Structs

        // Elf64 Header
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ElfHeader64
        {
            internal readonly InlineArray16<byte> e_ident;   /* ELF identification */
            internal readonly BigEndian<ushort> e_type;      /* object file type */
            internal readonly BigEndian<ushort> e_machine;   /* machine type */
            internal readonly BigEndian<uint> e_version;     /* object file version */
            internal readonly BigEndian<ulong> e_entry;      /* entry point address */
            internal readonly BigEndian<ulong> e_phoff;      /* program header offset */
            internal readonly BigEndian<ulong> e_shoff;      /* section header offset */
            internal readonly BigEndian<uint> e_flags;       /* processor-specific flags */
            internal readonly BigEndian<ushort> e_ehsize;    /* ELF header size */
            internal readonly BigEndian<ushort> e_phentsize; /* size of program header entry */
            internal readonly BigEndian<ushort> e_phnum;     /* number of program header entries */
            internal readonly BigEndian<ushort> e_shentsize; /* size of section header entry */
            internal readonly BigEndian<ushort> e_shnum;     /* number of section header entries */
            internal readonly BigEndian<ushort> e_shstrndx;  /* section name string table index */

            internal bool IsValid => IsValidIdent(e_ident);
        }

        // Elf32 Header
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ElfHeader32
        {
            internal readonly InlineArray16<byte> e_ident;   /* ELF identification */
            internal readonly BigEndian<ushort> e_type;      /* object file type */
            internal readonly BigEndian<ushort> e_machine;   /* machine type */
            internal readonly BigEndian<uint> e_version;     /* object file version */
            internal readonly BigEndian<uint> e_entry;      /* entry point address */
            internal readonly BigEndian<uint> e_phoff;      /* program header offset */
            internal readonly BigEndian<uint> e_shoff;      /* section header offset */
            internal readonly BigEndian<uint> e_flags;      /* processor-specific flags */
            internal readonly BigEndian<ushort> e_ehsize;    /* ELF header size */
            internal readonly BigEndian<ushort> e_phentsize; /* size of program header entry */
            internal readonly BigEndian<ushort> e_phnum;     /* number of program header entries */
            internal readonly BigEndian<ushort> e_shentsize; /* size of section header entry */
            internal readonly BigEndian<ushort> e_shnum;     /* number of section header entries */
            internal readonly BigEndian<ushort> e_shstrndx;  /* section name string table index */

            internal bool IsValid => IsValidIdent(e_ident);
        }

        private static bool IsValidIdent(ReadOnlySpan<byte> ident)
        {
            // Simply check if ELFMAG is present in the first 4 bytes
            int length = ElfMagRos.Length;
            ReadOnlySpan<byte> mag = ident.Slice(0, length);
            return mag.SequenceEqual(ElfMagRos);
        }

        private static void PrintElfInfo(ElfHeader64 mainHeader)
        {
            ReadOnlySpan<byte> ident = mainHeader.e_ident;
            byte bitSize = ident[4];     // 1 = 32bit, 2 = 64bit
            byte endianness = ident[5];  // endianness
            bool is64 = bitSize == ElfClass64;

            Console.WriteLine($"ELF {(is64 ? "64" : "32")}-bit");
            Console.WriteLine($"Machine: 0x{mainHeader.e_machine:X}");
            Console.WriteLine($"Endianness: 0x{endianness:X}");
            Console.WriteLine($"Program Headers: 0x{mainHeader.e_phnum:X}");
            Console.WriteLine($"Fake Entry: 0x{mainHeader.e_entry:X}");
        }
    }
}
