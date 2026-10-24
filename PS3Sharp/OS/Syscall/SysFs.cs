using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.FileSystem.FileSystemManager;

namespace PS3Sharp.OS.Syscall
{
    // File System Syscalls

    internal class SysFs
    {
        // This struct is explicitly aligned to 4 bytes
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct CellFsStat : IPS3Struct
        {
            internal BigEndian<int> Mode;          // s32
            internal BigEndian<int> UId;           // s32
            internal BigEndian<int> GId;           // s32
            internal BigEndian<ulong> AccessTime;  // u64
            internal BigEndian<ulong> ModifyTime;  // u64
            internal BigEndian<ulong> CreateTime;  // u64
            internal BigEndian<ulong> Size;        // u64
            internal BigEndian<ulong> BlkSize;     // u64
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellFsDirent : IPS3Struct
        {
            internal byte Type;
            internal byte NameLength;
            internal InlineData.InlineArray256<byte> Name;
        }

        // Same as Linux (uses base 8)
        internal static readonly uint CellFsSIfdir = Oct("0040000"); // Directory
        internal static readonly uint CellFsSIfreg = Oct("0100000"); // Regular
        internal static readonly uint CellFsSiflnk = Oct("0120000"); // Symbolic link
        internal static readonly uint CellFsSifwht = Oct("0160000"); // Unknown

        internal static readonly uint CellFsSIrusr = Oct("0000400"); // R for owner
        internal static readonly uint CellFsSIwusr = Oct("0000200"); // W for owner
        internal static readonly uint CellFsSIxusr = Oct("0000100"); // X for owner

        internal static readonly uint CellFsSIrgrp = Oct("0000040"); // R for group
        internal static readonly uint CellFsSIwgrp = Oct("0000020"); // W for group
        internal static readonly uint CellFsSIxgrp = Oct("0000010"); // X for group

        internal static readonly uint CellFsSIroth = Oct("0000004"); // R for other
        internal static readonly uint CellFsSIwoth = Oct("0000002"); // W for other
        internal static readonly uint CellFsSIxoth = Oct("0000001"); // X for other

        // Default Modes
        internal static readonly uint DirMode = Oct("0777") | CellFsSIfdir;   // RWX RWX RWX
        internal static readonly uint FileMode = Oct("0666") | CellFsSIfreg;   // RW- RW- RW-

        // SysFsOpen flags
        internal static readonly uint ORdonly = Oct("0000");              // Read only
        internal static readonly uint OWronly = Oct("0001");              // Write only
        internal static readonly uint ORdwr = Oct("0002");                // Read/write

        internal static readonly uint OCreat = Oct("0100");          // Create file if it does not exist
        internal static readonly uint OExcl = Oct("0200");           // Exclusive create
        internal static readonly uint ONoctty = Oct("0400");         // Do not assign controlling terminal
        internal static readonly uint OTrunc = Oct("01000");         // Truncate file
        internal static readonly uint OAppend = Oct("02000");        // Append on each write
        internal static readonly uint ONonblock = Oct("04000");      // Non-blocking I/O

        internal static readonly uint ODsync = Oct("010000");        // Synchronized writes (data only)
        internal static readonly uint OSync = Oct("04010000");       // Synchronized writes (data + metadata)
        internal readonly uint ORsync = OSync;

        internal static readonly uint ODirectory = Oct("0200000");   // Must be a directory
        internal static readonly uint ONofollow = Oct("0400000");    // Do not follow symlinks
        internal static readonly uint OCloexec = Oct("02000000");    // Close on exec

        // Same as linux?
        internal const uint FdStdin = 0;
        internal const uint FdStdout = 1;
        internal const uint FdStderr = 2;
        internal const uint FdEof = 4;

        // For the console
        private static CellFsStat ConsoleFsStat = new()
        {
            Mode = (int)(CellFsSIrusr | CellFsSIwusr | CellFsSIxusr |
                         CellFsSIrgrp | CellFsSIwgrp | CellFsSIxgrp |
                         CellFsSIroth | CellFsSIwoth | CellFsSIxoth |
                         CellFsSIfreg),

            UId = 0,
            GId = 0,
            AccessTime = 0,
            ModifyTime = 0,
            CreateTime = 0,
            Size = 0,
            BlkSize = 4096
        };

        private readonly MemoryManager _memoryManager;
        private readonly FileSystemManager _fileSystemManager;

        private static uint Oct(string v) => Convert.ToUInt32(v, 8);   // Helper (avoid using it at runtime)

        internal SysFs(MemoryManager memoryManager, FileSystemManager fileSystemManager)
        {
            _memoryManager = memoryManager;
            _fileSystemManager = fileSystemManager;
        }

        internal ulong SysFsAioInit(PPUThread ppu)
        {
            Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsAioInit");
            return CellOk;
        }

        internal uint SysFsStat(PPUThread ppu)
        {
            uint pathPtr = ppu.ReadArg<uint>(Arg0);
            GuestPtr<CellFsStat> statPtr = ppu.ReadArg<GuestPtr<CellFsStat>>(Arg1);

            if (pathPtr == 0)
            {
                return CellEFault;
            }

            string guestPath = _memoryManager.ReadString(pathPtr);

            // ?
            if (string.IsNullOrEmpty(guestPath))
            {
                return CellENoEnt;
            }

            // TODO
            if (!IsGuestAbsolutePath(guestPath))
            {
                throw new Exception($"path is not absolute -- path = {guestPath}");
            }

            string hostPath = GetHostPath(guestPath);
            bool isDirectory = IsDirectory(hostPath);
            bool isFile = IsFile(hostPath);

            if (!isDirectory && !isFile)
            {
                Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFstat file: {guestPath} does not exist");
                return CellENoEnt;
            }

            CellFsStat stat = new()
            {
                Mode = (int)(isDirectory ? DirMode : FileMode),
                UId = 0,
                GId = 0,
                AccessTime = 0,
                ModifyTime = 0,
                CreateTime = 0,
                Size = isDirectory ? GetDirectorySize(hostPath) : GetFileSize(hostPath),
                BlkSize = 4096
            };

            _memoryManager.Write(statPtr, stat);

            return CellOk;
        }

        internal ulong SysFsFstat(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            GuestPtr<CellFsStat> statPtr = ppu.ReadArg<GuestPtr<CellFsStat>>(Arg1);

            bool isConsole = fd == FdStdin || fd == FdStdout || fd == FdStderr;

            // Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsFstat fd = 0x{fd:X}");

            if (isConsole)
            {
                _memoryManager.Write(statPtr, ConsoleFsStat);
                Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsFstat => CELL_EBADF");
                return CellEBadF;
            }

            ulong size;
            int mode;

            if (_fileSystemManager.TryGetFile(fd, out GuestFile? guestFile))
            {
                size = guestFile.Size;
                mode = (int)FileMode;
            }
            else if (_fileSystemManager.TryGetDirectory(fd, out GuestDirectory? directory))
            {
                size = directory.Size;
                mode = (int)DirMode;
            }
            else
            {
                Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsFstat => CELL_EBADF");
                return CellEBadF;
            }

            CellFsStat stat = new()
            {
                Mode = mode,
                UId = 0,
                GId = 0,
                AccessTime = 0,
                ModifyTime = 0,
                CreateTime = 0,
                Size = size,
                BlkSize = 4096
            };

            _memoryManager.Write(statPtr, stat);

            return CellOk;
        }

        internal ulong SysFsOpen(PPUThread ppu)
        {
            uint pathPtr = ppu.ReadArg<uint>(Arg0);
            uint flags = ppu.ReadArg<uint>(Arg1);
            GuestPtr<BigEndian<int>> fdPtr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg2);

            // uint64_t mode, const void *arg, uint64_t size <- ARG3/4/5
            // the mode is only in the syscall not the library

            string guestPath = _memoryManager.ReadString(pathPtr);

            if (!IsGuestAbsolutePath(guestPath))
            {
                Console.WriteLine($"[{ppu.ThreadName}] [SysFsOpen] Error, path: {guestPath} is not absolute (CELL_ENOENT)");
                return CellENoEnt;
            }

            string hostPath = GetHostPath(guestPath);

            if (IsFile(hostPath))
            {
                if (IsEncryptedFile(hostPath))
                {
                    if (HasBeenDecrypted(hostPath))
                    {
                        hostPath += ".unedat";
                    }
                    else
                    {
                        throw new Exception($"Need to decrypt: {hostPath}");
                    }
                }
            }

            // Open file
            int handle = (int)_memoryManager.GetHandle();
            ulong result = _fileSystemManager.OpenFile(hostPath, handle, ppu.ThreadID, flags);

            if (result != CellOk)
            {
                Console.WriteLine($"[{ppu.ThreadName}] [SysFsOpen] path: {guestPath} failed: {CellErrorNames[result]}");
            }
            else
            {
                 Console.WriteLine($"[{ppu.ThreadName}] [SysFsOpen] cellFsOpen opened file {guestPath} fd = 0x{handle:X} flags = 0x{flags}");
                // Write the file descriptor
                _memoryManager.Write<BigEndian<int>>(fdPtr, handle);
            }

            return result;
        }

