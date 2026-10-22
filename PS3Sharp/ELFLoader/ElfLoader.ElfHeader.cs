using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal partial class ElfLoader
    {
        // Structs
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ElfHeader
        {
            // e_ident constants
            internal const int EiNident = 16;

            // ELFMAG bytes - these must appear at the beginning of every valid ELF file
            internal const byte Elfmag0 = 0x7F;
            internal const byte Elfmag1 = (byte)'E';
            internal const byte Elfmag2 = (byte)'L';
            internal const byte Elfmag3 = (byte)'F';

            // ELFMAG as a ROS
            internal static ReadOnlySpan<byte> ELFMAG_ROS => [Elfmag0, Elfmag1, Elfmag2, Elfmag3];

            internal const byte Elfclass32 = 1;
            internal const byte Elfclass64 = 2;

            internal const byte Elfdata2lsb = 1;
            internal const byte Elfdata2msb = 2;

            internal const byte EvCurrent = 1;

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
