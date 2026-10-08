using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal partial class ElfLoader
    {
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct SectionHeader
        {
            //Section types (SHT_*)
            internal const uint SHT_NULL = 0;
            internal const uint SHT_PROGBITS = 1;
            internal const uint SHT_SYMTAB = 2;
            internal const uint SHT_STRTAB = 3;
            internal const uint SHT_RELA = 4;
            internal const uint SHT_HASH = 5;
            internal const uint SHT_DYNAMIC = 6;
            internal const uint SHT_NOTE = 7;
            internal const uint SHT_NOBITS = 8;
            internal const uint SHT_REL = 9;
            internal const uint SHT_SHLIB = 10;
            internal const uint SHT_DYNSYM = 11;

            //OS-specific
            internal const uint SHT_LOOS = 0x60000000;
            internal const uint SHT_HIOS = 0x6FFFFFFF;

            //Processor-specific
            internal const uint SHT_LOPROC = 0x70000000;
            internal const uint SHT_HIPROC = 0x7FFFFFFF;

            //Fields
            internal readonly BigEndian<uint> sh_name;        /* section name */
            internal readonly BigEndian<uint> sh_type;        /* section type */
            internal readonly BigEndian<ulong> sh_flags;      /* section attributes */
            internal readonly BigEndian<ulong> sh_addr;       /* virtual address in memory */
            internal readonly BigEndian<ulong> sh_offset;     /* offset in file */
            internal readonly BigEndian<ulong> sh_size;       /* size of section */
            internal readonly BigEndian<uint> sh_link;        /* link to other section */
            internal readonly BigEndian<uint> sh_info;        /* miscellaneous information */
            internal readonly BigEndian<ulong> sh_addralign;  /* address alignment boundary */
            internal readonly BigEndian<ulong> sh_entsize;    /* size of entries, if section has table */
        }
    }
}
