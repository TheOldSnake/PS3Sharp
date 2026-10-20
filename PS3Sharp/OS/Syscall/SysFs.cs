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
            internal BigEndian<int> UID;           // s32
            internal BigEndian<int> GID;           // s32
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
        internal static readonly uint CELL_FS_S_IFDIR = Oct("0040000"); // Directory
        internal static readonly uint CELL_FS_S_IFREG = Oct("0100000"); // Regular
        internal static readonly uint CELL_FS_S_IFLNK = Oct("0120000"); // Symbolic link
        internal static readonly uint CELL_FS_S_IFWHT = Oct("0160000"); // Unknown

        internal static readonly uint CELL_FS_S_IRUSR = Oct("0000400"); // R for owner
        internal static readonly uint CELL_FS_S_IWUSR = Oct("0000200"); // W for owner
        internal static readonly uint CELL_FS_S_IXUSR = Oct("0000100"); // X for owner

        internal static readonly uint CELL_FS_S_IRGRP = Oct("0000040"); // R for group
        internal static readonly uint CELL_FS_S_IWGRP = Oct("0000020"); // W for group
        internal static readonly uint CELL_FS_S_IXGRP = Oct("0000010"); // X for group

        internal static readonly uint CELL_FS_S_IROTH = Oct("0000004"); // R for other
        internal static readonly uint CELL_FS_S_IWOTH = Oct("0000002"); // W for other
        internal static readonly uint CELL_FS_S_IXOTH = Oct("0000001"); // X for other

        // Default Modes
        internal static readonly uint DIR_MODE = Oct("0777") | CELL_FS_S_IFDIR;   // RWX RWX RWX
        internal static readonly uint FILE_MODE = Oct("0666") | CELL_FS_S_IFREG;   // RW- RW- RW-

        // SysFsOpen flags
        internal static readonly uint O_RDONLY = Oct("0000");              // Read only
        internal static readonly uint O_WRONLY = Oct("0001");              // Write only
        internal static readonly uint O_RDWR = Oct("0002");                // Read/write

        internal static readonly uint O_CREAT = Oct("0100");          // Create file if it does not exist
        internal static readonly uint O_EXCL = Oct("0200");           // Exclusive create
        internal static readonly uint O_NOCTTY = Oct("0400");         // Do not assign controlling terminal
        internal static readonly uint O_TRUNC = Oct("01000");         // Truncate file
        internal static readonly uint O_APPEND = Oct("02000");        // Append on each write
        internal static readonly uint O_NONBLOCK = Oct("04000");      // Non-blocking I/O

        internal static readonly uint O_DSYNC = Oct("010000");        // Synchronized writes (data only)
        internal static readonly uint O_SYNC = Oct("04010000");       // Synchronized writes (data + metadata)
        internal readonly uint O_RSYNC = O_SYNC;

        internal static readonly uint O_DIRECTORY = Oct("0200000");   // Must be a directory
        internal static readonly uint O_NOFOLLOW = Oct("0400000");    // Do not follow symlinks
        internal static readonly uint O_CLOEXEC = Oct("02000000");    // Close on exec

        // Same as linux?
        internal const uint FD_STDIN = 0;
        internal const uint FD_STDOUT = 1;
        internal const uint FD_STDERR = 2;
        internal const uint FD_EOF = 4;

        // For the console
        private static CellFsStat ConsoleFsStat = new()
        {
            Mode = (int)(CELL_FS_S_IRUSR | CELL_FS_S_IWUSR | CELL_FS_S_IXUSR |
                         CELL_FS_S_IRGRP | CELL_FS_S_IWGRP | CELL_FS_S_IXGRP |
                         CELL_FS_S_IROTH | CELL_FS_S_IWOTH | CELL_FS_S_IXOTH |
                         CELL_FS_S_IFREG),

            UID = 0,
            GID = 0,
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
            return CELL_OK;
        }

        internal uint SysFsStat(PPUThread ppu)
        {
            uint pathPtr = ppu.ReadArg<uint>(ARG0);
            GuestPtr<CellFsStat> statPtr = ppu.ReadArg<GuestPtr<CellFsStat>>(ARG1);

            if (pathPtr == 0)
            {
                return CELL_EFAULT;
            }

            string guestPath = _memoryManager.ReadString(pathPtr);

            // ?
            if (string.IsNullOrEmpty(guestPath))
            {
                return CELL_ENOENT;
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
                return CELL_ENOENT;
            }

            CellFsStat stat = new()
            {
                Mode = (int)(isDirectory ? DIR_MODE : FILE_MODE),
                UID = 0,
                GID = 0,
                AccessTime = 0,
                ModifyTime = 0,
                CreateTime = 0,
                Size = isDirectory ? GetDirectorySize(hostPath) : GetFileSize(hostPath),
                BlkSize = 4096
            };

            _memoryManager.Write(statPtr, stat);

            return CELL_OK;
        }

        internal ulong SysFsFstat(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(ARG0);
            GuestPtr<CellFsStat> statPtr = ppu.ReadArg<GuestPtr<CellFsStat>>(ARG1);

            bool isConsole = fd == FD_STDIN || fd == FD_STDOUT || fd == FD_STDERR;

            // Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsFstat fd = 0x{fd:X}");

            if (isConsole)
            {
                _memoryManager.Write(statPtr, ConsoleFsStat);
                Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsFstat => CELL_EBADF");
                return CELL_EBADF;
            }

            ulong size;
            int mode;

            if (_fileSystemManager.TryGetFile(fd, out GuestFile? guestFile))
            {
                size = guestFile.Size;
                mode = (int)FILE_MODE;
            }
            else if (_fileSystemManager.TryGetDirectory(fd, out GuestDirectory? directory))
            {
                size = directory.Size;
                mode = (int)DIR_MODE;
            }
            else
            {
                Console.WriteLine($"[{ppu.ThreadName}] [SysFs] SysFsFstat => CELL_EBADF");
                return CELL_EBADF;
            }

            CellFsStat stat = new()
            {
                Mode = mode,
                UID = 0,
                GID = 0,
                AccessTime = 0,
                ModifyTime = 0,
                CreateTime = 0,
                Size = size,
                BlkSize = 4096
            };

            _memoryManager.Write(statPtr, stat);

            return CELL_OK;
        }

        internal ulong SysFsOpen(PPUThread ppu)
        {
            uint pathPtr = ppu.ReadArg<uint>(ARG0);
            uint flags = ppu.ReadArg<uint>(ARG1);
            GuestPtr<int> fdPtr = ppu.ReadArg<GuestPtr<int>>(ARG2);

            // uint64_t mode, const void *arg, uint64_t size <- ARG3/4/5
            // the mode is only in the syscall not the library

            string guestPath = _memoryManager.ReadString(pathPtr);

            if (!IsGuestAbsolutePath(guestPath))
            {
                Console.WriteLine($"[{ppu.ThreadName}] [SysFsOpen] Error, path: {guestPath} is not absolute (CELL_ENOENT)");
                return CELL_ENOENT;
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

            if (result != CELL_OK)
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
            int fd = ppu.ReadArg<int>(ARG0);
            long offset = ppu.ReadArg<long>(ARG1);
            int whence = ppu.ReadArg<int>(ARG2);
            GuestPtr<ulong> posPtr = ppu.ReadArg<GuestPtr<ulong>>(ARG3);

            // Console.WriteLine($"[{ppu.ThreadName}] [SysFsLseek] cellFsLseek fd = 0x{fd:X} seek offset: 0x{offset:X} mode = {whence}");

            if (!_fileSystemManager.IsFileOpen(fd))
            {
                return CELL_EINVAL;
            }

            ulong position = (ulong)_fileSystemManager.Seek(fd, offset, whence);
            _memoryManager.Write<BigEndian<ulong>>(posPtr, position);

            return CELL_OK;
        }

        internal ulong SysFsRead(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(ARG0);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG1);
            ulong nbytes = ppu.ReadArg<ulong>(ARG2);
            GuestPtr<ulong> nreadPtr = ppu.ReadArg<GuestPtr<ulong>>(ARG3);

            // MemoryManager.MemoryBlock block = _memoryManager.GetAllocationByVirtualAddress(buf);
            // Console.WriteLine($"[{ppu.ThreadName}] [SysFsRead] cellFsRead fd = 0x{fd:X} amount: 0x{nbytes:X} dest: 0x{buf:X} base = 0x{b.VirtualAddress:X8}");

            if (buf.IsNullPtr || nreadPtr.IsNullPtr || nbytes == 0)
            {
                return CELL_EFAULT;
            }

            // Read into the buffer
            ulong readCount = _fileSystemManager.Read(fd, nbytes, buf);                                  

            // Write back the number of bytes read
            if (nreadPtr > 0)
            {
                _memoryManager.Write<BigEndian<ulong>>(nreadPtr, readCount);
            }

            // TODO: Seems like this syscall returns CELL_EFAULT if number of bytes read == 0, while CellFsRead does not
            return CELL_OK;
        }

        internal ulong SysFsWrite(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(ARG0);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG1);
            ulong nbytes = ppu.ReadArg<ulong>(ARG2);
            GuestPtr<ulong> nwrite_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG3);

            ulong nrwite = _fileSystemManager.Write(fd, (int)nbytes, buf);
            _memoryManager.Write<BigEndian<ulong>>(nwrite_ptr, nrwite); //Write back the number of bytes written

            return CELL_OK;
        }

        internal ulong SysFsClose(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(ARG0);

            _fileSystemManager.CloseFile(fd);
            //Console.WriteLine($"[SysFsClose] cellFsClose fd = 0x{fd:X}");

            return CELL_OK;
        }

        internal ulong SysFsMkDir(PPUThread ppu)
        {
            // todo
            Console.WriteLine($"[SysFsMkDir] SysFsMkDir . CELL_OK");
            return CELL_OK;
        }

        internal ulong SysFsOpenDir(PPUThread ppu)
        {
            GuestVoidPtr pathPtr = ppu.ReadArg<GuestVoidPtr>(ARG0);
            GuestPtr<uint> fdPtr = ppu.ReadArg<GuestPtr<uint>>(ARG1);

            string guestPath = _memoryManager.ReadString(pathPtr);
            string hostPath = GetHostPath(guestPath);

            // Console.WriteLine($"[SysFsOpenDir] SysFsOpenDir {guestPath}");

            int handle = (int)_memoryManager.GetHandle();
            ulong result = _fileSystemManager.OpenDirectory(hostPath, handle, ppu.ThreadID);

            if (result == CELL_OK)
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
            int fd = ppu.ReadArg<int>(ARG0);
            GuestPtr<CellFsDirent> direentPtr = ppu.ReadArg<GuestPtr<CellFsDirent>>(ARG1);
            GuestPtr<ulong> bytesReadPtr = ppu.ReadArg<GuestPtr<ulong>>(ARG2);

            ulong bytesRead = _fileSystemManager.ReadDirectory(fd, out CellFsDirent dirent);

            _memoryManager.Write(direentPtr, dirent);
            _memoryManager.Write<BigEndian<ulong>>(bytesReadPtr, bytesRead);

            return CELL_OK;
        }

        internal ulong SysFsCloseDir(PPUThread ppu)
        {
            int fd = ppu.ReadArg<int>(ARG0);
            _fileSystemManager.CloseDirectory(fd);
            return CELL_OK;
        }
    }
}