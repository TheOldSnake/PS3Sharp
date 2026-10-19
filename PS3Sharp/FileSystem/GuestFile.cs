using static PS3Sharp.OS.Syscall.SysFs;

namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal sealed class GuestFile : IDisposable
        {
            private readonly FileStream _stream;
            private readonly BinaryReader? _reader;
            private readonly BinaryWriter? _writer;

            internal string FilePath { get; }
            internal int FileDescriptor { get; }
            internal uint OwnerThread { get; }
            internal FileMode Mode { get; }
            internal FileAccess Access { get; }
            internal FileShare Share { get; }
            internal ulong Size => (ulong)_stream.Length;

            internal GuestFile(string path, int fileDescriptor, uint ownerThread, uint flags)
            {
                FilePath = path;
                FileDescriptor = fileDescriptor;
                OwnerThread = ownerThread;
                Mode = GetFileMode(flags);
                Access = GetFileAccess(flags);
                Share = GetFileShare(Access);

                // Create the directory first if it does not exist
                string? directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Open file stream and create reader / writer
                _stream = File.Open(path, Mode, Access, Share);
                _stream.Seek(0, SeekOrigin.Begin);

                if ((Access & FileAccess.Read) != 0)
                {
                    _reader = new BinaryReader(_stream);
                }

                if ((Access & FileAccess.Write) != 0)
                {
                    _writer = new BinaryWriter(_stream);
                }
            }

            internal long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

            internal bool ReadExactly(Span<byte> buffer)
            {
                if (_reader == null || Access == FileAccess.Write)
                {
                    return false;
                }

                _reader.ReadExactly(buffer);
                return true;
            }

            internal bool Write(Span<byte> buffer)
            {
                if (_writer == null || Access == FileAccess.Read)
                {
                    return false;
                }

                _writer.Write(buffer);
                return true;
            }

            private static FileMode GetFileMode(uint flags)
            {
                bool create = (flags & O_CREAT) != 0;
                return create ? FileMode.OpenOrCreate : FileMode.Open;
            }

            private static FileAccess GetFileAccess(uint flags)
            {
                uint access = flags & 0x3;

                if (access == O_RDONLY)
                {
                    return FileAccess.Read;
                }

                if (access == O_WRONLY)
                {
                    return FileAccess.Write;
                }

                return FileAccess.ReadWrite;
            }

            private static FileShare GetFileShare(FileAccess access)
            {
                switch (access)
                {
                    case FileAccess.Read:
                        return FileShare.Read;

                    case FileAccess.Write:
                        return FileShare.Write;

                    default:
                        return FileShare.ReadWrite;
                }
            }

            internal void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                _reader?.Close();
                _writer?.Close();
                _stream.Close();
            }
        }
    }
}