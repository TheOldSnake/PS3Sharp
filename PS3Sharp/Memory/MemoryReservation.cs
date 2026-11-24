namespace PS3Sharp.Memory
{
    internal partial class MemoryManager
    {
        internal readonly struct MemoryReservation
        {
            internal readonly uint ThreadID { init; get; }
            internal readonly ulong AddressStart { init; get; }
            internal readonly ulong Size { init; get; }
            internal readonly ulong AddressEnd => AddressStart + Size;

            // Registered wake up event for SPU threads
            internal readonly ManualResetEventSlim? LostReservationEvent { init; get; }

            internal bool Contains(ulong accessStart, ulong size)
            {
                ulong accessEnd = accessStart + size;
                return accessStart >= AddressStart && accessEnd <= AddressEnd;
            }

            internal bool OverlapsWith(ulong accessStart, ulong size)
            {
                ulong accessEnd = accessStart + size;
                return accessStart < AddressEnd && accessEnd > AddressStart;
            }

            // Notify the thread by setting the event, if it exists
            internal void SetEvent() => LostReservationEvent?.Set();
        }
    }
}