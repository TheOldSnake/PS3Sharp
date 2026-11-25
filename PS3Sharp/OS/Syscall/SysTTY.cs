using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysTTY
    {
        internal const uint Stdout = 1;
        internal const uint Stderr = 2;
        private readonly MemoryManager _memoryManager;
        private readonly StreamWriter _ttyOutputFile = File.AppendText("tty.txt");
        private static readonly bool LogTTY = false;    // TODO: Make it configurable

        internal SysTTY(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysTTYWrite(PPUThread ppu)
        {

            uint channel = ppu.ReadArg<uint>(Arg0); // TODO
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(Arg1);
            int len = ppu.ReadArg<int>(Arg2);    
            GuestPtr<BigEndian<int>> pwritelen = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg3);

            string text = _memoryManager.ReadString(buf, len);

            if (LogTTY)
            {
                _ttyOutputFile.Write(text);
            }
            else
            {
                Console.Write(text);
            }

            _memoryManager.Write<BigEndian<int>>(pwritelen, len);
            return CellOk;
        }
    }
}