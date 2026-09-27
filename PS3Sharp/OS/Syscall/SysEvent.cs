using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2EventPort;
using static PS3Sharp.OS.LV2.LV2EventQueue;

namespace PS3Sharp.OS.Syscall
{
    internal static class SysEvent
    {
        private static readonly ConcurrentDictionary<uint, LV2EventQueue> _eventQueues = new();
        private static readonly ConcurrentDictionary<uint, LV2EventPort> _eventPorts = new();

        internal static bool GetEventQueueByID(uint id, out LV2EventQueue? eventQueue) => _eventQueues.TryGetValue(id, out eventQueue);

        internal static void AddEventQueue(uint handle, LV2EventQueue eventQueue)
        {
            bool ok = _eventQueues.TryAdd(handle, eventQueue);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not add event queue to dictionary");
            }
        }

        internal static void RemoveEventQueue(uint handle)
        {
            bool ok = _eventQueues.TryRemove(handle, out _);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not remove event queue from dictionary");
            }
        }

        internal static bool GetEventPortByID(uint id, out LV2EventPort? evenPort) => _eventPorts.TryGetValue(id, out evenPort);

        internal static void AddEventPort(uint handle, LV2EventPort eventPort)
        {
            bool ok = _eventPorts.TryAdd(handle, eventPort);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not add event port to dictionary");
            }
        }

        internal static void RemoveEventPort(uint handle)
        {
            bool ok = _eventPorts.TryRemove(handle, out _);

            if (!ok)
            {
                throw new Exception($"[SysEvent] Could not remove event port from dictionary");
            }
        }

        internal static bool GetEventQueueBindingByIPCkey(ulong ipc_key, out LV2EventQueue? eventQueue)
        {
            //Search for the queue -- we should probably use another dictionary to look up using ipc_key directly
            eventQueue = _eventQueues.FirstOrDefault(eventQueue => eventQueue.Value.IPCKey == ipc_key).Value;
            return eventQueue != null;
        }


        // sys_event_queue syscalls

        internal static ulong SysEventQueueCreate(PPU ppu)
        {
            GuestPtr<uint> queue_id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<SysEventQueueAttribute> queue_attribute_ptr = ppu.ReadArg<GuestPtr<SysEventQueueAttribute>>(ARG1);
            ulong sys_ipc_key = ppu.ReadArg<ulong>(ARG2);   //used to connect by sys_event_port?
            int size = ppu.ReadArg<int>(ARG3);

            // Create the queue
            SysEventQueueAttribute attribute = queue_attribute_ptr.Read();
            LV2EventQueue eventQueue = new(size, sys_ipc_key, attribute);
            AddEventQueue(eventQueue.Handle, eventQueue);

            // Write the handle
            MemoryManager.Write<uint>(queue_id_ptr, eventQueue.Handle);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_create - id = 0x{eventQueue.Handle:X8} size = {size} type = {attribute.Type} protocol = {attribute.Protocol}");
            return CELL_OK;
        }

        internal static ulong SysEventQueueReceive(PPU ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(ARG0);
            GuestVoidPtr event_ptr = ppu.ReadArg<GuestVoidPtr>(ARG1); //?
            ulong us_timeout = ppu.ReadArg<ulong>(ARG2);

            Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_receive - waiting for event queue id 0x{queue_id:X8} timeout = {us_timeout}us");

            if (!GetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
            {
                return CELL_ESRCH;
            }

            if (eventQueue.IsSPUQueue)
            {
                return CELL_EINVAL;
            }

            // Hack
            //if (queue_id == 0x0000017B) {
            //    return CELL_OK;
            //}

            // Try to receive
            ulong errorCode = eventQueue.Receive(us_timeout, out SysEventData sysEventData);

            if (errorCode == CELL_OK)
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

        internal static ulong SysEventQueueDestroy(PPU ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(ARG0);
            int mode = ppu.ReadArg<int>(ARG1);      //?

            RemoveEventQueue(queue_id);
            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_destroy -- queue id = 0x{queue_id:X8}");

            return CELL_OK;
        }

        internal static ulong SysEventQueueTryReceive(PPU ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<SysEventData> event_array_ptr = ppu.ReadArg<GuestPtr<SysEventData>>(ARG1);
            int number_of_events = ppu.ReadArg<int>(ARG2);
            GuestPtr<int> read_ptr = ppu.ReadArg<GuestPtr<int>>(ARG3); //number of events that we could read

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_tryreceive -- queue id = 0x{queue_id:X8}");

            if (!GetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
            {
                return CELL_ESRCH;
            }

            ReadOnlySpan<SysEventData> received = eventQueue.TryReceive(number_of_events);

            // Write to the array, we can copy raw bytes because the struct is big endian
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<SysEventData, byte>(received);
            MemoryManager.CopyBufferToVirtualMemory(bytes, event_array_ptr);

            MemoryManager.Write<int>(read_ptr, received.Length);

            // Are there errors ?
            return CELL_OK;
        }

        internal static ulong SysEventQueueDrain(PPU ppu)
        {
            uint queue_id = ppu.ReadArg<uint>(ARG0);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_queue_drain -- queue id = 0x{queue_id:X8}");

            if (!GetEventQueueByID(queue_id, out LV2EventQueue? eventQueue))
            {
                return CELL_ESRCH;
            }

            eventQueue.Drain();
            return CELL_OK;
        }

        //sys_event_port syscalls

        internal static ulong SysEventPortCreate(PPU ppu)
        {
            GuestPtr<uint> eport_id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            int port_type = ppu.ReadArg<int>(ARG1);
            ulong name = ppu.ReadArg<ulong>(ARG2);

            //Create a port
            LV2EventPort eventPort = new(name, port_type);
            AddEventPort(eventPort.Handle, eventPort);

            //Write the handle
            MemoryManager.Write<uint>(eport_id_ptr, eventPort.Handle);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_create -- id = 0x{eventPort.Handle:X8}");
            return CELL_OK;
        }

        internal static ulong SysEventPortDestroy(PPU ppu)
        {
            uint eport_id = ppu.ReadArg<uint>(ARG0);

            RemoveEventPort(eport_id);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_destroy -- eport_id = 0x{eport_id:X8}");
            return CELL_OK;
        }

        internal static ulong SysEventPortConnectLocal(PPU ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(ARG0);
            uint queue_id = ppu.ReadArg<uint>(ARG1);

            bool foundPort = GetEventPortByID(event_port_id, out LV2EventPort? eventPort);
            bool foundEventQueue = GetEventQueueByID(queue_id, out LV2EventQueue? eventQueue);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_connect_local -- eport_id = 0x{event_port_id:X8} -> event_queue_id = 0x{queue_id:X8}");

            if (!foundPort || !foundEventQueue)
            {
                return CELL_ESRCH;
            }

            eventPort.Connect(eventQueue);

            return CELL_OK;
        }

        internal static ulong SysEventPortConnectIPC(PPU ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(ARG0);
            ulong ipc_key = ppu.ReadArg<ulong>(ARG1);

            bool foundPort = GetEventPortByID(event_port_id, out LV2EventPort? eventPort);
            bool foundEventQueue = GetEventQueueBindingByIPCkey(ipc_key, out LV2EventQueue? eventQueue);

            //Console.WriteLine($"[{ppu.ThreadName}]  sys_event_port_connect_ipc -- eport_id = 0x{event_port_id:X8} -> event_queue_id = 0x{ipc_key:X8}");

            if (!foundPort || !foundEventQueue)
            {
                return CELL_ESRCH;
            }

            eventPort.Connect(eventQueue);

            return CELL_OK;
        }

        internal static ulong SysEventPortDisconnect(PPU ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(ARG0);

            //Console.WriteLine($"[{ppu.ThreadName}]  sys_event_port_disconnect -- eport_id = 0x{event_port_id:X8}");

            if (!GetEventPortByID(event_port_id, out LV2EventPort? eventPort))
            {
                return CELL_ESRCH;
            }

            eventPort.Disconnect();

            return CELL_OK;
        }

        internal static ulong SysEventPortSend(PPU ppu)
        {
            uint event_port_id = ppu.ReadArg<uint>(ARG0);
            ulong data1 = ppu.ReadArg<ulong>(ARG1);
            ulong data2 = ppu.ReadArg<ulong>(ARG2);
            ulong data3 = ppu.ReadArg<ulong>(ARG3);

            if (!GetEventPortByID(event_port_id, out LV2EventPort? eventPort))
            {
                return CELL_ESRCH;
            }

            eventPort.Send(data1, data2, data3);

            //Console.WriteLine($"[{ppu.ThreadName}] sys_event_port_send -- eport_id = 0x{event_port_id:X8}");
            return CELL_OK;
        }
    }
}
