using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Cell.RSX.RealitySynthesizer;

namespace PS3Sharp.OS.Modules {
    public static class CellGcmSys {
        private static ReadOnlySpan<uint> TILED_PITCHES => [
            0x00000000, 0x00000200, 0x00000300, 0x00000400,
            0x00000500, 0x00000600, 0x00000700, 0x00000800,
            0x00000A00, 0x00000C00, 0x00000D00, 0x00000E00,
            0x00001000, 0x00001400, 0x00001800, 0x00001A00,
            0x00001C00, 0x00002000, 0x00002800, 0x00003000,
            0x00003400, 0x00003800, 0x00004000, 0x00005000,
            0x00006000, 0x00006800, 0x00007000, 0x00008000,
            0x0000A000, 0x0000C000, 0x0000D000, 0x0000E000,
            0x00010000
        ];

        const uint DEFAULT_COMMAND_WORD_SIZE = 0x20000;
        const uint DEFAULT_SEGMENT_WORD_SIZE = 0x2000;
        const uint COMMAND_BUFFER_SIZE = 32 * 1024;
        static uint TotalCommandBuffers = 0;

        private static GuestPtr<CellGcmCtxData> DefaultCtxPtr;
        public static GuestVoidPtr VblankHandler;
        public static GuestVoidPtr FlipHandler;
        public static ulong FlipStatus = 0;

         public struct CellGcmOffsetTable : IPS3Struct {
            public BigEndianGuestPtr<ushort> ioTableAddress;
            public BigEndianGuestPtr<ushort> eaTableAddress;
        }

        public static CellGcmOffsetTable OffsetTable;

        public static ulong CellGcmInitBody(PPU ppu) {
            //TODO
            uint ctx_ptr  = ppu.ReadArg<uint>(ARG0);
            uint cmd_size = ppu.ReadArg<uint>(ARG1);
            uint io_size  = ppu.ReadArg<uint>(ARG2);
            uint io_addr  = ppu.ReadArg<uint>(ARG3);

            if (PS3Sharp.RSX.MemoryAllocated) { //Can _CellGcmInitBody get called more than once?
                throw new Exception($"RSX memory already allocated"); //or we can initialize without allocating?
            }

            //Allocate RSX memory and registers
            PS3Sharp.RSX.AllocateRSXMemory();
            DefaultCtxPtr = ctx_ptr;

            //Create a PPU thread for RSX callbacks
            ulong r2 = ppu.ReadArg<ulong>(2); //Register 2, not ARG2
            PS3Sharp.RSX.CreateRSXPPUThread(r2);

            //Prepare callback
            MemoryManager.MemoryBlock callback = CreateRSXCallback();

            unsafe {
                // Intialize CellGcmConfig
                CellGcmConfig* initialConfig = PS3Sharp.RSX.ConfigPtr.AsNativePtr();
                initialConfig->Local_Size = VRAM_SIZE;
                initialConfig->Local_Addr = PS3Sharp.RSX.VRAM_Ptr.Address;
                initialConfig->IO_Addr = io_addr;
                initialConfig->IO_Size = io_size;
                initialConfig->MemFreq = 650000000;
                initialConfig->CoreFreq = 500000000;

                // Intialize CellGcmCtxData
                CellGcmCtxData* initialCtx = PS3Sharp.RSX.CtxPtr.AsNativePtr();
                initialCtx->Begin = io_addr + 4096;
                initialCtx->End = io_addr + COMMAND_BUFFER_SIZE - 4;
                initialCtx->Current = io_addr + 4096;
                initialCtx->Callback = (uint)callback.VirtualAddress;

                //Initialize offset table
                InitOffsetTable(*initialConfig);
            }

            //Write the CellGcmCtxData memory address to ctx_ptr
            MemoryManager.Write<uint>(ctx_ptr, PS3Sharp.RSX.CtxPtr.Address);


            //Start RSX thread
            ThreadManager.RunRSXThread();
            TotalCommandBuffers = cmd_size / COMMAND_BUFFER_SIZE;

            Console.WriteLine($"[CellGcmSys] _cellGcmInitBody ctx = {ctx_ptr:X8} cmd_size = {cmd_size:X} io_size = {io_size:X} io_addr = {io_addr:X8} numberOfCommandBuffers = {TotalCommandBuffers}");
            return CELL_OK;
        }

