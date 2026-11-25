using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using System.Runtime.CompilerServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.Syscall.SysFs;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellFs
    {
        private readonly MemoryManager _memoryManager;
        private readonly SysFs _sysFs;

        internal CellFs(MemoryManager memoryManager, SysFs sysFs)
        {
            _memoryManager = memoryManager;
            _sysFs = sysFs;
        }

        // Dispatch the calls to sysFs handler
        internal ulong CellFsAioInit(PPUThread ppu) => _sysFs.SysFsAioInit(ppu);

        internal ulong CellFsStat(PPUThread ppu) => _sysFs.SysFsStat(ppu);

        internal ulong CellFsFstat(PPUThread ppu) => _sysFs.SysFsFstat(ppu);

        internal ulong CellFsOpen(PPUThread ppu) => _sysFs.SysFsOpen(ppu);

        internal ulong CellFsLseek(PPUThread ppu) => _sysFs.SysFsLseek(ppu);

        internal ulong CellFsRead(PPUThread ppu) => _sysFs.SysFsRead(ppu);  // Note: There is a slight difference

        internal ulong CellFsWrite(PPUThread ppu) => _sysFs.SysFsWrite(ppu);

        internal ulong CellFsClose(PPUThread ppu) => _sysFs.SysFsClose(ppu);

        internal ulong CellFsMakedir(PPUThread ppu) => _sysFs.SysFsMkDir(ppu);

        internal ulong CellFsReaddir(PPUThread ppu) => _sysFs.SysFsReadDir(ppu);

        internal ulong CellFsOpenDir(PPUThread ppu) => _sysFs.SysFsOpenDir(ppu);

        internal ulong CellFsCloseDir(PPUThread ppu) => _sysFs.SysFsCloseDir(ppu);

        internal ulong CellFsGetFreeSize(PPUThread ppu)
        {
            GuestVoidPtr path_ptr = ppu.ReadArg<GuestVoidPtr>(Arg0);
            GuestPtr<BigEndian<uint>> block_size_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);
            GuestPtr<BigEndian<ulong>> block_cnt_ptr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg2);

            _memoryManager.Write<BigEndian<uint>>(block_size_ptr, 4096);
            _memoryManager.Write<BigEndian<ulong>>(block_cnt_ptr, 5242880); // 20gb / 4096

            return CellOk;
        }

        internal ulong CellFsGetDirectoryEntries(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            GuestPtr<CellFsDirent> direentPtr = ppu.ReadArg<GuestPtr<CellFsDirent>>(Arg1);
            int size = ppu.ReadArg<int>(Arg2);
            GuestPtr<int> dataCountPtr = ppu.ReadArg<GuestPtr<int>>(Arg3);

            int entriesCount = size / Unsafe.SizeOf<CellFsDirent>();
            int readCount = 0;
            uint offset = 0;

            for (int i = 0; i < entriesCount; i++)
            {
                ulong bytesRead = _sysFs.FileSystemManager.ReadDirectory(fd, out CellFsDirent dirent);

                // Break if we reached the end
                if (bytesRead == 0)
                {
                    break;
                }

                _memoryManager.Write(direentPtr + offset, dirent);
                offset += (uint)bytesRead;
                readCount++;
            }

            _memoryManager.Write(dataCountPtr, readCount);
            return CellOk;
        }
    }
}