namespace PS3Sharp.Memory
{
    internal partial class MemoryManager
    {
        internal sealed class MemoryReservation
        {
            internal ulong VirtualAddress;
            internal ulong Size;
            internal uint ThreadID;
        }
    }
}