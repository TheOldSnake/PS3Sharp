using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2EventPort;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2EventQueue
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SysEventQueueAttribute : IPS3Struct
        {
            internal BigEndian<uint> Protocol;
            internal BigEndian<uint> Type;
            internal InlineData.InlineArray8<byte> Name;
        }

        // Event Queue attributes
        internal const uint SysPpuQueue = 1;
        internal const uint SysSpuQueue = 2;

        private readonly MemoryManager _memoryManager;
        private readonly BlockingCollection<SysEventData> _queue;
        private readonly SysEventQueueAttribute _attribute;

        internal ulong IPCKey { get; }
        internal uint Handle { get; }
        internal bool IsSPUQueue => _attribute.Type == SysSpuQueue;

        internal LV2EventQueue(int size, ulong ipc, SysEventQueueAttribute attribute, MemoryManager memoryManager)
        {
            _queue = new BlockingCollection<SysEventData>(size);
            IPCKey = ipc;
            _attribute = attribute;
            _memoryManager = memoryManager;
            Handle = _memoryManager.GetHandle();
        }

        internal void Send(SysEventData evt)
        {
            _queue.Add(evt);
        }

        internal ulong Receive(PPUThread ppu, ulong us_timeout, out SysEventData sysEvent)
        {
            bool ok;

            if (us_timeout > 0)
            {
                ok = _queue.TryTake(out sysEvent, TimeSpan.FromMicroseconds(us_timeout));
            }
            else
            {
                // Blocks until event available or thread is interrupted
                ok = InterruptableWait(ppu, out sysEvent);
            }

            return ok ? CellOk : CellETimedout;
        }

        // Tries to read "count" events without blocking
        internal ReadOnlySpan<SysEventData> TryReceive(int events_count)
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

        internal void Drain()
        {
            // Since there is no built in Clear() in the BlockingCollection
            // Just take out all remaining items using a loop

            while (_queue.TryTake(out _))
            {

            }
        }

        private bool InterruptableWait(PPUThread ppu, out SysEventData sysEvent)
        {
            // Return true if it could take from the queue, false if the thread was interrupted
            while (true)
            {

                // Wait for 100ms
                bool ok = _queue.TryTake(out sysEvent, TimeSpan.FromMilliseconds(100));

                if (ok)
                {
                    return true;
                }

                // Check if the thread is interrupted by shutdown signal and must return immediately
                if (ppu.ShouldExit)
                {
                    return false;
                }
            }
        }
    }
}
