using System;
using System.Collections.Generic;
using System.Text;

namespace PS3Sharp
{
    public sealed class WorkItem
    {
        public Action Work { get; }
        public ManualResetEventSlim Done { get; }
        public WorkItem(Action work)
        {
            Work = work;
            Done = new ManualResetEventSlim(false);
        }
    }
}
