using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Cell.RSX;
using static PS3Sharp.Cell.PPU.PPU;
using static PS3Sharp.Cell.RSX.RealitySynthesizer;

namespace PS3Sharp
{
    public static class ThreadManager
    {
        private static uint NextID = 0x1000000;
        private static readonly List<PPU> _ppuThreads = new();
        private static RealitySynthesizer RSXThread;
        private static readonly object _threadManagerLock = new(); //I am not sure if this lock is even needed

        public static uint GetNewID()
        {
            lock (_threadManagerLock)
            {
                uint ret = NextID;
                NextID++;
                return ret;
            }
        }

        public static void CreateNativePPUThread(PPU ppu)
        {
            ppu.NativeThread = new Thread(() => RunPPU(ppu))
            {
                Name = ppu.ThreadName
            };

            lock (_threadManagerLock)
            {
                _ppuThreads.Add(ppu);
            }
        }

        public static void RunPPUThread(PPU ppu)
        {
            ppu.NativeThread.Start();
        }

        public static void CreateRSXThread(RealitySynthesizer RSX)
        {
            RSXThread = RSX;
            RSXThread.NativeThread = new Thread(() => RunRSX(RSXThread))
            {
                Name = "RSX Thread"
            };
        }

        public static void RunRSXThread()
        {
            RSXThread.NativeThread.Start();
        }

        public static void JoinThread(Thread thread)
        {
            thread.Join();
        }

        public static void RunPPU(PPU ppu)
        {
            ppu.StartThread();

            while (ppu.PPUThreadState != PPUThreadStates.Stopped)
            {
                if (ppu.PPUThreadState == PPUThreadStates.Paused)
                {
                    Thread.Sleep(1);
                    continue;
                }

                // PPURecompiler.RunPPU(ppu);
                // Temporary

                try
                {
                    PPUInterpreter.Step(ppu);
                }
                catch (Exception e)
                {
                    StopAll();
                    Console.WriteLine($"PPU Exception -- Thread: [{ppu.ThreadName}] ID: [0x{ppu.ThreadID:X8}] --  PC [0x{ppu.GetPC():X8}] -- LR [0x{ppu.GetLR():X8}]");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);

                    Console.WriteLine($"PPU Stack:");
                    PPUInterpreter.PrintCallstack(ppu);

                    Environment.Exit(0);
                }
            }
        }

        private static void RunRSX(RealitySynthesizer rsx)
        {
            rsx.StartThread();
            while (rsx.RsxThreadState != RSXThreadStates.Stopped)
            {
                if (rsx.RsxThreadState == RSXThreadStates.Paused)
                {
                    Thread.Sleep(1);
                    continue;
                }

                //Temporary
                try
                {
                    rsx.RSXThreadMain();
                }
                catch (Exception e)
                {
                    StopAll();
                    Console.WriteLine("RSX Exception");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Environment.Exit(0);
                }
            }
        }

        public static void StopAll()
        {
            foreach (PPU ppu_thread in _ppuThreads)
            {
                if (ppu_thread.PPUThreadState != PPUThreadStates.Stopped)
                {
                    ppu_thread.StopThread();
                }
            }

            RSXThread.StopThread();
        }

        public static PPU? GetThreadByID(ulong id)
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

        public static string GetThreadName(ulong id)
        {
            lock (_threadManagerLock)
            {
                for (int i = 0; i < _ppuThreads.Count; i++)
                {
                    if (_ppuThreads[i].ThreadID == id)
                    {
                        return _ppuThreads[i].ThreadName;
                    }
                }
            }

            return "unknown";
        }

        public static int JoinThreadByID(ulong id)
        {
            PPU? thread = GetThreadByID(id);

            if (thread != null)
            {
                thread.NativeThread.Join();
                return thread.ExitCode;

            }
            else
            {
                throw new Exception($"Cannot find thread id: 0x{id:X8}");
            }
        }
    }
}
