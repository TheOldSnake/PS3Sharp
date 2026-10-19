using System.Drawing;
using System.Runtime.InteropServices.Marshalling;

namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal sealed class GuestFile : IDisposable
        {
            internal string FilePath { get; }
            internal int FileDescriptor { get; }
            internal uint OwnerThread { get; }
            internal FileMode Mode { get; }
            internal FileAccess Access { get; }
            internal FileStream Stream { get; }
            internal BinaryReader? Reader { get; }
            internal BinaryWriter? Writer { get; }
            internal ulong Size => (ulong)Stream.Length;

            internal GuestFile(string path, int fileDescriptor, uint ownerThread, FileMode mode, FileAccess access)
            {
                FilePath = path;
                FileDescriptor = fileDescriptor;
                OwnerThread = ownerThread;
                Mode = mode;
                Access = access;

                // Create the directory first if it does not exist
                string? directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Open file stream and create reader / writer
                Stream = File.Open(path, Mode, Access);
                Stream.Seek(0, SeekOrigin.Begin);

                if ((Access & FileAccess.Read) != 0)
                {
                    Reader = new BinaryReader(Stream);
                }

                if ((Access & FileAccess.Write) != 0)
                {
                    Writer = new BinaryWriter(Stream);
                }
            }

            internal void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                Reader?.Close();
                Writer?.Close();
                Stream.Close();
            }
        }
    }
}