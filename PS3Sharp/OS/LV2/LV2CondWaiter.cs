namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2CondWaiter
    {
        internal uint WaiterThreadID { get; }
        internal ManualResetEventSlim Event { get; } = new(false);

        internal LV2CondWaiter(uint threadID)
        {
            WaiterThreadID = threadID;
        }
    }
}
