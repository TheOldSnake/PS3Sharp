using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Modules;
using PS3Sharp.OS.NID;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.ElfLoader;
using static PS3Sharp.PRXManager.PRXManager;

namespace PS3Sharp.PRXManager {
    public static class PRXManager {    // todo

        // List of function that are forced to be HLE
        public static List<string> ForceHLE = [
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
        public static readonly List<string> PRXList = [
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
        public static List<LibraryInfo> LoadedLibraries = [];

        // List of library initialization functions to be called at startup
        public static List<LibraryInitialization> LibraryInitializations = [];
        
        public struct LibraryInitialization {
            public string LibName;
            public uint Prologue;
            public uint Epilogue;
            public uint Start;
            public uint Stop;
        }

        public static Dictionary<uint, uint> ExportedFunctions = new();


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public readonly struct LibraryInfo : IPS3Struct {
            public readonly BigEndian<ushort> Attributes;
            public readonly InlineData.InlineArray2<byte> VerInfo;
            public readonly InlineData.InlineArray28<byte> Name;
            public readonly BigEndian<uint> TOC;

            // Exports
            public readonly BigEndian<uint> ExportsStart; 
            public readonly BigEndian<uint> ExportsEnd; 

            // Imports
            public readonly BigEndian<uint> ImportsStart; 
            public readonly BigEndian<uint> ImportsEnd; 
        }


        // Export or import module struct
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public readonly struct PRXModuleInfo : IPS3Struct {
            public readonly byte Size;
            public readonly byte Unk0;
            public readonly BigEndian<ushort> Version;
            public readonly BigEndian<ushort> Attributes;
            public readonly BigEndian<ushort> FunctionCount;
            public readonly BigEndian<ushort> VariablesCount;
            public readonly BigEndian<ushort> TLSVariablesCount;
            public readonly byte HashInfo;
            public readonly byte TLSHashInfo;
            public readonly InlineData.InlineArray2<byte> Unk1;

            public readonly BigEndian<uint> Name_Ptr;
            public readonly BigEndian<uint> NIDs_Ptr; 
            public readonly BigEndian<uint> Addresses_Ptr;

            //what are these for?
            public readonly BigEndian<uint> VNIDs_Ptr;
            public readonly BigEndian<uint> VStubs_Ptr;

            public readonly BigEndian<uint> Unk4;
            public readonly BigEndian<uint> Unk5;
        }

        public static void LoadPRXLibraries() {
            // Load all segments
            for (int i = 0; i < PRXList.Count; i++) {
                string name = PRXList[i];
                LibraryInfo lib = LoadLibrary(name);
                LoadedLibraries.Add(lib);
            }

            // Handle All exports
            for (int i = 0; i < LoadedLibraries.Count; i++) {
                HandleExports(LoadedLibraries[i]);
            }

            // Handle All imports
            for (int i = 0; i < LoadedLibraries.Count; i++) {
                HandleImports(LoadedLibraries[i]);
            }
            
            //throw new Exception("done");
        }

        private static LibraryInfo LoadLibrary(string name) {
            string path = PS3Sharp.FileSystemBase + @"dev_flash\sys\external\" + name + ".prx";
            if (!File.Exists(path)) {
                throw new FileNotFoundException($"Can't find file: {path}");
            }

            BinaryReader reader = new BinaryReader(File.OpenRead(path));

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

        private static void HandleExports(LibraryInfo lib_info) {
            //Console.WriteLine($"Exports start = 0x{lib_info.ExportsStart:X8}");
            //Console.WriteLine($"Exports end = 0x{lib_info.ExportsEnd:X8}");

            LibraryInitialization libraryInitialization = default;

            string libname = GetLibName(lib_info);
            libraryInitialization.LibName = libname;

            uint start = lib_info.ExportsStart;
            uint end = lib_info.ExportsEnd;
            uint currentAddress = start;

            while (currentAddress < end) {
                PRXModuleInfo module_info = MemoryManager.ReadStruct<PRXModuleInfo>(currentAddress);
                uint size = (uint)(module_info.Size > 0 ? module_info.Size : Unsafe.SizeOf<PRXModuleInfo>());

                if (module_info.Name_Ptr != 0) {
                    string name = MemoryManager.ReadString(module_info.Name_Ptr);
                }

                uint nid_table = module_info.NIDs_Ptr;
                uint address_table = module_info.Addresses_Ptr;

                for (uint i = 0; i < module_info.FunctionCount; i++) {
                    uint offset = i * sizeof(uint);
                    uint nid = MemoryManager.Read<uint>(nid_table + offset);
                    uint address = MemoryManager.Read<uint>(address_table + offset);

                    
                    // Special module
                    if ((module_info.Attributes & 0x8000) > 0) {

                        switch (nid) {
                            case 0x0d10fd3f: libraryInitialization.Prologue = address; break;
                            case 0x330f7005: libraryInitialization.Epilogue = address; break;
                            case 0xab779874: libraryInitialization.Stop = address; break;
                            case 0xbc9a0086: libraryInitialization.Start = address; break;
                        }
                        
                    } else {
                        string function_name;
                        if (NIDManager.NIDList.TryGetValue(nid, out string? value)) {
                             function_name = value;
                        } else {
                             function_name = "unknown";
                        }

                        if (!ForceHLE.Contains(function_name)) {
                            ExportedFunctions.Add(nid, address);
                            Console.WriteLine($"[{libname}] export at address: 0x{address:X8} function name = {function_name}");
                        }
                    }

                }

                currentAddress += size;
            }

            LibraryInitializations.Add(libraryInitialization);
        }

        private static void HandleImports(LibraryInfo lib_info) {
            //Console.WriteLine($"Imports start = 0x{lib_info.ImportsStart:X8}");
            //Console.WriteLine($"Imports end = 0x{lib_info.ImportsEnd:X8}");

            string libname = GetLibName(lib_info);

            uint start = lib_info.ImportsStart;
            uint end = lib_info.ImportsEnd;
            uint currentAddress = start;

            while (currentAddress < end) {
                PRXModuleInfo module_info = MemoryManager.ReadStruct<PRXModuleInfo>(currentAddress);
                uint size = (uint)(module_info.Size > 0 ? module_info.Size : Unsafe.SizeOf<PRXModuleInfo>());

                string moduleName = MemoryManager.ReadString(module_info.Name_Ptr);

                uint nid_table = module_info.NIDs_Ptr;
                uint address_table = module_info.Addresses_Ptr;

                for (uint i = 0; i < module_info.FunctionCount; i++) {
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

        public static void InitializeLibraries(PPU ppu) {
            uint pc;

            for (int i = 0; i < LibraryInitializations.Count; i++) {
                LibraryInitialization libraryInitialization = LibraryInitializations[i];

                if (libraryInitialization.LibName == "liblv2") {
                    continue;
                }

                Console.WriteLine($"Initializing lib: {libraryInitialization.LibName}");
                if (libraryInitialization.Prologue != 0) {
                    PPUInterpreter.ExecuteFunction(ppu, libraryInitialization.Prologue, toc: true, []);
                }

                if (libraryInitialization.Start != 0) {
                    PPUInterpreter.ExecuteFunction(ppu, libraryInitialization.Start, toc: true, []);
                }
            }
        }

        public static string GetLibName(LibraryInfo lib_info) {
            ReadOnlySpan<byte> name_buffer = lib_info.Name;
            int name_end = name_buffer.IndexOf((byte)0);
            return Encoding.UTF8.GetString(name_buffer[..name_end]);
        }
    }
}
