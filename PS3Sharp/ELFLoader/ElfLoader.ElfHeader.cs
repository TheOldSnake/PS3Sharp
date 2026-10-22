using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal partial class ElfLoader
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
        }
    }
}
