using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using PS3Sharp.SFOLoader;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal static readonly bool AllowWrites = false; // TODO: Make it configurable

        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<int, GuestFile> _openFiles = new();
        private readonly ConcurrentDictionary<int, GuestDirectory> _openDirectories = new();
        private SFO? _currentSFO;

        internal string CurrentContentDirectory { get; private set; } = string.Empty;
        internal string CurrentELFDirectory { get; private set; } = string.Empty;
        internal string CurrentSFODirectory { get; private set; } = string.Empty;

        internal FileSystemManager(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;

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

        internal static bool IsDirectory(string path) => Directory.Exists(path);
        internal bool IsDirectoryOpen(int fd) => _openDirectories.ContainsKey(fd);
        internal bool IsDirectoryOpen(string path) => _openDirectories.Values.Any(f => f.Path == path);
        internal bool TryGetDirectory(int fd, [NotNullWhen(true)] out GuestDirectory? directory) => _openDirectories.TryGetValue(fd, out directory);

        internal static bool IsFile(string path) => File.Exists(path);
        internal bool IsFileOpen(int fd) => _openFiles.ContainsKey(fd);
        internal bool IsFileOpen(string path) => _openFiles.Values.Any(f => f.Path == path);
        internal static bool IsEncryptedFile(string path) => path.EndsWith(".edat", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".sdat", StringComparison.OrdinalIgnoreCase);
        internal static bool HasBeenDecrypted(string path) => File.Exists(path + ".unedat");
        internal GuestFile GetFileByPath(string path) => _openFiles.Values.First(f => f.Path == path); // Normally not used
        internal bool TryGetFile(int fd, [NotNullWhen(true)] out GuestFile? file) => _openFiles.TryGetValue(fd, out file);

        internal static bool IsGuestAbsolutePath(string guestPath) => guestPath.StartsWith('/');

        internal static string GetHostPath(string guestPath)
        {
            //Assuming absolute paths 
            if (!IsGuestAbsolutePath(guestPath))
            {
                throw new Exception($"[FileSystem] path is not absolute -- path = {guestPath}");
            }

            string hostPath = guestPath.Substring(1).Replace($"/", $"\\");
            return hostPath;
        }

        internal static string GetGuestPath(string hostPath)
        {
            string guestPath = "/" + hostPath.Replace($"\\", $"/");
            return guestPath;
        }

        internal ulong GetFileSize(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            FileInfo fileInfo = new(filePath);
            return (ulong)fileInfo.Length; // size in bytes
        }

        internal ulong GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            DirectoryInfo dir = new(directoryPath);
            return (ulong)dir.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        internal ulong OpenFile(string host_path, int fd, uint ownerThread)
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
            GuestFile file = new(host_path, GetFileSize(host_path), fd, ownerThread);
            _openFiles.TryAdd(fd, file);

            return CELL_OK;
        }

        internal ulong OpenDirectory(string host_path, int fd, uint ownerThread)
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
            GuestDirectory directory = new(host_path, GetDirectorySize(host_path), fd, ownerThread);
            _openDirectories.TryAdd(fd, directory);

            return CELL_OK;
        }

        internal void Close(int fd)
        {
            _openFiles.Remove(fd, out GuestFile? file);
            file?.Close();
        }

        internal long Seek(int fd, long offset, int mode)
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

        internal ulong Read(int fd, ulong nbytes, GuestVoidPtr destinationBuffer)
        {
            if (nbytes > int.MaxValue)
            {
                throw new Exception($"[FileSystem] Read amount is too large: {nbytes}");
            }

            unsafe
            {
                Span<byte> nativeBuffer = _memoryManager.CreateByteSpan(destinationBuffer, (int)nbytes);

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

        internal Span<byte> ReadSpan(int fd, ulong nbytes)
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

        internal ulong Write(int fd, int nBytes, GuestVoidPtr sourceBuffer)
        {
            if (_openFiles.TryGetValue(fd, out GuestFile? file))
            {
                if (AllowWrites)
                {
                    unsafe
                    {
                        Span<byte> nativeBuffer = _memoryManager.CreateByteSpan(sourceBuffer, nBytes);
                        file.Writer?.Write(nativeBuffer);
                    }
                }
                else
                {
                    Console.WriteLine($"[FileSystemManager] Ignored write to {file?.Path}");
                }
            }

            return (ulong)nBytes;
        }

        internal ulong ReadDir(int fd, GuestPtr<SysFs.CellFsDirent> dirent_ptr)
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

            DirectoryInfo dir_info = directory!.DirectoryInfo;
            IEnumerable<FileSystemInfo> fs_info_list = dir_info.EnumerateFileSystemInfos();

            SysFs.CellFsDirent dirent = new();

            Span<byte> name_buffer;
            Span<byte> name_dest = dirent.Name;
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
                        dirent.Type = 0;
                        dirent.NameLength = 0;
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

            dirent.Type = type;
            name_buffer.CopyTo(name_dest);
            dirent.NameLength = (byte)(name_buffer.Length - 1); // length without '\0'

            // Write to memory
            _memoryManager.Write(dirent_ptr, dirent);

            Console.WriteLine($"dirent name = {Encoding.UTF8.GetString(dirent.Name)} name_length = {dirent.NameLength} type = 0x{dirent.Type:X} CurrentEntry = {directory.CurrentEntry} len = {fs_info_list.Count()}");

            directory.CurrentEntry++;

            return dirent_size;
        }

        internal bool TryGetRequestedExecutable(out string elfPath)
        {
            elfPath = string.Empty;
            List<(string content, string elf)> executables = GetAllExecutables();

            if (executables.Count == 0)
            {
                Console.WriteLine($"Did not find executables");
                return false;
            }

            Console.WriteLine($"Select which executable to launch:");

            for (int i = 0; i < executables.Count; i++)
            {
                (string content, string elf) executable = executables[i];
                Console.WriteLine($@"{i}: {executable.content}\{executable.elf}");
            }

            int index;

            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out index))
                {
                    if (index < executables.Count)
                    {
                        break;
                    }
                }

                Console.WriteLine($"Invalid input, please try again");
            }

            (string content, string elf) = executables[index];

            CurrentContentDirectory = $@"dev_hdd0\game\{content}";
            Console.WriteLine($"Content: {CurrentContentDirectory}");

            CurrentELFDirectory = $"{CurrentContentDirectory}{@"\USRDIR\"}{elf}";
            Console.WriteLine($"Elf: {CurrentELFDirectory}");

            string expectedSFOPath = $"{CurrentContentDirectory}{@"\PARAM.SFO"}";
            CurrentSFODirectory = File.Exists(expectedSFOPath) ? expectedSFOPath : string.Empty;

            if (CurrentSFODirectory != string.Empty)
            {
                Console.WriteLine($"Sfo: {CurrentSFODirectory}");
            }

            elfPath = CurrentELFDirectory;
            return true;
        }

        internal string GetCurrentTitleString()
        {
            if (TryGetCurrentSFO(out SFO? sfo))
            {
                return sfo!.Title;
            }
            else
            {
                return Path.GetFileName(CurrentELFDirectory);
            }
        }

        internal string GetCurrentTitleID()
        {
            if (TryGetCurrentSFO(out SFO? sfo))
            {
                return sfo!.TitleID;
            }
            else
            {
                return string.Empty;
            }
        }

        internal bool TryGetCurrentSFO(out SFO? sfo)
        {
            bool success;

            if (_currentSFO == null)
            {
                success = TryLoadSFO();
            }
            else
            {
                success = true;
            }

            sfo = _currentSFO;
            return success;
        }

        private bool TryLoadSFO()
        {
            if (!File.Exists(CurrentSFODirectory))
            {
                return false;
            }

            _currentSFO = SFO.LoadSFO(CurrentSFODirectory);
            return true;
        }

        private List<(string content, string elf)> GetAllExecutables()
        {
            DirectoryInfo allContents = new(@"dev_hdd0\game");
            List<(string content, string elf)> executables = [];

            foreach (DirectoryInfo content in allContents.GetDirectories())
            {
                foreach (DirectoryInfo contentSubDirectory in content.GetDirectories())
                {
                    if (contentSubDirectory.Name == "USRDIR")
                    {
                        foreach (FileInfo usrDirFile in contentSubDirectory.GetFiles())
                        {
                            if (usrDirFile.Name.EndsWith($".elf"))
                            {
                                executables.Add((content.Name, usrDirFile.Name));
                            }
                        }
                    }
                }
            }

            return executables;
        }
    }
}