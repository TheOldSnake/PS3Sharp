using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Runtime.InteropServices;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2EventPort
    {

        // This is what will be sent through event_queue
        // We can probably not use BigEndian here
        [StructLayout(LayoutKind.Sequential)]
        internal struct SysEventData : IPS3Struct
        {
            internal BigEndian<ulong> Source;
            internal BigEndian<ulong> Data1;
            internal BigEndian<ulong> Data2;
            internal BigEndian<ulong> Data3;
        }

        // We can probably not use BigEndian here too
        [StructLayout(LayoutKind.Sequential)]
        internal struct SysEventPort : IPS3Struct
        {
            internal BigEndian<ulong> Name;
            internal BigEndian<int> Type;
            internal BigEndian<ulong> EqueueID;
        }
        private readonly MemoryManager _memoryManager;

        internal readonly int Type;
        internal readonly ulong Name;
        internal uint Handle { get; }

        private LV2EventQueue? _qonnectedQueue;

        internal LV2EventPort(ulong name, int type, MemoryManager memoryManager)
        {
            Name = name;
            Type = type;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal void Connect(LV2EventQueue queue)
        {
            if (_qonnectedQueue == null)
            {
                _qonnectedQueue = queue;
            }
            else
            {
                //Can it connect to multiple queues?
                throw new Exception($"[sys_event_port_connect] Event port: 0x{Handle:X8} is already connected to a queue");
            }
        }

        internal void Disconnect()
        {
            _qonnectedQueue = null;
        }

        internal void Send(ulong data1, ulong data2, ulong data3)
        {
            if (_qonnectedQueue != null)
            {
                SysEventData data = new()
                {
                    Source = Name,
                    Data1 = data1,
                    Data2 = data2,
                    Data3 = data3
                };

                _qonnectedQueue.Send(data);
            }
            else
            {
                throw new Exception($"[sys_event_port_send] Event port: 0x{Handle:X8} was not connected to a queue");
            }
        }
    }
}
