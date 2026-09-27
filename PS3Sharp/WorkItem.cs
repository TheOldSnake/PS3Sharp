namespace PS3Sharp
{
    internal sealed class WorkItem
    {
        internal Action Work { get; }
        internal ManualResetEventSlim Done { get; }
        internal WorkItem(Action work)
        {
            Work = work;
            Done = new ManualResetEventSlim(false);
        }
    }
}
