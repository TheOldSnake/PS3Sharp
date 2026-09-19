using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;

namespace PS3Sharp.OS.LV2 {
    public class LV2EventPort {

        // This is what will be sent through event_queue
        // We can probably not use BigEndian here
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysEventData : IPS3Struct {
            public BigEndian<ulong> Source;
            public BigEndian<ulong> Data1;
            public BigEndian<ulong> Data2;
            public BigEndian<ulong> Data3;
        }

        // We can probably not use BigEndian here too
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysEventPort : IPS3Struct {
            public BigEndian<ulong> Name;
            public BigEndian<int> Type;
            public BigEndian<ulong> EqueueID;
        }

        public readonly int Type;
        public readonly ulong Name;
        public uint Handle { get; } = MemoryManager.GetHandle();

        private LV2EventQueue? _qonnectedQueue;

        public LV2EventPort(ulong name, int type) { 
            Name = name;
            Type = type;
        }

        public void Connect(LV2EventQueue queue) {
            if (_qonnectedQueue == null) {
                _qonnectedQueue = queue;
            } else {
                //Can it connect to multiple queues?
                throw new Exception($"[sys_event_port_connect] Event port: 0x{Handle:X8} is already connected to a queue");
            }
        }

        public void Disconnect() {
            _qonnectedQueue = null;
        }

        public void Send(ulong data1, ulong data2, ulong data3) {
            if (_qonnectedQueue != null) {
                SysEventData data = new SysEventData {
                    Source = Name,
                    Data1 = data1,
                    Data2 = data2,
                    Data3 = data3
                };

                _qonnectedQueue.Send(data);

            } else {
                throw new Exception($"[sys_event_port_send] Event port: 0x{Handle:X8} was not connected to a queue");
            }
        }
    }
}
