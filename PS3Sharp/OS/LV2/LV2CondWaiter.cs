namespace PS3Sharp.OS.LV2
{
    public sealed class LV2CondWaiter
    {
        public readonly uint WaiterTheadID;
        public ManualResetEventSlim Event = new(false);
        public LV2CondWaiter(uint threadID)
        {
            WaiterTheadID = threadID;
        }
    }
}
