using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Modules;
using PS3Sharp.OS.NID;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.ElfLoader;

namespace PS3Sharp.PRXManager
{
    internal static class PRXManager
    {    // todo

        // List of function that are forced to be HLE
        internal static List<string> ForceHLE = [
            "sys_process_exit",
            "sys_initialize_tls",

            "_sys_process_atexitspawn",
            "_sys_process_at_Exitspawn",

            "sys_time_get_system_time",

           // "_sys_malloc",
           // "_sys_free",

            "sys_lwmutex_lock",
            "sys_lwmutex_unlock",
            "sys_lwmutex_create",
            "sys_lwmutex_trylock",
            "sys_lwmutex_destroy",

            "sys_ppu_thread_create",
            "sys_ppu_thread_get_id",
            "sys_ppu_thread_once",
            "sys_ppu_thread_exit",

            "sys_spu_image_import",
        ];

        // List of LLEd libraries file names
        internal static readonly List<string> PRXList = [
            "liblv2",
            "libsync2",
            "libfiber",
            "libl10n",

            "libfont",
            "libfontFT",
            "libfreetype",

            "libsre",    // Needed for probably every game in existence other than arkedo and gta
            "libjpgdec", // Needed for Minecraft
            "libpngdec"  // Needed for Minecraft

           // "libgcm_sys"
        ];

        // List of loaded libraries
        internal static List<LibraryInfo> LoadedLibraries = [];

        // List of library initialization functions to be called at startup
        internal static List<LibraryInitialization> LibraryInitializations = [];

        internal struct LibraryInitialization
        {
            internal string LibName;
            internal uint Prologue;
            internal uint Epilogue;
            internal uint Start;
            internal uint Stop;
        }

        internal static Dictionary<uint, uint> ExportedFunctions = new();


        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct LibraryInfo : IPS3Struct
        {
            internal readonly BigEndian<ushort> Attributes;
            internal readonly InlineData.InlineArray2<byte> VerInfo;
            internal readonly InlineData.InlineArray28<byte> Name;
            internal readonly BigEndian<uint> TOC;

            // Exports
            internal readonly BigEndian<uint> ExportsStart;
            internal readonly BigEndian<uint> ExportsEnd;

            // Imports
            internal readonly BigEndian<uint> ImportsStart;
            internal readonly BigEndian<uint> ImportsEnd;
        }


        // Export or import module struct
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct PRXModuleInfo : IPS3Struct
        {
            internal readonly byte Size;
            internal readonly byte Unk0;
            internal readonly BigEndian<ushort> Version;
            internal readonly BigEndian<ushort> Attributes;
            internal readonly BigEndian<ushort> FunctionCount;
            internal readonly BigEndian<ushort> VariablesCount;
            internal readonly BigEndian<ushort> TLSVariablesCount;
            internal readonly byte HashInfo;
            internal readonly byte TLSHashInfo;
            internal readonly InlineData.InlineArray2<byte> Unk1;

            internal readonly BigEndian<uint> Name_Ptr;
            internal readonly BigEndian<uint> NIDs_Ptr;
            internal readonly BigEndian<uint> Addresses_Ptr;

            //what are these for?
            internal readonly BigEndian<uint> VNIDs_Ptr;
            internal readonly BigEndian<uint> VStubs_Ptr;

            internal readonly BigEndian<uint> Unk4;
            internal readonly BigEndian<uint> Unk5;
        }

        internal static void LoadPRXLibraries()
        {
            // Load all segments
            for (int i = 0; i < PRXList.Count; i++)
            {
                string name = PRXList[i];
                LibraryInfo lib = LoadLibrary(name);
                LoadedLibraries.Add(lib);
            }

            // Handle All exports
            for (int i = 0; i < LoadedLibraries.Count; i++)
            {
                HandleExports(LoadedLibraries[i]);
            }

            // Handle All imports
            for (int i = 0; i < LoadedLibraries.Count; i++)
            {
                HandleImports(LoadedLibraries[i]);
            }

            //throw new Exception("done");
        }

        private static LibraryInfo LoadLibrary(string name)
        {
            string path = PS3Sharp.FileSystemBase + @"dev_flash\sys\external\" + name + ".prx";
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Can't find file: {path}");
            }

            BinaryReader reader = new(File.OpenRead(path));

            // Load the loadable segments
            List<LoadedPRXSegment> loadedSegments = ElfLoader.LoadPRXElf(reader);

            // From PS3DevWiki: The Module Info is at offset Elf32_Phdrs[text_seg_id].p_paddr - Elf32_Phdrs[text_seg_id].p_offset
            LoadedPRXSegment text = loadedSegments[0];
            uint address = (uint)(text.ActualEntry + text.ProgramHeader.p_paddr - text.ProgramHeader.p_offset);
            LibraryInfo library_info = MemoryManager.ReadStruct<LibraryInfo>(address);

            return library_info;
            // HandleExports(library_info);
            // HandleImports(library_info);
        }

