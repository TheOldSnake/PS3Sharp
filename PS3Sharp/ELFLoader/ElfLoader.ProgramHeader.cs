using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal partial class ElfLoader
    {
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ProgramHeader
        {
            //Segment types (PT_*)
            internal const uint PT_NULL = 0;
            internal const uint PT_LOAD = 1;
            internal const uint PT_DYNAMIC = 2;
            internal const uint PT_INTERP = 3;
            internal const uint PT_NOTE = 4;
            internal const uint PT_SHLIB = 5;
            internal const uint PT_PHDR = 6;
            internal const uint PT_TLS = 7;

            //OS-specific
            internal const uint PT_LOOS = 0x60000000;
            internal const uint PT_HIOS = 0x6FFFFFFF;

            //Processor-specific
            internal const uint PT_LOPROC = 0x70000000;
            internal const uint PT_HIPROC = 0x7FFFFFFF;

            //PS3-specific
            internal const uint PROC_PARAM = 0x60000001;
            internal const uint PRX_PARAM = 0x60000002;
            internal const uint SCE_PPURELA = 0x700000A4;

            //Fields
            internal readonly BigEndian<uint> p_type;      /* Segment type */
            internal readonly BigEndian<uint> p_flags;     /* Segment flags */
            internal readonly BigEndian<ulong> p_offset;   /* Segment file offset */
            internal readonly BigEndian<ulong> p_vaddr;    /* Segment virtual address */
            internal readonly BigEndian<ulong> p_paddr;    /* Segment physical address */
            internal readonly BigEndian<ulong> p_filesz;   /* Segment size in file */
            internal readonly BigEndian<ulong> p_memsz;    /* Segment size in memory */
            internal readonly BigEndian<ulong> p_align;    /* Segment alignment */

            internal static string GetName(ProgramHeader programHeader)
            {
                ulong type = programHeader.p_type;
                switch (type)
                {
                    case PT_NULL: return "PT_NULL";
                    case PT_LOAD: return "PT_LOAD";
                    case PT_DYNAMIC: return "PT_DYNAMIC";
                    case PT_INTERP: return "PT_INTERP";
                    case PT_NOTE: return "PT_NOTE";
                    case PT_SHLIB: return "PT_SHLIB";
                    case PT_PHDR: return "PT_PHDR";
                    case PT_TLS: return "PT_TLS";

                    case PROC_PARAM: return "PROC_PARAM";
                    case PRX_PARAM: return "PRX_PARAM";

                    default: return $"Unknown type: ({type})";
                }
            }
        }
    }
}
