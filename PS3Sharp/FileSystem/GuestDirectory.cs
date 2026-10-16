namespace PS3Sharp.FileSystem
{
    internal sealed partial class FileSystemManager
    {
        internal sealed class GuestDirectory
        {
            internal string Path { get; }
            internal int FileDescriptor { get; }
            internal uint OwnerThread { get; }
            internal ulong Size { get; }
            internal int CurrentEntry { get; set; }
            internal DirectoryInfo DirectoryInfo { get; }

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