        public static void InitOffsetTable(CellGcmConfig config) {
            const uint size = (3072 + 512) * sizeof(ushort);
            MemoryManager.MemoryBlock block = MemoryManager.AllocateVirtualMemory(size);
            MemoryManager.ClearMemory(block.VirtualAddress, block.Size, 0xFF); // The table is initialized to all FFs

            OffsetTable.ioTableAddress = block.VirtualAddress;
            OffsetTable.eaTableAddress = block.VirtualAddress + (3072 * sizeof(ushort));

            for (uint i = 0; i < (config.IO_Size >> 20); i++) {
                MapEaIo(config.IO_Addr + (i << 20), i << 20);
            }
        }

        public static void MapEaIo(uint ea, uint io) {
            uint ioPage = io >> 20;
            uint eaPage = ea >> 20;

            MemoryManager.Write<ushort>(OffsetTable.eaTableAddress + (ioPage * sizeof(ushort)), (ushort)eaPage);
            MemoryManager.Write<ushort>(OffsetTable.ioTableAddress + (eaPage * sizeof(ushort)), (ushort)ioPage);
        }

        public static bool IsIoOffsMapped(uint io) {
            uint ioPage = io >> 20;
            uint eaPage = MemoryManager.Read<ushort>(OffsetTable.eaTableAddress + (ioPage * sizeof(ushort)));
            return eaPage != 0xFFFF;
        }

        public static bool IsEaOffsMapped(uint ea) {
            uint eaPage = ea >> 20;
            uint ioPage = MemoryManager.Read<ushort>(OffsetTable.ioTableAddress + (eaPage * sizeof(ushort)));
            return ioPage != 0xFFFF;
        }

        public static uint IoToEa(uint io) {
            uint ea_page_table = CellGcmSys.OffsetTable.eaTableAddress;
            uint io_page = io >> 20;
            uint ea_page = MemoryManager.Read<ushort>(ea_page_table + (io_page * sizeof(ushort)));
            return (ea_page << 20) | (io & 0xFFFFF);
        }

        public static uint EaToIo(uint ea) {
            uint io_page_table = CellGcmSys.OffsetTable.ioTableAddress;
            uint ea_page = ea >> 20;
            uint io_page = MemoryManager.Read<ushort>(io_page_table + (ea_page * sizeof(ushort)));
            return (io_page << 20) | (ea & 0xFFFFF);
        }

        public static ulong CellGcmSetDefaultCommandBuffer(PPU ppu) {
            MemoryManager.Write<uint>(DefaultCtxPtr, PS3Sharp.RSX.CtxPtr);
            Console.WriteLine($"[CellGcmSys] cellGcmSetDefaultCommandBuffer");
            return CELL_OK;
        }


        public const uint RSXCallbackSysID = 0x4C4C;

        public static MemoryManager.MemoryBlock CreateRSXCallback() {
            const uint li = 0x39600000 | RSXCallbackSysID;       //li r11, RSXCallbackSysID
            const uint sc = 0x44000002;                          //syscall
            const uint blr = 0x4e800020;                         //return
            const uint totalInstructions = 4;

            MemoryManager.MemoryBlock callback = MemoryManager.AllocateVirtualMemory(totalInstructions * sizeof(uint));
            uint address = (uint)callback.VirtualAddress;

            MemoryManager.Write<uint>(address + 0, address + 4); //Write the address in the first word, the ppu code loads this word and jumps to its value
            MemoryManager.Write<uint>(address + 4, li);
            MemoryManager.Write<uint>(address + 8, sc);
            MemoryManager.Write<uint>(address + 12, blr);
            return callback;
        }

