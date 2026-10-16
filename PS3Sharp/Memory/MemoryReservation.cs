namespace PS3Sharp.Memory
{
    internal partial class MemoryManager
    {
        internal sealed class MemoryReservation
        {
            internal ulong VirtualAddress { get; set; }
            internal ulong Size { get; set; }
            internal uint ThreadID { get; set; }
        }
    }
}