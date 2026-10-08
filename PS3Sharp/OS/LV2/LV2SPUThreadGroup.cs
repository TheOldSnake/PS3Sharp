using PS3Sharp.Cell.SPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.OS.Syscall.SysSPU;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2SPUThreadGroup
    {
        private readonly MemoryManager _memoryManager;
        private bool _started = false;

        internal uint Handle { get; }
        internal GuestPtr<SysSPUThreadGroupAttribute> AttributePtr { get; }
        internal uint Number { get; }
        internal int Priority { get; }
        internal List<SPUThread> SPUThreads { get; } = [];

        internal LV2SPUThreadGroup(GuestPtr<SysSPUThreadGroupAttribute> attrib_ptr, uint number, int prio, MemoryManager memoryManager)
        {
            AttributePtr = attrib_ptr;
            Number = number;
            Priority = prio;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal void AddThread(SPUThread spu)
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
