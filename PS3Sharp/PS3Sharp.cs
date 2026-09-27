using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Cell.PPU.PPURecompiler;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Memory_Manager;
using PS3Sharp.SFOLoader;

namespace PS3Sharp
{
    public sealed class PS3Sharp
    {

        // For now we share the file system with RPCS3
        public const string FileSystemBase = @"D:\RPCS3\";
        public const string ContentDir = @"\dev_hdd0\game\NPUB31790";
        public const string ELFDir = FileSystemBase + ContentDir + @"\USRDIR\EBOOT.elf";
        public const string SFODir = FileSystemBase + ContentDir + @"\PARAM.SFO";

        //public const string FileSystemBase = @"D:\RPCS3\"; 

        //public const string ContentDir = @"\test";
        //public const string ELFDir = FileSystemBase + ContentDir + @"\ppu_integer_shift.ppu.elf";
        //public const string SFODir = FileSystemBase + ContentDir + @"\PARAM.SFO";

        public static readonly SFO? SFO = SFO.Load(SFODir);
        public static readonly RealitySynthesizer RSX = RealitySynthesizer.Instance;

        public static void TerminateEmulator(int code) => Environment.Exit(code);

        public static void Main()
        {

            Console.WriteLine("PS3 lol");

            MemoryManager.Initialize();

            //RunSimpleElf(@"C:\Users\Old Snake\Desktop\Projects\Emudev\PS3\helloworld.elf"); // runs on recompiler
            //return;

            PPU ppu_main = CreateMainPPUThread(ELFDir);
            PRXManager.PRXManager.InitializeLibraries(ppu_main);
            RunEmu(ppu_main);
        }

        public static PPU CreateMainPPUThread(string eld_dir)
        {
            uint entryPointer = ElfLoader.LoadMainElf(eld_dir);
            PPU ppu_main = new("main_thread", entryPointer, path_to_executable: eld_dir);
            ThreadManager.CreateNativePPUThread(ppu_main);
            return ppu_main;
        }

        public static void RunEmu(PPU main)
        {
            ThreadManager.RunPPUThread(main);
            ThreadManager.JoinThread(main.NativeThread);  // If main_thread exits, terminate everything
            ThreadManager.StopAll();
        }

        public static void RunCellTestRom(string path)
        {
            // Load code at 0x1000000
            byte[] code = File.ReadAllBytes(path);
            MemoryManager.MemoryBlock code_block = MemoryManager.AllocateVirtualAddress(0x1000000, (uint)code.Length);
            MemoryManager.CopyBufferToVirtualMemory(code, code_block.VirtualAddress);

            // Create the PPU thread
            uint main_thread_id = ThreadManager.GetNewID();
            PPU ppu_main = new("main_thread", code_block.VirtualAddress, main_thread_id, MemoryManager.DEFAULT_STACK_SIZE);

            // Prepare arguments

            //R3: 0 (to bootstrap a constant value into the register file)
            ppu_main.WriteReg<ulong>(3, 0);


            // R4:pointer to a 32k (32768-byte) pre-cleared, cache-aligned scratch memory block (must not be address zero)
            MemoryManager.MemoryBlock r4_block = MemoryManager.AllocateVirtualMemory(32768);
            ppu_main.WriteReg<uint>(4, r4_block.VirtualAddress);

            //R5: pointer to a memory block into which failing instructions will be written (should be at least 64k to avoid overruns)
            MemoryManager.MemoryBlock r5_block = MemoryManager.AllocateVirtualMemory(2 * 32768);
            ppu_main.WriteReg<uint>(5, r5_block.VirtualAddress);

            uint failed_instructions_buffer = r5_block.VirtualAddress;

            while (true)
            {

                try
                {
                    PPUInterpreter.Step(ppu_main);
                }
                catch (Exception e)
                {

                    // Read "Fail" buffer

                    uint failed_instruction = MemoryManager.Read<uint>(failed_instructions_buffer);

                    while (failed_instruction != 0)
                    {
                        Console.WriteLine($"Detected Fail: 0x{failed_instruction:X8}");
                        failed_instructions_buffer += 32;
                        failed_instruction = MemoryManager.Read<uint>(failed_instructions_buffer);
                    }

                    Console.WriteLine($"Exception: {e.Message}");
                    Environment.Exit(1);
                }
            }
        }

        public static void RunSimpleElf(string path)
        {
            uint entryPointer = ElfLoader.LoadMainElf(path);

            // Create the PPU thread
            uint main_thread_id = ThreadManager.GetNewID();

            // ulong is probably wrong for PS3 ELFs, but correct for generic PPC64 ELFs?
            // uint is correct for PS3 elfs

            uint pc = MemoryManager.Read<uint>(entryPointer);
            uint toc = MemoryManager.Read<uint>(entryPointer + 4);

            PPU ppu_main = new("main_thread", (uint)pc, main_thread_id, MemoryManager.DEFAULT_STACK_SIZE);
            ppu_main.WriteReg<ulong>(2, toc);

            PPURecompiler ppu_recompiler = new();

            while (true)
            {
                try
                {
                    //PPUInterpreter.Step(ppu_main);
                    ppu_recompiler.RunPPU(ppu_main);
                }
                catch (Exception e)
                {

                    Console.WriteLine($"Exception: {e.Message}");
                    Console.WriteLine($"Trace: {e.StackTrace}");
                    Environment.Exit(1);
                }
            }
        }

        private static string ExtractContentDir(string fullPath)
        { //Not needed
            string directory = Path.GetDirectoryName(fullPath);

            // Split into folders
            string[] parts = directory.Split(Path.DirectorySeparatorChar);

            int devIndex = Array.FindIndex(parts, p =>
                p.StartsWith("dev_", StringComparison.OrdinalIgnoreCase));

            if (devIndex >= 0 && parts.Length > devIndex + 2)
            {
                // dev_hdd0 + game + TITLEID
                return @"\" + string.Join(@"\", parts, devIndex, 3) + @"\";
            }
            else
            {
                throw new Exception($"Invalid path: {fullPath}");
            }
        }
    }
}