        private static void HandleExports(LibraryInfo lib_info)
        {
            //Console.WriteLine($"Exports start = 0x{lib_info.ExportsStart:X8}");
            //Console.WriteLine($"Exports end = 0x{lib_info.ExportsEnd:X8}");

            LibraryInitialization libraryInitialization = default;

            string libname = GetLibName(lib_info);
            libraryInitialization.LibName = libname;

            uint start = lib_info.ExportsStart;
            uint end = lib_info.ExportsEnd;
            uint currentAddress = start;

            while (currentAddress < end)
            {
                PRXModuleInfo module_info = MemoryManager.ReadStruct<PRXModuleInfo>(currentAddress);
                uint size = (uint)(module_info.Size > 0 ? module_info.Size : Unsafe.SizeOf<PRXModuleInfo>());

                if (module_info.Name_Ptr != 0)
                {
                    string name = MemoryManager.ReadString(module_info.Name_Ptr);
                }

                uint nid_table = module_info.NIDs_Ptr;
                uint address_table = module_info.Addresses_Ptr;

                for (uint i = 0; i < module_info.FunctionCount; i++)
                {
                    uint offset = i * sizeof(uint);
                    uint nid = MemoryManager.Read<uint>(nid_table + offset);
                    uint address = MemoryManager.Read<uint>(address_table + offset);


                    // Special module
                    if ((module_info.Attributes & 0x8000) > 0)
                    {

                        switch (nid)
                        {
                            case 0x0d10fd3f: libraryInitialization.Prologue = address; break;
                            case 0x330f7005: libraryInitialization.Epilogue = address; break;
                            case 0xab779874: libraryInitialization.Stop = address; break;
                            case 0xbc9a0086: libraryInitialization.Start = address; break;
                        }

                    }
                    else
                    {
                        string function_name;
                        if (NIDManager.NIDList.TryGetValue(nid, out string? value))
                        {
                            function_name = value;
                        }
                        else
                        {
                            function_name = "unknown";
                        }

                        if (!ForceHLE.Contains(function_name))
                        {
                            ExportedFunctions.Add(nid, address);
                            Console.WriteLine($"[{libname}] export at address: 0x{address:X8} function name = {function_name}");
                        }
                    }

                }

                currentAddress += size;
            }

            LibraryInitializations.Add(libraryInitialization);
        }

        private static void HandleImports(LibraryInfo lib_info)
        {
            //Console.WriteLine($"Imports start = 0x{lib_info.ImportsStart:X8}");
            //Console.WriteLine($"Imports end = 0x{lib_info.ImportsEnd:X8}");

            string libname = GetLibName(lib_info);

            uint start = lib_info.ImportsStart;
            uint end = lib_info.ImportsEnd;
            uint currentAddress = start;

            while (currentAddress < end)
            {
                PRXModuleInfo module_info = MemoryManager.ReadStruct<PRXModuleInfo>(currentAddress);
                uint size = (uint)(module_info.Size > 0 ? module_info.Size : Unsafe.SizeOf<PRXModuleInfo>());

                string moduleName = MemoryManager.ReadString(module_info.Name_Ptr);

                uint nid_table = module_info.NIDs_Ptr;
                uint address_table = module_info.Addresses_Ptr;

                for (uint i = 0; i < module_info.FunctionCount; i++)
                {
                    uint offset = i * sizeof(uint);

                    uint nid = MemoryManager.Read<uint>(nid_table + offset);
                    uint address = MemoryManager.Read<uint>(address_table + offset);

                    string functionName = NIDManager.NIDList.GetValueOrDefault(nid, "unknown_function");
                    ModulePatcher.PatchModuleCallHLE(moduleName, functionName, nid, address);

                    Console.WriteLine($"[{libname}] import at address: 0x{address:X8} function: {functionName}");
                }

                currentAddress += size;
            }
        }

        internal static void InitializeLibraries(PPU ppu)
        {
            uint pc;

            for (int i = 0; i < LibraryInitializations.Count; i++)
            {
                LibraryInitialization libraryInitialization = LibraryInitializations[i];

                if (libraryInitialization.LibName == "liblv2")
                {
                    continue;
                }

                Console.WriteLine($"Initializing lib: {libraryInitialization.LibName}");
                if (libraryInitialization.Prologue != 0)
                {
                    PPUInterpreter.ExecuteFunction(ppu, libraryInitialization.Prologue, toc: true, []);
                }

                if (libraryInitialization.Start != 0)
                {
                    PPUInterpreter.ExecuteFunction(ppu, libraryInitialization.Start, toc: true, []);
                }
            }
        }

        internal static string GetLibName(LibraryInfo lib_info)
        {
            ReadOnlySpan<byte> name_buffer = lib_info.Name;
            int name_end = name_buffer.IndexOf((byte)0);
            return Encoding.UTF8.GetString(name_buffer[..name_end]);
        }
    }
}
