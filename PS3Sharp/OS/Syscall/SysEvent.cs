using PS3Sharp.Cell.Ppu;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2EventPort;
using static PS3Sharp.OS.LV2.LV2EventQueue;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysEvent
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2EventQueue> _eventQueues = new();
        private readonly ConcurrentDictionary<uint, LV2EventPort> _eventPorts = new();

        internal SysEvent(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal bool TryGetEventQueueByIPCkey(ulong ipc_key, [NotNullWhen(true)] out LV2EventQueue? eventQueue)
        {
            // Search for the queue -- we should probably use another dictionary to look up using ipc_key directly
            eventQueue = _eventQueues.FirstOrDefault(eventQueue => eventQueue.Value.IPCKey == ipc_key).Value;
            return eventQueue != null;
        }

        // sys_event_queue syscalls

        internal ulong SysEventQueueCreate(PpuThread ppu)
        {
            GuestPtr<BigEndian<uint>> queue_id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<SysEventQueueAttribute> queue_attribute_ptr = ppu.ReadArg<GuestPtr<SysEventQueueAttribute>>(Arg1);
            ulong sys_ipc_key = ppu.ReadArg<ulong>(Arg2);   // used to connect by sys_event_port?
            int size = ppu.ReadArg<int>(Arg3);

            // Create the queue
            SysEventQueueAttribute attribute = _memoryManager.Read<SysEventQueueAttribute>(queue_attribute_ptr);
            LV2EventQueue eventQueue = new(size, sys_ipc_key, attribute, _memoryManager);
            AddEventQueue(eventQueue.Handle, eventQueue);

            // Write the handle
            _memoryManager.Write<BigEndian<uint>>(queue_id_ptr, eventQueue.Handle);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_create - id = 0x{eventQueue.Handle:X8} size = {size} type = {attribute.Type} protocol = {attribute.Protocol}");
            return CellOk;
        }

        internal ulong SysEventQueueReceive(PpuThread ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(Arg0);
            GuestVoidPtr event_ptr = ppu.ReadArg<GuestVoidPtr>(Arg1); //?
            ulong us_timeout = ppu.ReadArg<ulong>(Arg2);

            Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_receive - waiting for event queue id 0x{queue_id:X8} timeout = {us_timeout}us pc = 0x{ppu.GetPC():X}");

            if (!TryGetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
            {
                return CellESrch;
            }

            if (eventQueue.IsSPUQueue)
            {
                return CellEInval;
            }

            // Hack
            //if (queue_id == 0x0000017B) {
            //    return CELL_OK;
            //}

            // Try to receive
            ulong errorCode = eventQueue.Receive(ppu, us_timeout, out SysEventData sysEventData);

            if (errorCode == CellOk)
            {
                // Write the data to GPR[4] - GPR[7]
                // (converted to little endian automatically)
                ppu.WriteReg<ulong>(4, sysEventData.Source);
                ppu.WriteReg<ulong>(5, sysEventData.Data1);
                ppu.WriteReg<ulong>(6, sysEventData.Data2);
                ppu.WriteReg<ulong>(7, sysEventData.Data3);
            }

            return errorCode;
        }

        internal ulong SysEventQueueDestroy(PpuThread ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(Arg0);
            int mode = ppu.ReadArg<int>(Arg1);      //?

            RemoveEventQueue(queue_id);
            // Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_destroy -- queue id = 0x{queue_id:X8}");

            return CellOk;
        }

        internal ulong SysEventQueueTryReceive(PpuThread ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(Arg0);
            GuestPtr<SysEventData> event_array_ptr = ppu.ReadArg<GuestPtr<SysEventData>>(Arg1);
            int number_of_events = ppu.ReadArg<int>(Arg2);
            GuestPtr<BigEndian<int>> read_ptr = ppu.ReadArg<GuestPtr<BigEndian<int>>>(Arg3); //number of events that we could read

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_tryreceive -- queue id = 0x{queue_id:X8}");

            if (!TryGetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
            {
                return CellESrch;
            }

            ReadOnlySpan<SysEventData> received = eventQueue.TryReceive(number_of_events);
            _memoryManager.WriteArray(event_array_ptr, received);
            _memoryManager.Write<BigEndian<int>>(read_ptr, received.Length);

            // Are there errors ?
            return CellOk;
        }

        internal ulong SysEventQueueDrain(PpuThread ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(Arg0);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_drain -- queue id = 0x{queue_id:X8}");

            if (!TryGetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
            {
                return CellESrch;
            }

            eventQueue.Drain();

            return CellOk;
        }

        //sys_event_port syscalls

        internal ulong SysEventPortCreate(PpuThread ppu)
        {
            GuestPtr<BigEndian<uint>> eport_id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            int port_type = ppu.ReadArg<int>(Arg1);
            ulong name = ppu.ReadArg<ulong>(Arg2);

            //Create a port
            LV2EventPort eventPort = new(name, port_type, _memoryManager);
            AddEventPort(eventPort.Handle, eventPort);

            //Write the handle
            _memoryManager.Write<BigEndian<uint>>(eport_id_ptr, eventPort.Handle);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_create -- id = 0x{eventPort.Handle:X8}");

            return CellOk;
        }

        internal ulong SysEventPortDestroy(PpuThread ppu)
        {
            uint eport_id = ppu.ReadArg<uint>(Arg0);

            RemoveEventPort(eport_id);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_destroy -- eport_id = 0x{eport_id:X8}");

            return CellOk;
        }

        internal ulong SysEventPortConnectLocal(PpuThread ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(Arg0);
            uint queue_id = ppu.ReadArg<uint>(Arg1);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_connect_local -- eport_id = 0x{event_port_id:X8} -> event_queue_id = 0x{queue_id:X8}");

            if (!TryGetEventPortByID(event_port_id, out LV2EventPort? eventPort))
            {
				return CellESrch;
			}

			if (!TryGetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
			{
				return CellESrch;
			}

			eventPort.Connect(eventQueue);

			return CellOk;
		}

		internal ulong SysEventPortConnectIPC(PpuThread ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(Arg0);
            ulong ipc_key = ppu.ReadArg<ulong>(Arg1);

			//Console.WriteLine($"[{ppu.ThreadName}]  sys_event_port_connect_ipc -- eport_id = 0x{event_port_id:X8} -> event_queue_id = 0x{ipc_key:X8}");

			if (!TryGetEventPortByID(event_port_id, out LV2EventPort? eventPort))
			{
				return CellESrch;
			}

			if (!TryGetEventQueueByIPCkey(ipc_key, out LV2EventQueue? eventQueue))
			{
				return CellESrch;
			}

			eventPort.Connect(eventQueue);

			return CellOk;
        }

        internal ulong SysEventPortDisconnect(PpuThread ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(Arg0);

            //Console.WriteLine($"[{ppu.ThreadName}]  sys_event_port_disconnect -- eport_id = 0x{event_port_id:X8}");

            if (!TryGetEventPortByID(event_port_id, out LV2EventPort? eventPort))
            {
                return CellESrch;
            }

            eventPort.Disconnect();

            return CellOk;
        }

        internal ulong SysEventPortSend(PpuThread ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(Arg0);
            ulong data1 = ppu.ReadArg<ulong>(Arg1);
            ulong data2 = ppu.ReadArg<ulong>(Arg2);
            ulong data3 = ppu.ReadArg<ulong>(Arg3);

            if (!TryGetEventPortByID(event_port_id, out LV2EventPort? eventPort))
            {
                return CellESrch;
            }

            eventPort.Send(data1, data2, data3);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_send -- eport_id = 0x{event_port_id:X8}");

            return CellOk;
        }

        internal bool TryGetEventQueueByID(uint id, [NotNullWhen(true)] out LV2EventQueue? eventQueue) => _eventQueues.TryGetValue(id, out eventQueue);

        internal bool TryGetEventPortByID(uint id, [NotNullWhen(true)] out LV2EventPort? evenPort) => _eventPorts.TryGetValue(id, out evenPort);

        // Request may come from Cell Audio
        internal void AddEventQueue(uint handle, LV2EventQueue eventQueue)
        {
            bool ok = _eventQueues.TryAdd(handle, eventQueue);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not add event queue to dictionary");
            }
        }

        private void RemoveEventQueue(uint handle)
        {
            bool ok = _eventQueues.TryRemove(handle, out _);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not remove event queue from dictionary");
            }
        }

        private void AddEventPort(uint handle, LV2EventPort eventPort)
        {
            bool ok = _eventPorts.TryAdd(handle, eventPort);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not add event port to dictionary");
            }
        }

        private void RemoveEventPort(uint handle)
        {
            bool ok = _eventPorts.TryRemove(handle, out _);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not remove event port from dictionary");
            }
        }
    }
}
