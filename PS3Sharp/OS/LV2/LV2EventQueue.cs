using PS3Sharp.Cell;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Syscall;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using static PS3Sharp.OS.LV2.LV2EventPort;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.LV2
{
    public sealed class LV2EventQueue
    {
        // Event Queue attributes
        public const uint SYS_PPU_QUEUE = 1;
        public const uint SYS_SPU_QUEUE = 2;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SysEventQueueAttribute : IPS3Struct
        {
            public BigEndian<uint> Protocol;
            public BigEndian<uint> Type;
            public InlineData.InlineArray8<byte> Name;
        }

        private readonly BlockingCollection<SysEventData> _queue;
        public readonly SysEventQueueAttribute _attribute;
        public readonly ulong IPCKey;
        public uint Handle { get; } = MemoryManager.GetHandle();

        public bool IsSPUQueue => _attribute.Type == SYS_SPU_QUEUE;

        public LV2EventQueue(int size, ulong ipc, SysEventQueueAttribute attribute)
        {
            _queue = new BlockingCollection<SysEventData>(size);
            IPCKey = ipc;
            _attribute = attribute;
        }

        public void Send(SysEventData evt)
        {
            _queue.Add(evt);
        }

        public ulong Receive(ulong us_timeout, out SysEventData sysEvent)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = _queue.TryTake(out sysEvent, TimeSpan.FromMicroseconds(us_timeout));

            }
            else
            {
                // Blocks until event available
                sysEvent = _queue.Take();
                ok = true;
            }

            return ok ? CELL_OK : CELL_ETIMEDOUT;
        }

        // Tries to read "count" events without blocking
        public ReadOnlySpan<SysEventData> TryReceive(int events_count)
        {
            SysEventData[] data = new SysEventData[events_count];
            int read = 0;

            while (read < events_count)
            {
                if (_queue.TryTake(out SysEventData sysEvent, 0))
                {
                    data[read++] = sysEvent;
                }
                else
                {
                    break;
                }
            }

            return data.AsSpan(0, read);
        }

        public void Drain()
        {
            // Since there is no built in Clear() in the BlockingCollection
            // Just take out all remaining items using a loop

            while (_queue.TryTake(out _))
            {

            }
        }
    }
}
