using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.Syscall;
using PS3Sharp.SFOLoader;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static PS3Sharp.OS.Syscall.SysFs;
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

            if (!Directory.Exists("app_home"))
            {
                Console.WriteLine("Creating app_home");
                Directory.CreateDirectory("app_home");
            }
        }

        internal static bool IsDirectory(string path) => Directory.Exists(path);
        internal bool IsDirectoryOpen(int fd) => _openDirectories.ContainsKey(fd);
        internal bool IsDirectoryOpen(string path) => _openDirectories.Values.Any(f => f.Path == path);
        internal bool TryGetDirectory(int fd, [NotNullWhen(true)] out GuestDirectory? directory) => _openDirectories.TryGetValue(fd, out directory);

        internal static bool IsFile(string path) => File.Exists(path);
        internal bool IsFileOpen(int fd) => _openFiles.ContainsKey(fd);
        internal bool IsFileOpen(string path) => _openFiles.Values.Any(f => f.FilePath == path);
        internal static bool IsEncryptedFile(string path) => path.EndsWith(".edat", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".sdat", StringComparison.OrdinalIgnoreCase);
        internal static bool HasBeenDecrypted(string path) => File.Exists(path + ".unedat");
        internal GuestFile GetFileByPath(string path) => _openFiles.Values.First(f => f.FilePath == path); // Normally not used
        internal bool TryGetFile(int fd, [NotNullWhen(true)] out GuestFile? file) => _openFiles.TryGetValue(fd, out file);

        internal static bool IsGuestAbsolutePath(string guestPath) => guestPath.StartsWith('/');

        internal static string GetHostPath(string guestPath)
        {
            // Assuming absolute paths 
            if (!IsGuestAbsolutePath(guestPath))
            {
                throw new Exception($"[FileSystem] path is not absolute -- path = {guestPath}");
            }
            
            // Root directory is emulator folder
            if (guestPath == "/")
            {
                return ".";
            }

            string hostPath = guestPath.Substring(1).Replace($"/", $"\\");
            return hostPath;
        }

        internal static string GetGuestPath(string hostPath)
        {
            string guestPath = "/" + hostPath.Replace($"\\", $"/");
            return guestPath;
        }

        internal static ulong GetFileSize(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            FileInfo fileInfo = new(filePath);
            return (ulong)fileInfo.Length; // size in bytes
        }

        internal static ulong GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            DirectoryInfo dir = new(directoryPath);
            return (ulong)dir.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        internal ulong OpenFile(string hostPath, int fd, uint ownerThread, uint flags)
        {
            // Error if it's a directory
            if (IsDirectory(hostPath))
            {
                return CELL_EISDIR;
            }

            // Error if the file does not exist (and create flag is not set)
            bool exists = IsFile(hostPath);
            bool create = (flags & O_CREAT) != 0;

            if (!(exists || create))
            {
                return CELL_ENOENT;
            }

            // Open file
            GuestFile file = new(hostPath, fd, ownerThread, flags);
            _openFiles.TryAdd(fd, file);
            return CELL_OK;
        }

        internal ulong OpenDirectory(string hostPath, int fd, uint ownerThread)
        {
            // Error if it's a file
            if (IsFile(hostPath))
            {
                return CELL_ENOTDIR;
            }

            // Error if the directory does not exist
            if (!IsDirectory(hostPath))
            {
                return CELL_ENOENT;
            }

            // Open directory
            GuestDirectory directory = new(hostPath, fd, ownerThread);
            _openDirectories.TryAdd(fd, directory);

            return CELL_OK;
        }

        internal void CloseFile(int fd)
        {
            _openFiles.Remove(fd, out GuestFile? file);
            file?.Close();
        }

        internal void CloseDirectory(int fd)
        {
            _openDirectories.Remove(fd, out _);
        }

        internal long Seek(int fd, long offset, int mode)
        {
            if (mode < 0 || mode > 2)
            {
                throw new InvalidOperationException($"[FileSystem] Invalid seek mode: {mode}");
            }

            if (_openFiles.TryGetValue(fd, out GuestFile? file))
            {
                return file.Seek(offset, (SeekOrigin)mode);
            }
            else
            {
                throw new InvalidOperationException("[FileSystem] file was not open!");
            }
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
                        file.ReadExactly(nativeBuffer);
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

        internal ulong Write(int fd, int nBytes, GuestVoidPtr sourceBuffer)
        {
            if (_openFiles.TryGetValue(fd, out GuestFile? file))
            {
                if (AllowWrites)
                {
                    unsafe
                    {
                        Span<byte> nativeBuffer = _memoryManager.CreateByteSpan(sourceBuffer, nBytes);
                        file.Write(nativeBuffer);
                    }
                }
                else
                {
                    Console.WriteLine($"[FileSystemManager] Ignored write to {file?.FilePath}");
                }
            }

            return (ulong)nBytes;
        }

        internal ulong ReadDirectory(int fd, out CellFsDirent dirent)
        {
            if (_openDirectories.TryGetValue(fd, out GuestDirectory? directory))
            {
                return directory.ReadNextEntry(out dirent);
            }
            else
            {
                throw new InvalidOperationException("[FileSystem] Directory was not open!");
            }
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