namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal sealed class GuestFile : IDisposable
        {
            internal string Path { get; }
            internal int FileDescriptor { get; }
            internal uint OwnerThread { get; }
            internal FileStream Stream { get; }
            internal BinaryReader Reader { get; }
            internal BinaryWriter? Writer { get; }
            internal ulong Size { get; }

            internal GuestFile(string path, ulong size, int fileDescriptor, uint ownerThread)
            {
                Path = path;
                FileDescriptor = fileDescriptor;
                Size = size;
                OwnerThread = ownerThread;
                Stream = File.OpenRead(path); // todo: open or create flag
                Reader = new BinaryReader(Stream);
                //Writer = new BinaryWriter(Stream);
                Stream.Seek(0, SeekOrigin.Begin);
            }

            internal void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                Reader.Close();
                Writer?.Close();
                Stream.Close();
            }
        }
    }
}