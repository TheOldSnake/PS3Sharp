using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysTTY
    {
        internal const uint Stdout = 1;
        internal const uint Stderr = 2;
        private readonly MemoryManager _memoryManager;

        // private StreamWriter output = File.AppendText();

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

            uint ch = ppu.ReadArg<uint>(Arg0);                               //channel? probably file descriptor
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(Arg1);              //void*
            uint len = ppu.ReadArg<uint>(Arg2);                              //uint32_t
            GuestPtr<uint> pwritelen = ppu.ReadArg<GuestPtr<uint>>(Arg3);    //uint32_t*

            string text = _memoryManager.ReadString(buf);
            ConsoleColor color = ch == Stdout ? ConsoleColor.Green : ConsoleColor.Red;
            Print(text, color);

            return CellOk;
        }
    }
}