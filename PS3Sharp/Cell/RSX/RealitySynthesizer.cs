using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Modules;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.RSX {
    public partial class RealitySynthesizer : IThread {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellGcmConfig : IPS3Struct {
            public BigEndian<uint> Local_Addr;    // Pointer to RSX local memory
            public BigEndian<uint> IO_Addr;
            public BigEndian<uint> Local_Size;    // Size of RSX memory
            public BigEndian<uint> IO_Size;
            public BigEndian<uint> MemFreq;
            public BigEndian<uint> CoreFreq;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellGcmCtxData : IPS3Struct {
            public BigEndian<uint> Begin;       /* Start address of command buffer */
            public BigEndian<uint> End;         /* End address of command buffer */
            public BigEndian<uint> Current;     /* Current address of command buffer */
            public BigEndian<uint> Callback;    /* Callback function that is called when "current" reaches "end" */
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellGcmCtrlRegister : IPS3Struct {
            public BigEndian<uint> Put;        /* member for accessing the PUT register */
            public BigEndian<uint> Get;        /* member for accessing the GET register */
            public BigEndian<uint> Ref;		   /* member for accessing the REF register. Initial value is 0xFFFFFFFF */
        }

        public class RSXFrameBuffer {
            public uint Address;
            public uint Pitch;
            public uint Width;
            public uint Height;
        }

        public enum RSXThreadStates {
            Running,
            Paused,
            Stopped
        }

        // Constants
        public const uint GCM_LOCATION_RSX = 0;     // Memory buffer is located in RSX memory.
        public const uint GCM_LOCATION_CELL = 1;    // Memory buffer is located in main memory.
        public const uint GCM_LOCATION_REPORT = 2;  // Memory buffer is located in report memory.
        public const uint VRAM_SIZE = 249 * 1024 * 1024;
        public const ulong FRAME_TIME_US = (ulong)(16.666 * 1000);

        public bool MemoryAllocated { get; private set; }

        public GuestPtr<CellGcmConfig> ConfigPtr { get; private set; }
        public GuestPtr<CellGcmCtxData> CtxPtr { get; private set; }
        public GuestPtr<CellGcmCtrlRegister> CtrlPtr { get; private set; }
        public GuestVoidPtr VRAM_Ptr { get; private set; }
        public GuestPtr<byte> LabelsPtr { get; private set; }

        public RSXFrameBuffer[] FrameBuffers { get; } = new RSXFrameBuffer[255];       
        public RSXInternalState CurrentState { get; } = new RSXInternalState();
        private static PPU.PPU? _RSX_PPU_Thread;    // for executing rsx callbacks that are ppu functions
        public Thread? NativeThread { get; set; }
        public RSXThreadStates RsxThreadState = RSXThreadStates.Stopped;

        private readonly ConcurrentQueue<WorkItem> CommandQueue = new ConcurrentQueue<WorkItem>(); // External commands from PPU thread
        public static RealitySynthesizer Instance { get; } = new RealitySynthesizer();

        private Stack<uint> CallStack = new Stack<uint>();
        private ulong LastVblank;
        public static ulong LastFlipTime;

        public void EnqueueCommand(WorkItem command) => CommandQueue.Enqueue(command);

        private RealitySynthesizer() {
            for (int i = 0; i < FrameBuffers.Length; i++) {
                FrameBuffers[i] = new RSXFrameBuffer();
            }

            for (int i = 0; i < CurrentState.VertexBuffers.Length; i++) {
                CurrentState.VertexBuffers[i] = new RSXVertexBuffer();
            }

            for (int i = 0; i < CurrentState.Textures.Length; i++) {
                CurrentState.Textures[i] = new RSXTexture();
            }

            // Create a native thread
            ThreadManager.CreateRSXThread(this);
        }

        public void CreateRSXPPUThread(ulong r2) {
            if (_RSX_PPU_Thread == null) {

                _RSX_PPU_Thread = new PPU.PPU (
                    threadName: "rsx_ppu_thread", 
                    pc: 0, 
                    threadID: 0x55555555, 
                    stackSize: MemoryManager.DEFAULT_STACK_SIZE
                );

                _RSX_PPU_Thread.WriteReg<ulong>(2, r2);
            }
        }

        public void AllocateRSXMemory(){
            AllocateVram();
            AllocateConfigReg();
            AllocateCtrReg();
            AllocateDataCtxReg();
            AllocateLabels();
            MemoryAllocated = true;
        }

        private void AllocateVram() {
            if (VRAM_Ptr == 0) {
                //Allocate local rsx memory
                VRAM_Ptr = (uint)MemoryManager.AllocateVirtualMemory(VRAM_SIZE).VirtualAddress;
            } else {
                throw new Exception("RSX VRAM was already allocated"); //Can we ignore?
            }
        }

        private void AllocateConfigReg() {
            if (ConfigPtr == 0) {
                //Allocate CellGcmConfig memory
                uint size = (uint)Unsafe.SizeOf<CellGcmConfig>();
                ConfigPtr = (uint)MemoryManager.AllocateVirtualMemory(size).VirtualAddress;
            } else {
                throw new Exception("RSX CellGcmConfig was already allocated"); //Can we ignore?
            }
        }

        private void AllocateCtrReg() {
            if (CtrlPtr == 0) {
                //Allocate CellGcmCtrlRegister memory
                uint size = (uint)Unsafe.SizeOf<CellGcmCtrlRegister>();
                CtrlPtr = (uint)MemoryManager.AllocateVirtualMemory(size).VirtualAddress;
                MemoryManager.UpdateStruct<CellGcmCtrlRegister>(CtrlPtr, (ref ctrlReg) => {
                    ctrlReg.Ref = 0xFFFFFFFF;
                });

            } else {
                throw new Exception("RSX CellGcmCtrlRegister was already allocated"); //Can we ignore?
            }
        }

        private void AllocateDataCtxReg() {
            if (CtxPtr == 0) {
                //Allocate CellGcmCtxData memory
                uint size = (uint)Unsafe.SizeOf<CellGcmCtxData>();
                CtxPtr = (uint)MemoryManager.AllocateVirtualMemory(size).VirtualAddress;
            } else {
                throw new Exception("RSX CellGcmCtxData was already allocated"); //Can we ignore?
            }
        }

        private void AllocateLabels() {
            if (LabelsPtr == 0) {
                //Allocate labels memory
                //Seems that they are indexed by u8, which is then multiplied by 0x10
                //Which means there are 256 labels, and each label is 0x10 bytes..?
                LabelsPtr = (uint)MemoryManager.AllocateVirtualMemory(0x10 * 256).VirtualAddress;
            } else {
                throw new Exception("RSX Labels were already allocated"); //Can we ignore?
            }
        }

        public uint AddressToOffset(uint address) {
            CellGcmCtxData ctx = MemoryManager.ReadStruct<CellGcmCtxData>(CtxPtr);
            CellGcmConfig config = MemoryManager.ReadStruct<CellGcmConfig>(ConfigPtr);

            if (address >= VRAM_Ptr.Address && address < VRAM_Ptr.Address + VRAM_SIZE) { // Relative to VRAM
               return address - VRAM_Ptr.Address;

            } else {

                if (CellGcmSys.IsEaOffsMapped(address)) {                              // Relative to IO space
                    return CellGcmSys.EaToIo(address);
                }

                return address - (config.IO_Addr + 4096);
            }
        }

        public uint OffsetToAddress(uint offset, uint location) {
            uint address;
            if (location == GCM_LOCATION_RSX) {
                // Relative to VRAM (Local_Addr)
                CellGcmConfig config = MemoryManager.ReadStruct<CellGcmConfig>(ConfigPtr);
                address = offset + config.Local_Addr;

            } else {
                uint ea_table = CellGcmSys.OffsetTable.eaTableAddress;
                uint io_page = offset >> 20;
                uint ea_page = MemoryManager.Read<ushort>(ea_table + (io_page * 2));
                address = (ea_page << 20) | (offset & 0xfffff);
            }

            return address;
        }

        public void UpdateGet(int value) {
            MemoryManager.UpdateStruct<CellGcmCtrlRegister>(CtrlPtr, (ref ctrl) => {
                ctrl.Get += (uint)value;
            });
        }

        public void SetPut(uint value) {
            MemoryManager.UpdateStruct<CellGcmCtrlRegister>(CtrlPtr, (ref ctrl) => {
                ctrl.Put = value;
            });
        }

        public void SetGet(uint value) {
            MemoryManager.UpdateStruct<CellGcmCtrlRegister>(CtrlPtr, (ref ctrl) => {
                ctrl.Get = value;
            });
        }

        public void RSXThreadMain() {
            // CellGcmCtrlRegister ctrl = MemoryManager.ReadStruct<CellGcmCtrlRegister>(GCMCtrl_Address);
            unsafe {
                CellGcmCtrlRegister* ctrl = CtrlPtr.AsNativePtr();

                if (ctrl->Put != ctrl->Get) {
                    RunCommandBuffer();
                }
            }

            //Run any pending commands
            while (CommandQueue.TryDequeue(out var command)) {
                command.Work();
                command.Done.Set(); //Signal incase the main thread is waiting
            }

            // vblank every 1000 µs, for some reason
            ulong current_us = Utility.ReadTime_Microseconds();
            if (current_us - LastVblank >= 1000) {
                Vblank();
                LastVblank = current_us;

                // Also Process window events
                WindowManager.ProcessEvents();
            }
        }

        public void RunCommandBuffer() {

            unsafe {
                CellGcmCtrlRegister* ctrl = CtrlPtr.AsNativePtr();
                CellGcmCtxData* ctx = CtxPtr.AsNativePtr();

                //Console.WriteLine($"[RSX] Current GET: [0x{ctrl.Get:X8}] Running until: [0x{ctrl.Put:X8}]");

                while (ctrl->Get != ctrl->Put) {
                    uint currentAddress = CellGcmSys.IoToEa(ctrl->Get);
                    uint instruction = MemoryManager.ReadRSXInstruction(currentAddress);
                    RSXCommand CurrentCommand = new RSXCommand { Raw = instruction };

                    //Console.WriteLine($"[0x{currentAddress:X}] cmd: 0x{instruction:X} get: [0x{ctrl.Get:X}]");

                    //Jump
                    if (CurrentCommand.Jump) {
                        ctrl->Get = instruction & 0x1ffffffc;
                        //Console.WriteLine($"[RSX] Jump to 0x{ctrl.Get:X}");
                        continue;
                    }

                    //Call
                    if (CurrentCommand.Call) {
                        CallStack.Push(ctrl->Get);
                        ctrl->Get = instruction & 0x1ffffffc;
                        //Console.WriteLine($"[RSX] Call to 0x{(instruction & 0x1ffffffc):X}");
                        continue;
                    }

                    //TODO
                    if (CurrentCommand.Return) {
                        ctrl->Get = CallStack.Pop() + 4;
                        //Console.WriteLine($"[RSX] Return to 0x{(ctrl.Get):X}");
                        continue;
                    }

                    ctrl->Get += 4;
                    HandleInstruction(CurrentCommand, ctrl);
                }
            }

            //Console.WriteLine($"RSX Done, PUT: [0x{ctrl.Put:X}]");

        }

        public void PrintCommand(RSXCommand command) {
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

        public void StartThread() {
            //Console.WriteLine($"RSX Thread Started");
            RsxThreadState = RSXThreadStates.Running;

            // Initialize the window and the renderer
            if (!WindowManager.IsInitialized) {
                WindowManager.InitializeWindow();
                GLRenderer.InitializeRenderer();
            }
        }

        public void PauseThread() {
            //Console.WriteLine($"RSX Thread Paused");
            RsxThreadState = RSXThreadStates.Paused;
        }

        public void StopThread() {
            //Console.WriteLine($"RSX Thread Stopped");
            RsxThreadState = RSXThreadStates.Stopped;
        }
    }
}