        public static ulong CellGcmCallback(PPU ppu) {
            //CellGcmCallback syscall 

            GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(ARG0);
            //Console.WriteLine($"CellGcmCallback");

            // Flush RSX remaining commands (if any)

            uint ctx_current = ctx_ptr.Read().Current;

           // Console.WriteLine($"Flushing RSX!");

            WorkItem rsxFlush = new WorkItem(() => {
                PS3Sharp.RSX.SetPut(PS3Sharp.RSX.AddressToOffset(ctx_current));
               // Console.WriteLine($"[RSX] Flush Call!");
                PS3Sharp.RSX.RunCommandBuffer();
            });

            PS3Sharp.RSX.EnqueueCommand(rsxFlush);

            // Wait
            rsxFlush.Done.Wait();

           // Console.WriteLine($"Flushing RSX Done!");

            // RSX should be waiting for PUT now, but We might need the RSX to do this too, similar to above
            uint newBegin;
            uint newEnd;

            unsafe {
                CellGcmCtxData* ctx = ctx_ptr.AsNativePtr(); // Ctx argument
                CellGcmConfig* config = PS3Sharp.RSX.ConfigPtr.AsNativePtr();
                CellGcmCtrlRegister* ctrl = PS3Sharp.RSX.CtrlPtr.AsNativePtr();

                // Go to next command buffer if we did not reach the last one, otherwise reset to the beginning
                uint currentCommandBuffer = (ctx->Begin - (config->IO_Addr + 4096)) / COMMAND_BUFFER_SIZE;
                uint nextCommandBuffer = currentCommandBuffer + 1;

                // Console.WriteLine($"CURRENT COMMAND BUFFER: {currentCommandBuffer}");

                if (nextCommandBuffer < TotalCommandBuffers) {
                    //Advance by one command buffer
                    newBegin = (nextCommandBuffer * COMMAND_BUFFER_SIZE) + config->IO_Addr + 4096;
                    newEnd = ctx->Begin + COMMAND_BUFFER_SIZE - 4;

                    //  Console.WriteLine($"NEXT COMMAND BUFFER: {nextCommandBuffer}");

                } else {
                    //Reset
                    newBegin = config->IO_Addr + 4096;
                    newEnd = config->IO_Addr + COMMAND_BUFFER_SIZE - 4;
                }

                // Inject a Jump command to the new location in the current stream
                uint jump = 0x20000000 | PS3Sharp.RSX.AddressToOffset(newBegin);
                MemoryManager.Write<uint>(ctx_current, jump);

                // Update context struct
                ctx->Begin = newBegin;
                ctx->End = newEnd;
                ctx->Current = ctx->Begin;
            }

          //  Console.WriteLine($"Jump at: 0x{old_current:X} -- offset 0x{PS3Sharp.RSX.AddressToOffset(old_current):X}");
          //  Console.WriteLine($"Jump get: 0x{ctrl.Get:X} -- put 0x{ctrl.Put:X}");    
            return CELL_OK;
        }

        public static ulong CellGcmGetConfiguration(PPU ppu) {
           // Console.WriteLine($"[CellGcmSys] cellGcmGetConfiguration");

            GuestPtr<CellGcmConfig> gcmConfigPtr = ppu.ReadArg<GuestPtr<CellGcmConfig>>(ARG0);
            
            //Ensure it was allocated
            if (PS3Sharp.RSX.ConfigPtr.IsNullPtr) {
                throw new Exception($"[CellGcmSys] cellGcmGetConfiguration: config block was not allocated!");
            }


            // Copy the struct
            unsafe {
               CellGcmConfig* rsxConfig = PS3Sharp.RSX.ConfigPtr.AsNativePtr();
               CellGcmConfig* destConfig = gcmConfigPtr.AsNativePtr();
               *destConfig = *rsxConfig;
            }

            return CELL_OK;
        }

