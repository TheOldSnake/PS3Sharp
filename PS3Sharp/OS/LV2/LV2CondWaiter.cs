namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2CondWaiter
    {
        internal uint WaiterTheadID { get; }
        internal ManualResetEventSlim Event = new(false);

        internal LV2CondWaiter(uint threadID)
        {
            WaiterTheadID = threadID;
        }
    }
}
