using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp {
    public static partial class ElfLoader {
        //Structs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public readonly struct ElfHeader {
            //e_ident constants
            public const int EI_NIDENT = 16;

            // ELFMAG bytes - these must appear at the beginning of every valid ELF file
            public const byte ELFMAG0 = 0x7F;
            public const byte ELFMAG1 = (byte)'E';
            public const byte ELFMAG2 = (byte)'L';
            public const byte ELFMAG3 = (byte)'F';

            // ELFMAG as a ROS
            public static ReadOnlySpan<byte> ELFMAG_ROS => [ELFMAG0, ELFMAG1, ELFMAG2, ELFMAG3];

            public const byte ELFCLASS32 = 1;
            public const byte ELFCLASS64 = 2;

            public const byte ELFDATA2LSB = 1;
            public const byte ELFDATA2MSB = 2;

            public const byte EV_CURRENT = 1;

            //Actual Fields          
            public readonly InlineArray16<byte> e_ident;   /* ELF identification */
            public readonly BigEndian<ushort> e_type;      /* object file type */
            public readonly BigEndian<ushort> e_machine;   /* machine type */
            public readonly BigEndian<uint> e_version;     /* object file version */
            public readonly BigEndian<ulong> e_entry;      /* entry point address */
            public readonly BigEndian<ulong> e_phoff;      /* program header offset */
            public readonly BigEndian<ulong> e_shoff;      /* section header offset */
            public readonly BigEndian<uint> e_flags;       /* processor-specific flags */
            public readonly BigEndian<ushort> e_ehsize;    /* ELF header size */
            public readonly BigEndian<ushort> e_phentsize; /* size of program header entry */
            public readonly BigEndian<ushort> e_phnum;     /* number of program header entries */
            public readonly BigEndian<ushort> e_shentsize; /* size of section header entry */
            public readonly BigEndian<ushort> e_shnum;     /* number of section header entries */
            public readonly BigEndian<ushort> e_shstrndx;  /* section name string table index */
        }
    }
}