        public static ulong CellGcmGetControlRegister(PPU ppu) {
            uint ctr_ptr = ppu.ReadArg<uint>(ARG0);
            uint ret = PS3Sharp.RSX.CtrlPtr.Address;
          //  Console.WriteLine($"[CellGcmSys] cellGcmGetControlRegister ret = {ret:X8}");
            return ret; //Address goes to R3
        }

        public static ulong CellGcmAddressToOffset(PPU ppu) {
            uint address = ppu.ReadArg<uint>(ARG0);
            uint offset_ptr = ppu.ReadArg<uint>(ARG1);
            uint offset = PS3Sharp.RSX.AddressToOffset(address);
            MemoryManager.Write<uint>(offset_ptr, offset);

          //  Console.WriteLine($"[CellGcmSys] cellGcmAddressToOffset address = {address:X8} offset_ptr = {offset_ptr:X8} ret = {offset:X8}");
            return CELL_OK;
        }

        public static ulong CellGcmGetLabelAddress(PPU ppu) {
            uint index = ppu.ReadArg<uint>(ARG0);
            uint ret = PS3Sharp.RSX.LabelsPtr + (index * 0x10);
            //Console.WriteLine($"[CellGcmSys] cellGcmGetLabelAddress index = {index:X8} ret = {ret:X8}");
            return ret;
        }

        public static ulong CellGcmSetFlipMode(PPU ppu) {
            //Console.WriteLine($"[CellGcmSys] cellGcmSetFlipMode");
            uint ret = ppu.ReadArg<uint>(ARG0);
            return ret; //No return ?
        }

        public static ulong CellGcmSetDisplayBuffer(PPU ppu) {
            //const u8 bufferId,const u32 offset,const u32 pitch,const u32 width,const u32 height
            byte bufferId = ppu.ReadArg<byte>(ARG0);
            uint offset   = ppu.ReadArg<uint>(ARG1);
            uint pitch    = ppu.ReadArg<uint>(ARG2);
            uint width    = ppu.ReadArg<uint>(ARG3);
            uint height   = ppu.ReadArg<uint>(ARG4);

            PS3Sharp.RSX.EnqueueCommand(new WorkItem(() => {
                PS3Sharp.RSX.FrameBuffers[bufferId].Address = PS3Sharp.RSX.VRAM_Ptr.Address + offset;
                PS3Sharp.RSX.FrameBuffers[bufferId].Pitch = pitch;
                PS3Sharp.RSX.FrameBuffers[bufferId].Width = width;
                PS3Sharp.RSX.FrameBuffers[bufferId].Height = height;

                if (bufferId > 1) {
                    //Need to handle more than 2 framebuffers ...
                    throw new Exception($"[CellGcmSys] Set Display Buffer: {bufferId}");
                }
            }));
            
            Console.WriteLine($"[CellGcmSys] cellGcmSetDisplayBuffer bufferId = {bufferId} offset = 0x{offset:X} pitch = 0x{pitch:X} width = {width} height = {height}");
            return CELL_OK;
        }

        public static ulong CellGcmResetFlipStatus(PPU ppu) {
            //Console.WriteLine($"[CellGcmSys] CellGcmResetFlipStatus");
            //Interlocked.Exchange<uint>(ref FlipStatus, 1);
            Interlocked.Exchange<ulong>(ref FlipStatus, 1);

            return CELL_OK;
        }

        public static ulong CellGcmGetFlipStatus(PPU ppu) {
            //Console.WriteLine($"[CellGcmSys] CellGcmGetFlipStatus: {FlipStatus}");
            return Interlocked.Read(ref FlipStatus); 
        }