        internal ulong SysFsLseek(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            long offset = ppu.ReadArg<long>(Arg1);
            int whence = ppu.ReadArg<int>(Arg2);
            GuestPtr<BigEndian<ulong>> posPtr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg3);

            // Console.WriteLine($"[{ppu.ThreadName}] [SysFsLseek] cellFsLseek fd = 0x{fd:X} seek offset: 0x{offset:X} mode = {whence}");

            if (!_fileSystemManager.IsFileOpen(fd))
            {
                return CellEInval;
            }

            ulong position = (ulong)_fileSystemManager.Seek(fd, offset, whence);
            _memoryManager.Write<BigEndian<ulong>>(posPtr, position);

            return CellOk;
        }

        internal ulong SysFsRead(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(Arg1);
            ulong nbytes = ppu.ReadArg<ulong>(Arg2);
            GuestPtr<BigEndian<ulong>> nreadPtr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg3);

            // MemoryManager.MemoryBlock block = _memoryManager.GetAllocationByVirtualAddress(buf);
            // Console.WriteLine($"[{ppu.ThreadName}] [SysFsRead] cellFsRead fd = 0x{fd:X} amount: 0x{nbytes:X} dest: 0x{buf:X} base = 0x{b.VirtualAddress:X8}");

            if (buf.IsNullPtr || nreadPtr.IsNullPtr || nbytes == 0)
            {
                return CellEFault;
            }

