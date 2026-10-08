namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal sealed class GuestDirectory
        {
            internal readonly string Path;
            internal readonly int FileDescriptor;
            internal readonly uint OwnerThread;
            internal readonly ulong Size;
            internal int CurrentEntry = 0;
            internal DirectoryInfo DirectoryInfo { get; private set; }

            internal GuestDirectory(string path, ulong size, int fileDescriptor, uint ownerThread)
            {
                Path = path;
                FileDescriptor = fileDescriptor;
                OwnerThread = ownerThread;
                Size = size;
                DirectoryInfo = new DirectoryInfo(path);
            }
        }
    }
}
