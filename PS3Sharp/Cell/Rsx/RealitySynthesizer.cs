using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using PS3Sharp.OS;
using PS3Sharp.OS.Modules;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class RealitySynthesizer : IEmulatorThread
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CellGcmConfig : IPS3Struct
        {
            internal BigEndian<uint> Local_Addr;    // Pointer to RSX local memory
            internal BigEndian<uint> IO_Addr;
            internal BigEndian<uint> Local_Size;    // Size of RSX memory
            internal BigEndian<uint> IO_Size;
            internal BigEndian<uint> MemFreq;
            internal BigEndian<uint> CoreFreq;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellGcmCtxData : IPS3Struct
        {
            internal BigEndian<uint> Begin;       /* Start address of command buffer */
            internal BigEndian<uint> End;         /* End address of command buffer */
            internal BigEndian<uint> Current;     /* Current address of command buffer */
            internal BigEndian<uint> Callback;    /* Callback function that is called when "current" reaches "end" */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellGcmCtrlRegister : IPS3Struct
        {
            internal BigEndian<uint> Put;        /* member for accessing the PUT register */
            internal BigEndian<uint> Get;        /* member for accessing the GET register */
            internal BigEndian<uint> Ref;		   /* member for accessing the REF register. Initial value is 0xFFFFFFFF */
        }

        internal sealed class RSXFrameBuffer
        {
            internal uint Address;
            internal uint Pitch;
            internal uint Width;
            internal uint Height;
        }

        internal enum RSXThreadStates
        {
            Running,
            Paused,
            Stopped
        }

        // Constants
        internal const uint GcmLocationRsx = 0;     // Memory buffer is located in RSX memory.
        internal const uint GcmLocationCell = 1;    // Memory buffer is located in main memory.
        internal const uint GcmLocationReport = 2;  // Memory buffer is located in report memory.
        internal const uint VramSize = 249 * 1024 * 1024;
        internal const uint LabelsSize = 0x10 * 256;
        internal const ulong FrameTimeUs = (ulong)(16.666 * 1000);

        private PPU.PPUThread? _RSX_PPU_Thread;            // For executing rsx callbacks that are ppu functions
        private readonly ThreadManager _threadManager;
        private readonly MemoryManager _memoryManager;
        private readonly EmulatorWindow _window;
        private readonly CellGcmSys _cellGcmSys;
        private readonly VertexShaderGenerator _vertexShaderGenerator;
        private readonly FragmentShaderGenerator _fragmentShaderGenerator;
        private readonly ConcurrentQueue<WorkItem> CommandQueue = new(); // External commands from PPU thread
        private readonly Stack<uint> _callStack = new();
        private ulong _lastVblank;

        private GLRenderer GLRenderer => _window.GLRenderer!;
        private bool ShouldExit => _threadManager.GlobalCancellationSource.IsCancellationRequested || LocalCancellationToken.IsCancellationRequested;

        internal CancellationTokenSource LocalCancellationToken { get; } = new();
        internal RSXInternalState CurrentState { get; }
        internal bool MemoryAllocated { get; private set; }
        internal GuestPtr<CellGcmConfig> ConfigPtr { get; private set; }
        internal GuestPtr<CellGcmCtxData> CtxPtr { get; private set; }
        internal GuestPtr<CellGcmCtrlRegister> CtrlPtr { get; private set; }
        internal GuestVoidPtr VRAMPtr { get; private set; }
        internal GuestPtr<byte> LabelsPtr { get; private set; }
        internal ulong LastFlipTime { get; private set; }
        internal RSXFrameBuffer[] FrameBuffers { get; } = new RSXFrameBuffer[255];
        internal RSXThreadStates RsxThreadState = RSXThreadStates.Stopped;
        internal Thread NativeThread { get; }
        internal int ExitCode { get; private set; }

        internal RealitySynthesizer(ThreadManager threadManager, MemoryManager memoryManager, EmulatorWindow window, CellGcmSys cellGcmSys)
        {
            _threadManager = threadManager;
            _memoryManager = memoryManager;
            _window = window;
            _cellGcmSys = cellGcmSys;

            CurrentState = new RSXInternalState(_memoryManager);
            _vertexShaderGenerator = new();
            _fragmentShaderGenerator = new(_memoryManager, this);

            for (int i = 0; i < FrameBuffers.Length; i++)
            {
                FrameBuffers[i] = new RSXFrameBuffer();
            }

            for (int i = 0; i < CurrentState.VertexBuffers.Length; i++)
            {
                CurrentState.VertexBuffers[i] = new RSXVertexBuffer(_memoryManager);
            }

            for (int i = 0; i < CurrentState.Textures.Length; i++)
            {
                CurrentState.Textures[i] = new RSXTexture(this);
            }

            // Create a native thread
            NativeThread = new Thread(RunRSX)
            {
                Name = "RSX Native Thread"
            };
        }

        internal void CreateRSXPPUThread(ulong r2, LV2SyscallDispatcher syscallDispatcher)
        {
            if (_RSX_PPU_Thread == null)
            {
                _RSX_PPU_Thread = _threadManager.CreatePPUThread("rsx_ppu_thread", 0, 0, 0, syscallDispatcher);
                _RSX_PPU_Thread.WriteReg<ulong>(2, r2);
            }
        }

        internal void AllocateRSXMemory()
        {
            // Allocate vram
            MemoryManager.MemoryBlock vram = _memoryManager.Allocator.AllocateVirtualMemory(VramSize);
            VRAMPtr = vram.VirtualAddress;

            // Allocate a page for structs + labels
            MemoryManager.MemoryBlock page = _memoryManager.Allocator.AllocatePages(pageCount: 1);
            ulong pageStart = page.VirtualAddress;
            ulong pageEnd = page.End;

            ulong configSize = (ulong)Unsafe.SizeOf<CellGcmConfig>();
            ulong ctrlSize = (ulong)Unsafe.SizeOf<CellGcmCtrlRegister>();
            ulong ctxSize = (ulong)Unsafe.SizeOf<CellGcmCtxData>();

            // Align the structs starting addresses to 16 byte
            ulong alignment = 16;
            ulong configStartAddress = pageStart;  // Page base is already page-aligned
            ulong ctrlStartAddress = Utility.AlignPowerOfTwo(configStartAddress + configSize, alignment);
            ulong ctxStartAddress = Utility.AlignPowerOfTwo(ctrlStartAddress + ctrlSize, alignment);
            ulong labelsStartAddress = Utility.AlignPowerOfTwo(ctxStartAddress + ctxSize, alignment);

            Debug.Assert(labelsStartAddress + LabelsSize < pageEnd);

            ConfigPtr = (uint)configStartAddress;
            CtrlPtr = (uint)ctrlStartAddress;
            CtxPtr = (uint)ctxStartAddress;
            LabelsPtr = (uint)labelsStartAddress;

            Console.WriteLine($"RSX Config at 0x{ConfigPtr:X8}");
            Console.WriteLine($"RSX Ctr at 0x{CtrlPtr:X8}");
            Console.WriteLine($"RSX Ctx at 0x{CtxPtr:X8}");
            Console.WriteLine($"RSX Labels at 0x{LabelsPtr:X8}");

            MemoryAllocated = true;
        }

        internal void EnqueueCommand(WorkItem command) => CommandQueue.Enqueue(command);

        internal uint AddressToOffset(uint address)
        {
            CellGcmConfig config = _memoryManager.Read<CellGcmConfig>(ConfigPtr);

            // Relative to VRAM
            if (address >= VRAMPtr.Address && address < VRAMPtr.Address + VramSize)
            { 
                return address - VRAMPtr.Address;
            }
            else
            {
                // Relative to IO space
                if (_cellGcmSys.IsEaOffsMapped(address))
                { 
                    return _cellGcmSys.EaToIo(address);
                }

                return address - (config.IO_Addr + 4096);
            }
        }

        internal uint OffsetToAddress(uint offset, uint location)
        {
            uint address;
            if (location == GcmLocationRsx)
            {
                // Relative to VRAM (Local_Addr)
                CellGcmConfig config = _memoryManager.Read<CellGcmConfig>(ConfigPtr);
                address = offset + config.Local_Addr;
            }
            else
            {
                uint ea_table = _cellGcmSys.OffsetTable.eaTableAddress;
                uint io_page = offset >> 20;
                uint ea_page = _memoryManager.Read<BigEndian<ushort>>(ea_table + (io_page * 2));
                address = (ea_page << 20) | (offset & 0xfffff);
            }

            return address;
        }

        internal void UpdateGet(int value)
        {
            CellGcmCtrlRegister ctrl = _memoryManager.Read<CellGcmCtrlRegister>(CtrlPtr);
            ctrl.Get += (uint)value;
            _memoryManager.Write(CtrlPtr, ctrl);
        }

        internal void SetPut(uint value)
        {
            CellGcmCtrlRegister ctrl = _memoryManager.Read<CellGcmCtrlRegister>(CtrlPtr);
            ctrl.Put = value;
            _memoryManager.Write(CtrlPtr, ctrl);
        }

        internal void SetGet(uint value)
        {
            CellGcmCtrlRegister ctrl = _memoryManager.Read<CellGcmCtrlRegister>(CtrlPtr);
            ctrl.Get = value;
            _memoryManager.Write(CtrlPtr, ctrl);
        }

        internal void RSXThreadMain()
        {
            CellGcmCtrlRegister ctrl = _memoryManager.Read<CellGcmCtrlRegister>(CtrlPtr);

            if (ctrl.Put != ctrl.Get)
            {
                RunCommandBuffer();
            }

            //Run any pending commands
            while (CommandQueue.TryDequeue(out WorkItem? command))
            {
                command.Work();
                command.Done.Set(); //Signal incase the main thread is waiting
            }

            // vblank every 1000 µs, for some reason
            ulong current_us = Utility.ReadTime_Microseconds();
            if (current_us - _lastVblank >= 1000)
            {
                Vblank();
                _lastVblank = current_us;

                // Also Process window events
                _window.ProcessEvents();
            }
        }

        internal void RunCommandBuffer()
        {
            unsafe
            {
                // Note: we control and guarantee the alignment of this struct in AllocateRSXMemory
                CellGcmCtrlRegister* ctrl = _memoryManager.GetPtr(CtrlPtr);

                //Console.WriteLine($"[RSX] Current GET: [0x{ctrl->Get:X8}] Running until: [0x{ctrl->Put:X8}]");

                while (ctrl->Get != ctrl->Put)
                {
                    uint currentAddress = _cellGcmSys.IoToEa(ctrl->Get);
                    uint instruction = _memoryManager.ReadRSXInstruction(currentAddress);
                    RSXCommand CurrentCommand = new()
                    {
                        Raw = instruction
                    };

                    //Console.WriteLine($"[0x{currentAddress:X}] cmd: 0x{instruction:X} get: [0x{ctrl->Get:X}]");

                    // Jump
                    if (CurrentCommand.Jump)
                    {
                        ctrl->Get = instruction & 0x1ffffffc;
                        // Console.WriteLine($"[RSX] Jump to 0x{ctrl.Get:X}");
                        continue;
                    }

                    // Call
                    if (CurrentCommand.Call)
                    {
                        _callStack.Push(ctrl->Get);
                        ctrl->Get = instruction & 0x1ffffffc;
                        // Console.WriteLine($"[RSX] Call to 0x{(instruction & 0x1ffffffc):X}");
                        continue;
                    }

                    // Return
                    if (CurrentCommand.Return)
                    {
                        ctrl->Get = _callStack.Pop() + 4;
                        // Console.WriteLine($"[RSX] Return to 0x{(ctrl.Get):X}");
                        continue;
                    }

                    ctrl->Get += 4;
                    HandleInstruction(CurrentCommand, ref *ctrl);
                }

                 //Console.WriteLine($"RSX Done, PUT: [0x{ctrl->Put:X}]");
            }
        }

        internal void PrintCommand(RSXCommand command)
        {
            Console.WriteLine($"RSX Command = 0x{command.Raw:X8} - " +
                $"NonIncrement = {command.NonIncrement} - " +
                $"Jump = {command.Jump} - " +
                $"Return = {command.Return} - " +
                $"Call = {command.Call} - " +
                $"MethodCount: 0x{command.MethodCount:X} - " +
                $"MethodRegister = 0x{command.MethodRegister:X} - " +
                $"MethodSubChannel = 0x{command.MethodSubChannel:X} - " +
                $"JumpOffset = 0x{command.JumpOffset:X} - " +
                $"CallOffset = 0x{command.CallOffset:X}");
        }

        public void StartThread()
        {
            //Console.WriteLine($"RSX Thread Started");
            RsxThreadState = RSXThreadStates.Running;
            NativeThread.Start();
        }

        public void PauseThread()
        {
            //Console.WriteLine($"RSX Thread Paused");
            RsxThreadState = RSXThreadStates.Paused;
        }

        public void StopThread()
        {
            //Console.WriteLine($"RSX Thread Stopped");
            RsxThreadState = RSXThreadStates.Stopped;
        }

        internal bool AddressIsSurface(uint address)
        {
            return GLRenderer.AddressIsSurface(address);
        }

        private void RunRSX()
        {
            // Create a window and GL renderer
            if (!_window.IsWindowCreated)
            {
                _window.CreateWindow();
            }

            try
            {
                while (!ShouldExit)
                {
                    RSXThreadMain();
                }
            }
            catch (Exception exception)
            {
                string msg = GetException(exception);
                Console.WriteLine(msg);
                ExitCode = 1;
                _threadManager.RequestShutdown();
            }
            finally
            {
                // The RSX thread is responsible of disposing the window and GL renderer
                _window.Dispose();
            }
        }

        private static string GetException(Exception exception)
        {
            string exceptionMessage = exception.Message;
            string? trace = exception.StackTrace;
            string msg = $"RSX Exception: {exceptionMessage} Trace: {trace}";
            return msg;
        }
    }
}
