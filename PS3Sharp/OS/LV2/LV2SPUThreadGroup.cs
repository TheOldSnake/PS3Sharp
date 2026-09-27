using PS3Sharp.Cell.SPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using static PS3Sharp.OS.Syscall.SysSPU;

namespace PS3Sharp.OS.LV2 {
    public sealed class LV2SPUThreadGroup {
        public uint Handle { get; } = MemoryManager.GetHandle();
        public GuestPtr<SysSPUThreadGroupAttribute> AttributePtr { get; }
        public uint Number { get; }
        public int Priority { get; }
        public List<SPU> SPUThreads { get; } = new();
        private bool _started = false;

        public LV2SPUThreadGroup(GuestPtr<SysSPUThreadGroupAttribute> attrib_ptr, uint number, int prio) {
            AttributePtr = attrib_ptr;
            Number = number;
            Priority = prio;
        }

        public void AddThread(SPU spu) {
           SPUThreads.Add(spu);
        }

        public void StartGroup() {
            if (_started) {
                return;
            }

            for (int i = 0; i < SPUThreads.Count; i++) {
                SPUThreads[i].Start();
            }

            _started = true;
        }
    }
}
