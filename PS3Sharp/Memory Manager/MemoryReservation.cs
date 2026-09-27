namespace PS3Sharp.Memory_Manager
{
    public static partial class MemoryManager
    {
        public sealed class MemoryReservation
        {
            public ulong VirtualAddress;
            public ulong Size;
            public uint ThreadID;
        }
    }
}