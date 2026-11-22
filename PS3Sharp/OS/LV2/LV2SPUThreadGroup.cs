using PS3Sharp.Cell.Spu;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.OS.Syscall.SysSpu;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2SpuThreadGroup
    {
        internal static int LimitSpuThreadCount = 0;    // For debugging

        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;

        private readonly Lock _groupLock = new();
        private bool _started = false;
        private readonly ManualResetEventSlim _groupFinishedEvent = new(false);
        private int _groupExitStatus;
        private readonly List<SpuThread> _spuThreads = [];

        private LV2EventQueue? _runEvents;
        private LV2EventQueue? _exceptionEvents;
        private LV2EventQueue? _sysModuleEvents;

        internal uint Handle { get; }
        internal GuestPtr<SysSpuThreadGroupAttribute> AttributePtr { get; }
        internal uint Number { get; }
        internal int Priority { get; }

        internal CancellationTokenSource GroupCts { get; private set; } = new();
        internal int ExitStatus => _groupExitStatus;

        internal LV2SpuThreadGroup(ThreadManager threadManager, MemoryManager memoryManager, GuestPtr<SysSpuThreadGroupAttribute> attribPtr, uint number, int prio)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
            AttributePtr = attribPtr;
            Number = number;
            Priority = prio;
            Handle = _memoryManager.GetHandle();

            if (LimitSpuThreadCount > 0)
            {
                Console.WriteLine($"[LV2SpuThreadGroup] Warning: Spu thread count is limited to {LimitSpuThreadCount}");
            }
        }

        internal SpuThread CreateSpuThread(SysSpuImage img, SysSpuThreadAttribute attribute, SysSpuThreadArgument arg)
        {
            using (_groupLock.EnterScope())
            {
                uint count = (uint)(_spuThreads.Count & 0xFF);
                uint id = (count << 24) | (Handle & 0xFFFFFF);

                GuestVoidPtr name_ptr = attribute.NamePtr;
                string thread_name = name_ptr.IsNullPtr ? "Unnamed-SPU" : _memoryManager.ReadString(name_ptr);

                SpuThread spu = _threadManager.CreateSPUThread(thread_name, id, Handle, img, arg, this);
                _spuThreads.Add(spu);

                return spu;
            }
        }

        internal void StartGroup()
        {
            using (_groupLock.EnterScope())
            {
                if (_started)
                {
                    return;
                }

                if (!GroupCts.TryReset())
                {
                    GroupCts.Dispose();
                    GroupCts = new();
                }

                _groupFinishedEvent.Reset();
                _groupExitStatus = 0;

                for (int i = 0; i < _spuThreads.Count; i++)
                {
                    if (LimitSpuThreadCount > 0 && i >= LimitSpuThreadCount)
                    {
                        break;
                    }

                    _spuThreads[i].Start();
                }

                _started = true;
            }

            // Send a run event if an equeue exists
            LV2EventPort.SysEventData eventData = new()
            {
                Source = SysSpuThreadGroupEventRunKey,
                Data1 = Handle,
                Data2 = 0,
                Data3 = 0
            };

            _runEvents?.Send(eventData);
        }

        internal void GroupExit(int status)
        {
            lock (_groupLock)
            {
                // If already cancelled do nothing
                if (GroupCts.IsCancellationRequested)
                {
                    return;
                }

                // Signal cancellation token
                GroupCts.Cancel();

                // Set exit status
                _groupExitStatus = status;

                // Signal finished event -- wake up any waiting PPU thread
                _groupFinishedEvent.Set();

                _started = false;
            }
        }

        internal int JoinGroup()
        {
            // TODO: Interruptable wait
            _groupFinishedEvent.Wait();
            return _groupExitStatus;
        }

        internal bool SetEventQueue(uint type, LV2EventQueue eventQueue)
        {
            using (_groupLock.EnterScope())
            {
                switch (type)
                {
                    case SysSpuThreadGroupEventRun:
                        _runEvents = eventQueue;
                        break;

                    case SysSpuThreadGroupEventException:
                        _exceptionEvents = eventQueue;
                        break;

                    case SysSpuThreadGroupEventSystemModule:
                        _sysModuleEvents = eventQueue;
                        break;

                    default:
                        return false;
                }

                return true;
            }
        }

        internal bool TryConnectEventAllThreads(ulong req, LV2EventQueue equeue, out byte portNumber)
        {
            using(_groupLock.EnterScope())
            {
                // Find a port that is free on all SPU threads 
                List<SpuThread> threads = _spuThreads;
                byte port = 0;
                bool foundFreePort = false;

                for (port = 0; port < 64; port++)
                {
                    // Skip not requested ports
                    bool request = ((req >> port) & 1) != 0;

                    if (!request)
                    {
                        continue;
                    }

                    // Check all threads      
                    foundFreePort = true;

                    foreach (SpuThread spu in threads)
                    {
                        if (!spu.PortIsFree(port))
                        {
                            foundFreePort = false;
                            break;
                        }
                    }

                    if (foundFreePort)
                    {
                        break;
                    }
                }

                if (foundFreePort)
                {
                    // Connect the port to all spu threads
                    for (int i = 0; i < threads.Count; i++)
                    {
                        threads[i].SetPort(port, equeue);
                    }

                    portNumber = port;
                    return true;
                }
                else
                {
                    portNumber = 0;
                    return false;
                }
            }
        }
    }
}
