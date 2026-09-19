using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall {
    public static class SysTTY {
        private static ConsoleColor[] Colors = [
            //todo
        ];

        public const uint STDOUT = 1;
        public const uint STDERR = 2;

        //static StreamWriter output = File.AppendText(@"D:\RPCS3\app_home\output.txt");
        private static void Print(string s, ConsoleColor color) {
            //  ConsoleColor old = Console.ForegroundColor;
            //  Console.ForegroundColor = color;

              Console.Write(s);

            //  Console.ForegroundColor = old;
            //output.Write(s);
        }

        public static uint SysTTYWrite(PPU ppu) {

            uint ch = ppu.ReadArg<uint>(ARG0);                               //channel? probably file descriptor
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG1);              //void*
            uint len = ppu.ReadArg<uint>(ARG2);                              //uint32_t
            GuestPtr<uint> pwritelen = ppu.ReadArg<GuestPtr<uint>>(ARG3);    //uint32_t*

            //Console.WriteLine($"sys_tty_write ch = {ch} buf = 0x{buf:X8} len = {len} pwritelen = {pwritelen:X8}");

            string text = MemoryManager.ReadString(buf);
            ConsoleColor color = ch == STDOUT ? ConsoleColor.Green : ConsoleColor.Red;
            Print(text, color);

            return CELL_OK;
        }
    }
}
