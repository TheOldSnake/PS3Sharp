using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Cell.PPU.PPURecompiler;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Memory_Manager;
using PS3Sharp.SFOLoader;

namespace PS3Sharp
{
    internal sealed class PS3Sharp
    {

        // For now we share the file system with RPCS3
        internal const string FileSystemBase = @"D:\RPCS3\";
        internal const string ContentDir = @"\dev_hdd0\game\NPUB31790";
        internal const string ELFDir = FileSystemBase + ContentDir + @"\USRDIR\EBOOT.elf";
        internal const string SFODir = FileSystemBase + ContentDir + @"\PARAM.SFO";

        //internal const string FileSystemBase = @"D:\RPCS3\"; 

        //internal const string ContentDir = @"\test";
        //internal const string ELFDir = FileSystemBase + ContentDir + @"\ppu_integer_shift.ppu.elf";
        //internal const string SFODir = FileSystemBase + ContentDir + @"\PARAM.SFO";

        internal static readonly SFO? SFO = SFO.Load(SFODir);
        internal static void TerminateEmulator(int code) => Environment.Exit(code);

        internal static void Main()
        {
            using EmulatorRuntime emulator = new EmulatorRuntime();
            emulator.RunEmulator(ELFDir, false);
            //emulator.RunEmulator(@"C:\Users\Old Snake\Desktop\Projects\Emudev\PS3\helloworld.elf", true);
        }
    }
}
