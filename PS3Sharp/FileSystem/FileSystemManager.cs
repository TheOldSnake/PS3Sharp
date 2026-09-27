using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.FileSystem
{
    public static class FileSystemManager
    {
        private static readonly ConcurrentDictionary<int, GuestFile> _openFiles = new();
        private static readonly ConcurrentDictionary<int, GuestDirectory> _openDirectories = new();
        public sealed class GuestFile : IDisposable
        {
            public readonly string Path;
            public readonly int FileDescriptor;
            public readonly uint OwnerThread;
            public readonly FileStream Stream;
            public readonly BinaryReader Reader;
            public readonly BinaryWriter Writer;

            public readonly ulong Size;

            public GuestFile(string path, int fileDescriptor, uint ownerThread)
            {
                Path = path;
                FileDescriptor = fileDescriptor;
                Size = GetFileSize(path);
                OwnerThread = ownerThread;
                Stream = File.OpenRead(path); // todo: open or create flag
                Reader = new BinaryReader(Stream);
                //Writer = new BinaryWriter(Stream);
                Stream.Seek(0, SeekOrigin.Begin);
            }

            public void Dispose()
            {
                Reader.Dispose();
                Stream.Dispose();
            }
        }

        public sealed class GuestDirectory
        {
            public readonly string Path;
            public readonly int FileDescriptor;
            public readonly uint OwnerThread;
            public readonly ulong Size;
            public int CurrentEntry = 0;
            public DirectoryInfo DirectoryInfo { get; private set; }

            public GuestDirectory(string path, int fileDescriptor, uint ownerThread)
            {
                Path = path;
                FileDescriptor = fileDescriptor;
                OwnerThread = ownerThread;
                Size = GetDirectorySize(path);
                DirectoryInfo = new DirectoryInfo(path);
            }
        }

        public static void InitializeFileSystem()
        {
            if (!Directory.Exists(@"dev_hdd0\game"))
            {
                Console.WriteLine(@"Creating dev_hdd0\game");
                Directory.CreateDirectory(@"dev_hdd0\game");
            }

            if (!Directory.Exists("dev_flash"))
            {
                Console.WriteLine("Creating dev_flash");
                Directory.CreateDirectory("dev_flash");
            }
        }

        public static bool IsDirectory(string path) => Directory.Exists(path);
        public static bool IsDirectoryOpen(int fd) => _openDirectories.ContainsKey(fd);
        public static bool IsDirectoryOpen(string path) => _openDirectories.Values.Any(f => f.Path == path);
        public static bool GetDirectory(int fd, out GuestDirectory? directory) => _openDirectories.TryGetValue(fd, out directory);

        public static bool IsFile(string path) => File.Exists(path);
        public static bool IsFileOpen(int fd) => _openFiles.ContainsKey(fd);
        public static bool IsFileOpen(string path) => _openFiles.Values.Any(f => f.Path == path);
        public static bool IsEncryptedFile(string path) => path.EndsWith(".edat", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".sdat", StringComparison.OrdinalIgnoreCase);
        public static bool HasBeenDecrypted(string path) => File.Exists(path + ".unedat");
        public static GuestFile GetFileByPath(string path) => _openFiles.Values.First(f => f.Path == path); // Normally not used
        public static bool GetFile(int fd, out GuestFile? file) => _openFiles.TryGetValue(fd, out file);

        public static ulong GetFileSize(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            FileInfo fileInfo = new(filePath);
            return (ulong)fileInfo.Length; // size in bytes
        }

        public static ulong GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            DirectoryInfo dir = new(directoryPath);
            return (ulong)dir.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        public static ulong OpenFile(string host_path, int fd, uint ownerThread)
        {

            // Error if it's a directory
            if (IsDirectory(host_path))
            {
                return CELL_EISDIR;
            }

            // Error if the file does not exist
            if (!IsFile(host_path))
            {
                return CELL_ENOENT;
            }

            // Open file
            GuestFile file = new(host_path, fd, ownerThread);
            _openFiles.TryAdd(fd, file);

            return CELL_OK;
        }

        public static ulong OpenDirectory(string host_path, int fd, uint ownerThread)
        {

            // Error if it's a file
            if (IsFile(host_path))
            {
                return CELL_ENOTDIR;
            }

            // Error if the directory does not exist
            if (!IsDirectory(host_path))
            {
                return CELL_ENOENT;
            }

            // Open directory
            GuestDirectory directory = new(host_path, fd, ownerThread);
            _openDirectories.TryAdd(fd, directory);

            return CELL_OK;
        }

        public static void Close(int fd)
        {
            _openFiles.Remove(fd, out GuestFile? file);
            file?.Dispose();
        }

        public static long Seek(int fd, long offset, int mode)
        {
            if (mode < 0 || mode > 2)
            {
                throw new InvalidOperationException($"[FileSystem] Invalid seek mode: {mode}");
            }

            if (_openFiles.TryGetValue(fd, out GuestFile? file))
            {
                file.Stream.Seek(offset, (SeekOrigin)(mode));
            }
            else
            {
                throw new InvalidOperationException("[FileSystem] file was not open!");
            }

            return file.Stream.Position;
        }

        public static ulong Read(int fd, ulong nbytes, GuestVoidPtr destinationBuffer)
        {
            if (nbytes > int.MaxValue)
            {
                throw new Exception($"[FileSystem] Read amount is too large: {nbytes}");
            }

            unsafe
            {
                Span<byte> nativeBuffer = MemoryManager.CreateSpan<byte>(destinationBuffer, (int)nbytes);

                if (_openFiles.TryGetValue(fd, out GuestFile? file))
                {
                    try
                    {
                        // Read directly into the buffer
                        file.Reader.ReadExactly(nativeBuffer);
                    }
                    catch (EndOfStreamException e)
                    {
                        // Might be incorrect
                        Console.WriteLine($"[FileSystem] Reached end of stream");
                        return 0;
                    }

                }
                else
                {
                    throw new InvalidOperationException("[FileSystem] file was not open!");
                }
            }

            //Return number of bytes read
            return nbytes;
        }

        public static Span<byte> ReadSpan(int fd, ulong nbytes)
        {
            if (nbytes > int.MaxValue)
            {
                throw new Exception($"[FileSystem] Read amount is too large: {nbytes}");
            }

            if (_openFiles.TryGetValue(fd, out GuestFile? file))
            {
                return file.Reader.ReadBytes((int)nbytes);

            }
            else
            {
                throw new InvalidOperationException("[FileSystem] file was not open!");
            }
        }

        public static ulong Write(int fd, int nBytes, GuestVoidPtr sourceBuffer)
        {
            if (_openFiles.TryGetValue(fd, out GuestFile? file))
            {

                // Hacky check to ensure we don't write something outside app_home
                if (!file.Path.StartsWith(@"D:\RPCS3\app_home\"))
                {
                    throw new Exception($"Attempt to write to file outside app_home, path = {file.Path}");
                }

                unsafe
                {
                    Span<byte> nativeBuffer = MemoryManager.CreateSpan<byte>(sourceBuffer, nBytes);
                    file.Writer.Write(nativeBuffer);
                }
            }

            return (ulong)nBytes;
        }

        public static ulong ReadDir(int fd, GuestPtr<SysFs.CellFsDirent> dirent_ptr)
        {
            // Todo: move constants 
            const byte CELL_FS_TYPE_DIRECTORY = 1;
            const byte CELL_FS_TYPE_REGULAR = 2;

            ulong dirent_size = (ulong)Unsafe.SizeOf<SysFs.CellFsDirent>();
            bool found = _openDirectories.TryGetValue(fd, out GuestDirectory? directory);

            if (!found)
            {
                throw new InvalidOperationException("[FileSystem] Directory was not open!");
            }

            DirectoryInfo dir_info = directory.DirectoryInfo;
            IEnumerable<FileSystemInfo> fs_info_list = dir_info.EnumerateFileSystemInfos();

            unsafe
            {
                SysFs.CellFsDirent* dirent = dirent_ptr.AsNativePtr();

                Span<byte> name_buffer;
                Span<byte> name_dest = dirent->Name;
                name_dest.Clear();
                byte type;

                switch (directory.CurrentEntry)
                {

                    case 0: // "." entry
                        name_buffer = Utility.ToCString($".");
                        type = CELL_FS_TYPE_DIRECTORY;
                        break;

                    case 1: // ".." entry
                        name_buffer = Utility.ToCString($"..");
                        type = CELL_FS_TYPE_DIRECTORY;
                        break;

                    default:
                        //entry number relative to the beginning of the list (which does not contain '.' and '..')
                        int actualEntry = directory.CurrentEntry - 2;

                        //if reached end
                        if (actualEntry >= fs_info_list.Count())
                        {
                            dirent->Type = 0;
                            dirent->NameLength = 0;
                            name_dest.Clear();
                            directory.CurrentEntry = 0;
                            return 0;
                        }

                        FileSystemInfo fs_info = fs_info_list.ElementAt(actualEntry);
                        name_buffer = Utility.ToCString(fs_info.Name);

                        bool isDir = (fs_info.Attributes & FileAttributes.Directory) != 0;
                        type = isDir ? CELL_FS_TYPE_DIRECTORY : CELL_FS_TYPE_REGULAR;
                        break;
                }

                dirent->Type = type;
                name_buffer.CopyTo(name_dest);
                dirent->NameLength = (byte)(name_buffer.Length - 1); // length without '\0'

                Console.WriteLine($"dirent name = {Encoding.UTF8.GetString(dirent->Name)} name_length = {dirent->NameLength} type = 0x{dirent->Type:X} CurrentEntry = {directory.CurrentEntry} len = {fs_info_list.Count()}");

                directory.CurrentEntry++;
            }

            return dirent_size;
        }
    }
}
