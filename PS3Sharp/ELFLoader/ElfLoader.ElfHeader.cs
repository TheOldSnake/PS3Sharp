using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal static partial class ElfLoader
    {
        //Structs
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ElfHeader
        {
            //e_ident constants
            internal const int EI_NIDENT = 16;

            // ELFMAG bytes - these must appear at the beginning of every valid ELF file
            internal const byte ELFMAG0 = 0x7F;
            internal const byte ELFMAG1 = (byte)'E';
            internal const byte ELFMAG2 = (byte)'L';
            internal const byte ELFMAG3 = (byte)'F';

            // ELFMAG as a ROS
            internal static ReadOnlySpan<byte> ELFMAG_ROS => [ELFMAG0, ELFMAG1, ELFMAG2, ELFMAG3];

            internal const byte ELFCLASS32 = 1;
            internal const byte ELFCLASS64 = 2;

            internal const byte ELFDATA2LSB = 1;
            internal const byte ELFDATA2MSB = 2;

            internal const byte EV_CURRENT = 1;

            //Actual Fields          
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
    }
}
