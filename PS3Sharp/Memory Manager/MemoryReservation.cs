namespace PS3Sharp.Memory_Manager
{
    internal static partial class MemoryManager
    {
        internal sealed class MemoryReservation
        {
            internal ulong VirtualAddress;
            internal ulong Size;
            internal uint ThreadID;
        }
    }
}