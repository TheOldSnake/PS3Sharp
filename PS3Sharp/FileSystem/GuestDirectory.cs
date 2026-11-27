using System.Runtime.CompilerServices;
using System.Text;
using static PS3Sharp.OS.Syscall.SysFs;

namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal sealed class GuestDirectory
        {
            internal const byte CellFsTypeDirectory = 1;
            internal const byte CellFsTypeRegular = 2;

            private static readonly CellFsDirent _currentDirectoryEntry = CreateEntry(name: ".", isDirectory: true);
            private static readonly CellFsDirent _previousDirectoryEntry = CreateEntry(name: "..", isDirectory: true);
            private static readonly CellFsDirent _nullEntry;
            private static readonly ulong _entrySize = (ulong)Unsafe.SizeOf<CellFsDirent>();

            private readonly DirectoryInfo _directoryInfo;
            private readonly IEnumerable<FileSystemInfo> _fileSystemInfoList;
            private readonly List<CellFsDirent> _directoryEntries;
            private int _currentEntry;

            internal string Path { get; }
            internal int FileDescriptor { get; }
            internal uint OwnerThread { get; }
            internal ulong Size { get; }

            internal GuestDirectory(string path, int fileDescriptor, uint ownerThread)
            {
                Path = path;
                FileDescriptor = fileDescriptor;
                OwnerThread = ownerThread;
                Size = GetDirectorySize(path);

                _directoryInfo = new DirectoryInfo(path);
                _fileSystemInfoList = _directoryInfo.EnumerateFileSystemInfos();
                _directoryEntries = CreateEntriesList(_fileSystemInfoList);
            }

            internal ulong ReadNextEntry(out CellFsDirent dirent)
            {
                if (_currentEntry >= _directoryEntries.Count)
                {
                    _currentEntry = 0;
                    dirent = _nullEntry;
                    return 0;
                }

                dirent = _directoryEntries[_currentEntry];
                _currentEntry++;

                // Size report might be wrong because the size here includes a full 256 byte name buffer
                return _entrySize;
            }

            private static List<CellFsDirent> CreateEntriesList(IEnumerable<FileSystemInfo> fileSystemInfoList)
            {
                List<CellFsDirent> entries = [];

                entries.Add(_currentDirectoryEntry);
                entries.Add(_previousDirectoryEntry);

                foreach (FileSystemInfo fileSystemInfo in fileSystemInfoList)
                {
                    bool isDirectory = (fileSystemInfo.Attributes & FileAttributes.Directory) != 0;
                    CellFsDirent entry = CreateEntry(fileSystemInfo.Name, isDirectory);
                    entries.Add(entry);
                }

                return entries;
            }

            private static CellFsDirent CreateEntry(string name, bool isDirectory)
            {
                Span<byte> nameBuffer = Encoding.UTF8.GetBytes(name);
                byte type = isDirectory ? CellFsTypeDirectory : CellFsTypeRegular;

                CellFsDirent entry = new()
                {
                    Type = type,
                    NameLength = (byte)nameBuffer.Length,
                };

                nameBuffer.CopyTo(entry.Name);
                return entry;
            }
        }
    }
}