        public static ulong CellGcmSetFlip(PPU ppu) {
            //TODO
            GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(ARG0); // Ctx argument
            byte bufferId = ppu.ReadArg<byte>(ARG1);

            unsafe {
                CellGcmCtxData* ctx = ctx_ptr.AsNativePtr();

                // Ensure enough space in the current command buffer
                if (ctx->Current + 8 >= ctx->End) {
                    CellGcmCallback(ppu);
                }

                // Inject a flip command
                uint flipCommand = RSXCommand.GCM_FLIP_COMMAND | (1 << RSXCommand.RSX_METHOD_COUNT_SHIFT); // 1 Argument
                MemoryManager.Write<uint>(ctx->Current, flipCommand);
                MemoryManager.Write<uint>(ctx->Current + 4, bufferId);
                ctx->Current += 8;
            }

            //Console.WriteLine($"[CellGcmSys] CellGcmSetFlip bufferId = {bufferId}");
            return CELL_OK;
        }

        public static ulong CellGcmSetWaitFlip(PPU ppu) {
            //Console.WriteLine($"[CellGcmSys] CellGcmSetWaitFlip: {ppu.PC:X8}");

           /* GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(ARG0);
            ref CellGcmCtxData ctx = ref ctx_ptr.AsRef(); // Ctx argument

            // Ensure enough space in the current command buffer
            if (ctx.Current + 16 >= ctx.End) {
                CellGcmCallback(ppu);
            }

            using (MemoryManager.AcquireTemporaryLock()) {
                // Inject a command to write 0 
                uint offsetCommand = RSXCommand.NV406ETCL_SEMAPHORE_OFFSET | (1 << RSXCommand.RSX_METHOD_COUNT_SHIFT); // 1 Argument
                uint writeCommand = RSXCommand.NV406ETCL_SEMAPHORE_ACQUIRE | (1 << RSXCommand.RSX_METHOD_COUNT_SHIFT); // 1 Argument

                MemoryManager.Write<uint>(ctx.Current, offsetCommand);
                MemoryManager.Write<uint>(ctx.Current + 4, 0x10);

                MemoryManager.Write<uint>(ctx.Current + 8, writeCommand);
                MemoryManager.Write<uint>(ctx.Current + 12, 0);
                ctx.Current += 16;
            }*/

            //Console.WriteLine($"[CellGcmSys] CellGcmSetWaitFlip ok");
            return CELL_OK;
        }

        public static ulong CellGcmGetTiledPitchSize(PPU ppu) {
            Console.WriteLine($"[CellGcmSys] CellGcmGetTiledPitchSize");

            uint size = ppu.ReadArg<uint>(ARG0);
            int length = TILED_PITCHES.Length - 1;

            for (int i = 0; i < length; i++) {
                uint tile_pitch = TILED_PITCHES[i];
                uint next_tile_pitch = TILED_PITCHES[i + 1];

                if (size > tile_pitch && size <= next_tile_pitch) {
                    return next_tile_pitch;
                }
            }

            return CELL_OK;
        }

        public static ulong CellGcmSetTileInfo(PPU ppu) {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetTileInfo");
            return CELL_OK;
        }

        public static ulong CellGcmBindTile(PPU ppu) {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmBindTile");
            return CELL_OK;
        }

        public static ulong CellGcmSetTile(PPU ppu) {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetTile");
            return CELL_OK;
        }

        public static ulong CellGcmBindZcull(PPU ppu) {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmBindZcull");
            return CELL_OK;
        }

        public static ulong CellGcmSetDebugOutputLevel(PPU ppu) {
            //TODO
            Console.WriteLine($"[CellGcmSys] CellGcmSetDebugOutputLevel");
            return CELL_OK;
        }

        public static ulong CellGcmMapMainMemory(PPU ppu) {
            //TODO

            GuestVoidPtr address = ppu.ReadArg<GuestVoidPtr>(ARG0);
            uint size = ppu.ReadArg<uint>(ARG1);
            GuestPtr<uint> offset_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG2);

