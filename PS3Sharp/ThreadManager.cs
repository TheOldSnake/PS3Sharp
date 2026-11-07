using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.Recompiler;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Cell.Spu;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS;
using PS3Sharp.OS.Modules;
using PS3Sharp.OS.Syscall;
using System.Diagnostics.CodeAnalysis;

namespace PS3Sharp
{
    internal class ThreadManager : IDisposable
    {
        private readonly ManualResetEventSlim _processTerminationEvent = new(false);     // Shutdown event
        private readonly MemoryManager _memoryManager;
        private readonly PPURecompiler _ppuRecompiler;
        private readonly List<PPUThread> _ppuThreads = [];
        private readonly List<SpuThread> _spuThreads = [];
        private readonly object _threadManagerLock = new();
        private bool _hasCreatedMainThread;
        private bool _hasCreatedRSXThread;
        private RealitySynthesizer? RSXThread;
        private uint _nextID = 0x1000000;

        // _isShuttingDown means the emulator is in a shutdown state
        // and will not accept any new thread creation requests
        private bool _isShuttingDown;

        internal CancellationTokenSource GlobalCancellationSource { get; } = new();      // Global shutdown signal

        internal ThreadManager(MemoryManager memoryManager, PPURecompiler ppuRecompiler)
        {
            _memoryManager = memoryManager;
            _ppuRecompiler = ppuRecompiler;
        }

        internal uint GetNewID()
        {
            lock (_threadManagerLock)
            {
                uint ret = _nextID;
                _nextID++;
                return ret;
            }
        }

        internal PPUThread CreateMainPPUThread(uint entryPointer, string executablePath, LV2SyscallDispatcher syscallDispatcher)
        {
            lock (_threadManagerLock)
            {
                ArgumentException.ThrowIfNullOrEmpty(executablePath);

                if (_hasCreatedMainThread)
                {
                    throw new InvalidOperationException($"[ThreadManager] Only one main thread is allowed");
                }

                uint stackSize = MemoryManager.DefaultStackSize;
                ulong arg = 0;

                PPUThread ppu = CreatePPUThread("main_thread", entryPointer, stackSize, arg, syscallDispatcher, executablePath);
                _hasCreatedMainThread = true;

                return ppu;
            }
        }

        internal PPUThread CreatePPUThread(string threadName, uint entryPointer, uint stackSize, ulong thread_arg, LV2SyscallDispatcher syscallDispatcher, string? executablePath = null)
        {
            lock (_threadManagerLock)
            {
                if (_isShuttingDown)
                {
                    throw new InvalidOperationException($"[ThreadManager] Cannot create new threads in a shutdown state");
                }

                if ((stackSize & 0xF) > 0)
                {
                    stackSize = (uint)Utility.AlignPowerOfTwo(stackSize, 16);
                }

                if (stackSize < 0x1000)
                {
                    stackSize = MemoryManager.DefaultStackSize;
                }

                uint id = GetNewID();
                PPUThread ppu = new(threadName, entryPointer, stackSize, executablePath, id, thread_arg, this, _memoryManager, syscallDispatcher, _ppuRecompiler);

                _ppuThreads.Add(ppu);
                return ppu;
            }
        }

        internal RealitySynthesizer CreateRSXThread(EmulatorWindow window, CellGcmSys cellGcmSys)
        {
            if (_hasCreatedRSXThread)
            {
                throw new InvalidOperationException($"[ThreadManager] Only one RSX thread is allowed");
            }

            RSXThread = new(this, _memoryManager, window, cellGcmSys);
            _hasCreatedRSXThread = true;

            return RSXThread;
        }

        internal SpuThread CreateSPUThread(string name, uint id, uint groupID, SysSpu.SysSpuImage img, SysSpu.SysSpuThreadArgument arg)
        {
            SpuThread spu = new(name, id, groupID, img, arg, _memoryManager);
            _spuThreads.Add(spu);
            return spu;
        }

        internal void JoinPPUThread(PPUThread ppu_thread, bool timeout = false)
        {
            JoinNativeThread(ppu_thread.NativeThread, timeout);
        }

