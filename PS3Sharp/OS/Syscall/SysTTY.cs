using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysTTY
    {
        internal const uint STDOUT = 1;
        internal const uint STDERR = 2;
        private readonly MemoryManager _memoryManager;

        // private StreamWriter output = File.AppendText(@"D:\RPCS3\app_home\output.txt");

        internal SysTTY(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        private void Print(string s, ConsoleColor color)
        {
            Console.Write(s);
            //output.Write(s);
        }

        internal ulong SysTTYWrite(PPUThread ppu)
        {

            uint ch = ppu.ReadArg<uint>(ARG0);                               //channel? probably file descriptor
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG1);              //void*
            uint len = ppu.ReadArg<uint>(ARG2);                              //uint32_t
            GuestPtr<uint> pwritelen = ppu.ReadArg<GuestPtr<uint>>(ARG3);    //uint32_t*

            string text = _memoryManager.ReadString(buf);
            ConsoleColor color = ch == STDOUT ? ConsoleColor.Green : ConsoleColor.Red;
            Print(text, color);

            return CELL_OK;
        }
    }
}
