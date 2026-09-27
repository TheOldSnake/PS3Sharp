using PS3Sharp.Cell.SPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.OS.Syscall.SysSPU;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2SPUThreadGroup
    {
        internal uint Handle { get; } = MemoryManager.GetHandle();
        internal GuestPtr<SysSPUThreadGroupAttribute> AttributePtr { get; }
        internal uint Number { get; }
        internal int Priority { get; }
        internal List<SPU> SPUThreads { get; } = new();
        private bool _started = false;

        internal LV2SPUThreadGroup(GuestPtr<SysSPUThreadGroupAttribute> attrib_ptr, uint number, int prio)
        {
            AttributePtr = attrib_ptr;
            Number = number;
            Priority = prio;
        }

        internal void AddThread(SPU spu)
        {
            SPUThreads.Add(spu);
        }

        internal void StartGroup()
        {
            if (_started)
            {
                return;
            }

            for (int i = 0; i < SPUThreads.Count; i++)
            {
                SPUThreads[i].Start();
            }

            _started = true;
        }
    }
}