        internal void RequestShutdown()
        {
            // Attempts to fully shutdown the emulator

            lock (_threadManagerLock)
            {
                // if another thread has already started the shutdown, don't do anything.
                if (_isShuttingDown)
                {
                    return;
                }

                Console.WriteLine($"[ThreadManager] Entering shutdown state");

                // Signal the global shutdown token to notify all threads to exit
                GlobalCancellationSource.Cancel();

                // Signal the ProcessTerminationEvent to wake up the main emulator thread
                _processTerminationEvent.Set();

                // Set the shutdown flag to prevent new threads from being created, and copy the thread list
                _isShuttingDown = true;
            }

            // Do not do any joining/waiting inside the lock, as it can cause potential deadlocks
        }

        internal void WaitForProcessEnd()
        {
            // Wait for the virtual process to enter shutdown state
            _processTerminationEvent.Wait();

            // Aquire the lock and copy the thread list (which cannot be modified at this point)
            PPUThread[] threads;

            lock (_threadManagerLock)
            {
                threads = _ppuThreads.ToArray();
            }

            JoinAllPPUThreads(threads);          // Wait for all ppu threads to exit
            JoinRSXThread();                     // Wait for the rsx thread to exit
        }

        internal void PrintExitCodes()
        {
            PPUThread[] threads;

            lock (_threadManagerLock)
            {
                threads = _ppuThreads.ToArray();
            }

            Console.WriteLine($"[ThreadManager] PPU Exit codes:");

            foreach (PPUThread ppu in threads)
            {
                Console.WriteLine($"PPU [{ppu.ThreadName}] Exit Code = 0x{ppu.ExitCode:X8}");
            }
        }

        internal PPUThread? GetPPUThreadByID(ulong id)
        {
            lock (_threadManagerLock)
            {
                for (int i = 0; i < _ppuThreads.Count; i++)
                {
                    if (_ppuThreads[i].ThreadID == id)
                    {
                        return _ppuThreads[i];
                    }
                }
            }

            return null;
        }

        internal bool TryGetSpuThreadByID(ulong id, [NotNullWhen(true)] out SpuThread? spu)
        {
            lock (_threadManagerLock)
            {
                for (int i = 0; i < _spuThreads.Count; i++)
                {
                    if (_spuThreads[i].ID == id)
                    {
                        spu = _spuThreads[i];
                        return true;
                    }
                }
            }

            spu = null;
            return false;
        }

        internal int JoinThreadByID(ulong id)
        {
            PPUThread? thread = GetPPUThreadByID(id);

            if (thread != null)
            {
                JoinPPUThread(thread);
                return thread.ExitCode;
            }
            else
            {
                throw new Exception($"Cannot find thread id: 0x{id:X8}");
            }
        }

        private void JoinAllPPUThreads(PPUThread[] threads)
        {
            Console.WriteLine($"[ThreadManager] Joining all PPU threads");

            foreach (PPUThread ppuThread in threads)
            {
                Console.WriteLine($"[ThreadManager] Attempting to join {ppuThread.ThreadName}");
                JoinPPUThread(ppuThread, timeout: true);
            }
        }

        private void JoinRSXThread()
        {
            if (RSXThread == null)
            {
                return;
            }

            Console.WriteLine($"[ThreadManager] Attempting to join RSX thread");
            JoinNativeThread(RSXThread.NativeThread, timeout: true);
        }

        private void JoinNativeThread(Thread thread, bool timeout)
        {
            // Do not try to join on the caller as it will deadlock
            if (thread == Thread.CurrentThread)
            {
                return;
            }

            // Some threads may have not started, like RSX thread if the process did not initialize RSX
            // Try to join and catch the exception if needed.

            try
            {
                if (timeout)
                {
                    // If a thread refuses to stop after 5 seconds then give up and abort without cleaning.
                    bool ok = thread.Join(TimeSpan.FromSeconds(5));

                    if (!ok)
                    {
                        Console.WriteLine($"[ThreadManager] Join timeout, aborting...");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    thread.Join();
                }
            }
            catch (ThreadStateException exception)
            {
                // An exception is expected if the thread was not started. This is not a bug.
                Console.WriteLine($"[ThreadManager] Catched exception when joining thread: {exception.Message}");
            }
        }

        public void Dispose()
        {
            GlobalCancellationSource.Dispose();
            _processTerminationEvent.Dispose();
        }
    }
}