            // Read into the buffer
            ulong readCount = _fileSystemManager.Read(fd, nbytes, buf);                                  

            // Write back the number of bytes read
            if (nreadPtr > 0)
            {
                _memoryManager.Write<BigEndian<ulong>>(nreadPtr, readCount);
            }

            // TODO: Seems like this syscall returns CELL_EFAULT if number of bytes read == 0, while CellFsRead does not
            return CellOk;
        }

        internal ulong SysFsWrite(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(Arg1);
            ulong nbytes = ppu.ReadArg<ulong>(Arg2);
            GuestPtr<BigEndian<ulong>> nwrite_ptr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg3);

            ulong nrwite = _fileSystemManager.Write(fd, (int)nbytes, buf);
            _memoryManager.Write<BigEndian<ulong>>(nwrite_ptr, nrwite); //Write back the number of bytes written

            return CellOk;
        }

        internal ulong SysFsClose(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);

            _fileSystemManager.CloseFile(fd);
            //Console.WriteLine($"[SysFsClose] cellFsClose fd = 0x{fd:X}");

            return CellOk;
        }

        internal ulong SysFsMkDir(PPUThread ppu)
        {
            // todo
            Console.WriteLine($"[SysFsMkDir] SysFsMkDir -> CellOk");
            return CellOk;
        }

        internal ulong SysFsOpenDir(PPUThread ppu)
        {
            GuestVoidPtr pathPtr = ppu.ReadArg<GuestVoidPtr>(Arg0);
            GuestPtr<BigEndian<uint>> fdPtr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);

            string guestPath = _memoryManager.ReadString(pathPtr);
            string hostPath = GetHostPath(guestPath);

            // Console.WriteLine($"[SysFsOpenDir] SysFsOpenDir {guestPath}");

            int handle = (int)_memoryManager.GetHandle();
            ulong result = _fileSystemManager.OpenDirectory(hostPath, handle, ppu.ThreadID);

            if (result == CellOk)
            {
                _memoryManager.Write<BigEndian<uint>>(fdPtr, (uint)handle);
            }
            else
            {
                Console.WriteLine($"[SysFsOpenDir] SysFsOpenDir failed: {CellErrorNames[result]}");
            }

            return result;
        }

        internal ulong SysFsReadDir(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            GuestPtr<CellFsDirent> direentPtr = ppu.ReadArg<GuestPtr<CellFsDirent>>(Arg1);
            GuestPtr<BigEndian<ulong>> bytesReadPtr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg2);

            ulong bytesRead = _fileSystemManager.ReadDirectory(fd, out CellFsDirent dirent);

            _memoryManager.Write(direentPtr, dirent);
            _memoryManager.Write<BigEndian<ulong>>(bytesReadPtr, bytesRead);

            return CellOk;
        }

        internal ulong SysFsCloseDir(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(Arg0);
            _fileSystemManager.CloseDirectory(fd);
            return CellOk;
        }
    }
}