            Console.WriteLine($"[CellGcmSys] cellGcmMapMainMemory -- !!");

            uint pageCount = size >> 20; 

            uint freePages = 0;
            uint offs;

            for (offs = 0; offs < (MemoryManager.RAM_SIZE >> 20); offs++) {
                if (!IsIoOffsMapped(offs << 20)) {
                    freePages++;
                } else {
                    freePages = 0;
                }


                if (freePages == pageCount) {
                    offs -= freePages - 1;

                    for (uint i = 0; i < pageCount; i++) {
                        MapEaIo(address + (i << 20), (offs + i) << 20);
                    }

                    break;
                }
            }

             MemoryManager.Write<uint>(offset_ptr, offs << 20);
            return CELL_OK;
        }

        public static ulong CellGcmMapEaIoAddress(PPU ppu) {
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] cellGcmMapEaIoAddress");

            uint ea = ppu.ReadArg<uint>(ARG0);
            uint io = ppu.ReadArg<uint>(ARG1);
            uint size = ppu.ReadArg<uint>(ARG2);

            uint pageCount = size >> 20;

            for (uint i = 0; i < pageCount; i++) {
                uint pageOffset = i << 20;
                MapEaIo(ea + pageOffset, io + pageOffset);
            }

            return CELL_OK;
        }

        public static ulong CellGcmMapEaIoAddressWithFlags(PPU ppu) {
            // We ignore flags for now
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] CellGcmMapEaIoAddressWithFlags -> CellGcmMapEaIoAddress");
            return CellGcmMapEaIoAddress(ppu); 
        }


        public static ulong CellGcmGetOffsetTable(PPU ppu) {
            GuestPtr<CellGcmOffsetTable> offset_table_ptr = ppu.ReadArg<GuestPtr<CellGcmOffsetTable>>(ARG0);
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] cellGcmGetOffsetTable");

            // Copy our internal offset table
            unsafe {
                *offset_table_ptr.AsNativePtr() = OffsetTable;
            }

            return CELL_OK;
        }

        public static ulong CellGcmSetSecondVFrequency(PPU ppu) {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetSecondVFrequency");
            return CELL_OK;
        }

        public static ulong CellGcmSetVBlankFrequency(PPU ppu) {
            uint freq = ppu.ReadArg<uint>(ARG0);
            Console.WriteLine($"[CellGcmSys] cellGcmSetVBlankFrequency");
            return CELL_OK;
        }

        public static ulong CellGcmSetVBlankHandler(PPU ppu) { // very important
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetVBlankHandler");
            VblankHandler = ppu.ReadArg<GuestVoidPtr>(ARG0);

           // throw new NotImplementedException();
            return CELL_OK;
        }

        public static ulong CellGcmSetFlipHandler(PPU ppu) { // very important
            Console.WriteLine($"[CellGcmSys] cellGcmSetFlipHandler");
            FlipHandler = ppu.ReadArg<GuestVoidPtr>(ARG0);
            return CELL_OK;
        }   

        public static ulong CellGcmGetLastFlipTime(PPU ppu) {
           // Console.WriteLine($"[CellGcmSys] cellGcmGetLastFlipTime");
            return RealitySynthesizer.LastFlipTime;
        }

        public static ulong CellGcmGetLastSecondVTime(PPU ppu) {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetLastSecondVTime");
            return CELL_OK;
        }

        public static ulong CellGcmGetDefaultCommandWordSize(PPU ppu) {
            Console.WriteLine($"[CellGcmSys] cellGcmGetDefaultCommandWordSize");
            return DEFAULT_COMMAND_WORD_SIZE;
        }   

        public static ulong CellGcmGetDefaultSegmentWordSize(PPU ppu) {
            Console.WriteLine($"[CellGcmSys] cellGcmGetDefaultSegmentWordSize");
            return DEFAULT_SEGMENT_WORD_SIZE;
        }     
    }
}
