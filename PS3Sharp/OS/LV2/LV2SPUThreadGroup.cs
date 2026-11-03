using PS3Sharp.Cell.Spu;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.OS.Syscall.SysSpu;

namespace PS3Sharp.OS.LV2
{
    internal sealed class LV2SpuThreadGroup
    {
        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;
        private bool _started = false;
        private readonly ManualResetEventSlim _groupFinished = new(false);
        private readonly List<SpuThread> _spuThreads = [];
        private readonly Lock _groupLock = new();

        private LV2EventQueue? _runEvents;
        private LV2EventQueue? _exceptionEvents;
        private LV2EventQueue? _sysModuleEvents;

        internal uint Handle { get; }
        internal GuestPtr<SysSpuThreadGroupAttribute> AttributePtr { get; }
        internal uint Number { get; }
        internal int Priority { get; }

        internal LV2SpuThreadGroup(ThreadManager threadManager, MemoryManager memoryManager, GuestPtr<SysSpuThreadGroupAttribute> attribPtr, uint number, int prio)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
            AttributePtr = attribPtr;
            Number = number;
            Priority = prio;
            Handle = _memoryManager.GetHandle();
        }

        internal SpuThread CreateSpuThread(SysSpuImage img, SysSpuThreadAttribute attribute, SysSpuThreadArgument arg)
        {
            using (_groupLock.EnterScope())
            {
                uint count = (uint)(_spuThreads.Count & 0xFF);
                uint id = (count << 24) | (Handle & 0xFFFFFF);

                GuestVoidPtr name_ptr = attribute.NamePtr;
                string thread_name = name_ptr.IsNullPtr ? "Unnamed-SPU" : _memoryManager.ReadString(name_ptr);

                SpuThread spu = _threadManager.CreateSPUThread(thread_name, id, Handle, img, arg);
                _spuThreads.Add(spu);

                return spu;
            }
        }

        internal void StartGroup()
        {
            using ( _groupLock.EnterScope())
            {
                if (_started)
                {
                    return;
                }

                for (int i = 0; i < _spuThreads.Count; i++)
                {
                    _spuThreads[i].Start();
                }

                _started = true;
            }
        }

        internal void JoinGroup()
        {
            _groupFinished.Wait();
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

        internal bool TryConnectEventAllThreads(ulong req, uint equeueId, out byte portNumber)
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
                        threads[i].SetPort(port, equeueId);
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
