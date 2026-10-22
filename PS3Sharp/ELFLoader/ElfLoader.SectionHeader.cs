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
            internal const uint ShtNull = 0;
            internal const uint ShtProgbits = 1;
            internal const uint ShtSymtab = 2;
            internal const uint ShtStrtab = 3;
            internal const uint ShtRela = 4;
            internal const uint ShtHash = 5;
            internal const uint ShtDynamic = 6;
            internal const uint ShtNote = 7;
            internal const uint ShtNobits = 8;
            internal const uint ShtRel = 9;
            internal const uint ShtShlib = 10;
            internal const uint ShtDynsym = 11;

            //OS-specific
            internal const uint ShtLoos = 0x60000000;
            internal const uint ShtHios = 0x6FFFFFFF;

            //Processor-specific
            internal const uint ShtLoproc = 0x70000000;
            internal const uint ShtHiproc = 0x7FFFFFFF;

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
