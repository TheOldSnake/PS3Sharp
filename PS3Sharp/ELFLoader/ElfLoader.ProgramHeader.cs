using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal partial class ElfLoader
    {
        // Constants

        // Segment types (PT_*)
        internal const uint PtNull = 0;
        internal const uint PtLoad = 1;
        internal const uint PtDynamic = 2;
        internal const uint PtInterp = 3;
        internal const uint PtNote = 4;
        internal const uint PtShlib = 5;
        internal const uint PtPhdr = 6;
        internal const uint PtTls = 7;

        // OS-specific
        internal const uint PtLoos = 0x60000000;
        internal const uint PtHios = 0x6FFFFFFF;

        // Processor-specific
        internal const uint PtLoproc = 0x70000000;
        internal const uint PtHiproc = 0x7FFFFFFF;

        // PS3-specific
        internal const uint ProcParam = 0x60000001;
        internal const uint PrxParam = 0x60000002;
        internal const uint ScePpurela = 0x700000A4;

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ProgramHeader64
        {
            internal readonly BigEndian<uint> p_type;      /* Segment type */
            internal readonly BigEndian<uint> p_flags;     /* Segment flags */
            internal readonly BigEndian<ulong> p_offset;   /* Segment file offset */
            internal readonly BigEndian<ulong> p_vaddr;    /* Segment virtual address */
            internal readonly BigEndian<ulong> p_paddr;    /* Segment physical address */
            internal readonly BigEndian<ulong> p_filesz;   /* Segment size in file */
            internal readonly BigEndian<ulong> p_memsz;    /* Segment size in memory */
            internal readonly BigEndian<ulong> p_align;    /* Segment alignment */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct ProgramHeader32
        {
            internal readonly BigEndian<uint> p_type;      /* Segment type */
            internal readonly BigEndian<uint> p_offset;   /* Segment file offset */
            internal readonly BigEndian<uint> p_vaddr;    /* Segment virtual address */
            internal readonly BigEndian<uint> p_paddr;    /* Segment physical address */
            internal readonly BigEndian<uint> p_filesz;   /* Segment size in file */
            internal readonly BigEndian<uint> p_memsz;    /* Segment size in memory */
            internal readonly BigEndian<uint> p_flags;     /* Segment flags */
            internal readonly BigEndian<uint> p_align;    /* Segment alignment */
        }

        internal static string GetProgramHeaderName(ProgramHeader64 programHeader)
        {
            ulong type = programHeader.p_type;
            return PTypeToString(type);
        }

        internal static string GetProgramHeaderName(ProgramHeader32 programHeader)
        {
            ulong type = programHeader.p_type;
            return PTypeToString(type);
        }

        internal static string PTypeToString(ulong type)
        {
            switch (type)
            {
                case PtNull: return "PT_NULL";
                case PtLoad: return "PT_LOAD";
                case PtDynamic: return "PT_DYNAMIC";
                case PtInterp: return "PT_INTERP";
                case PtNote: return "PT_NOTE";
                case PtShlib: return "PT_SHLIB";
                case PtPhdr: return "PT_PHDR";
                case PtTls: return "PT_TLS";

                case ProcParam: return "PROC_PARAM";
                case PrxParam: return "PRX_PARAM";

                default: return $"Unknown type: ({type})";
            }
        }
    }
}
