using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp {
    public static unsafe partial class ElfLoader {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public readonly struct SectionHeader {
            //Section types (SHT_*)
            public const uint SHT_NULL = 0;
            public const uint SHT_PROGBITS = 1;
            public const uint SHT_SYMTAB = 2;
            public const uint SHT_STRTAB = 3;
            public const uint SHT_RELA = 4;
            public const uint SHT_HASH = 5;
            public const uint SHT_DYNAMIC = 6;
            public const uint SHT_NOTE = 7;
            public const uint SHT_NOBITS = 8;
            public const uint SHT_REL = 9;
            public const uint SHT_SHLIB = 10;
            public const uint SHT_DYNSYM = 11;

            //OS-specific
            public const uint SHT_LOOS = 0x60000000;
            public const uint SHT_HIOS = 0x6FFFFFFF;

            //Processor-specific
            public const uint SHT_LOPROC = 0x70000000;
            public const uint SHT_HIPROC = 0x7FFFFFFF;

            //Fields
            public readonly BigEndian<uint> sh_name;        /* section name */
            public readonly BigEndian<uint> sh_type;        /* section type */
            public readonly BigEndian<ulong> sh_flags;      /* section attributes */
            public readonly BigEndian<ulong> sh_addr;       /* virtual address in memory */
            public readonly BigEndian<ulong> sh_offset;     /* offset in file */
            public readonly BigEndian<ulong> sh_size;       /* size of section */
            public readonly BigEndian<uint> sh_link;        /* link to other section */
            public readonly BigEndian<uint> sh_info;        /* miscellaneous information */
            public readonly BigEndian<ulong> sh_addralign;  /* address alignment boundary */
            public readonly BigEndian<ulong> sh_entsize;    /* size of entries, if section has table */
        }
    }
}
