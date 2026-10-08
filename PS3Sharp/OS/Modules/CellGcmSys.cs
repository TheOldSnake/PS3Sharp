using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Cell.RSX.RealitySynthesizer;

namespace PS3Sharp.OS.Modules
{
    internal class CellGcmSys
    {
        internal struct CellGcmOffsetTable : IPS3Struct
        {
            internal BigEndianGuestPtr<ushort> ioTableAddress;
            internal BigEndianGuestPtr<ushort> eaTableAddress;
        }

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

        internal const uint RSXCallbackSysID = 0x4C4C;
        internal const uint DEFAULT_SEGMENT_WORD_SIZE = 0x2000;
        internal const uint COMMAND_BUFFER_SIZE = 32 * 1024;
        internal const uint DEFAULT_COMMAND_WORD_SIZE = 0x20000;

        internal CellGcmOffsetTable OffsetTable;
        internal GuestPtr<CellGcmCtxData> DefaultCtxPtr;

        internal ulong FlipStatus;
        internal uint TotalCommandBuffers { get; private set; }
        internal GuestVoidPtr VblankHandler { get; private set; }
        internal GuestVoidPtr FlipHandler { get; private set; }

        private readonly MemoryManager _memoryManager;
        private RealitySynthesizer _rsx;

        internal CellGcmSys(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal void SetRSX(RealitySynthesizer rsx)
        {
            _rsx = rsx;
        }

        internal ulong CellGcmInitBody(PPUThread ppu)
        {
            //TODO
            uint ctx_ptr = ppu.ReadArg<uint>(ARG0);
            uint cmd_size = ppu.ReadArg<uint>(ARG1);
            uint io_size = ppu.ReadArg<uint>(ARG2);
            uint io_addr = ppu.ReadArg<uint>(ARG3);

            if (_rsx.MemoryAllocated)
            { //Can _CellGcmInitBody get called more than once?
                throw new Exception($"RSX memory already allocated"); //or we can initialize without allocating?
            }

            //Allocate RSX memory and registers
            _rsx.AllocateRSXMemory();
            DefaultCtxPtr = ctx_ptr;

            //Create a PPU thread for RSX callbacks
            ulong r2 = ppu.ReadArg<ulong>(2); //Register 2, not ARG2
            _rsx.CreateRSXPPUThread(r2, ppu.SyscallDispatcher);

            //Prepare callback
            MemoryManager.MemoryBlock callback = CreateRSXCallback();

            unsafe
            {
                // Intialize CellGcmConfig
                CellGcmConfig* initialConfig = _memoryManager.GetPtr(_rsx.ConfigPtr);
                initialConfig->Local_Size = VRAM_SIZE;
                initialConfig->Local_Addr = _rsx.VRAM_Ptr.Address;
                initialConfig->IO_Addr = io_addr;
                initialConfig->IO_Size = io_size;
                initialConfig->MemFreq = 650000000;
                initialConfig->CoreFreq = 500000000;

                // Intialize CellGcmCtxData
                CellGcmCtxData* initialCtx = _memoryManager.GetPtr(_rsx.CtxPtr);
                initialCtx->Begin = io_addr + 4096;
                initialCtx->End = io_addr + COMMAND_BUFFER_SIZE - 4;
                initialCtx->Current = io_addr + 4096;
                initialCtx->Callback = (uint)callback.VirtualAddress;

                //Initialize offset table
                InitOffsetTable(*initialConfig);
            }

            //Write the CellGcmCtxData memory address to ctx_ptr
            _memoryManager.Write<uint>(ctx_ptr, _rsx.CtxPtr.Address);


            //Start RSX thread
            _rsx.StartThread();
            TotalCommandBuffers = cmd_size / COMMAND_BUFFER_SIZE;

            Console.WriteLine($"[CellGcmSys] _cellGcmInitBody ctx = {ctx_ptr:X8} cmd_size = {cmd_size:X} io_size = {io_size:X} io_addr = {io_addr:X8} numberOfCommandBuffers = {TotalCommandBuffers}");
            return CELL_OK;
        }

        internal void InitOffsetTable(CellGcmConfig config)
        {
            const uint size = (3072 + 512) * sizeof(ushort);
            MemoryManager.MemoryBlock block = _memoryManager.AllocateVirtualMemory(size);
            _memoryManager.ClearMemory(block.VirtualAddress, block.Size, 0xFF); // The table is initialized to all FFs

            OffsetTable.ioTableAddress = block.VirtualAddress;
            OffsetTable.eaTableAddress = block.VirtualAddress + (3072 * sizeof(ushort));

            for (uint i = 0; i < (config.IO_Size >> 20); i++)
            {
                MapEaIo(config.IO_Addr + (i << 20), i << 20);
            }
        }

        internal void MapEaIo(uint ea, uint io)
        {
            uint ioPage = io >> 20;
            uint eaPage = ea >> 20;

            _memoryManager.Write<ushort>(OffsetTable.eaTableAddress + (ioPage * sizeof(ushort)), (ushort)eaPage);
            _memoryManager.Write<ushort>(OffsetTable.ioTableAddress + (eaPage * sizeof(ushort)), (ushort)ioPage);
        }

        internal bool IsIoOffsMapped(uint io)
        {
            uint ioPage = io >> 20;
            uint eaPage = _memoryManager.Read<ushort>(OffsetTable.eaTableAddress + (ioPage * sizeof(ushort)));
            return eaPage != 0xFFFF;
        }

        internal bool IsEaOffsMapped(uint ea)
        {
            uint eaPage = ea >> 20;
            uint ioPage = _memoryManager.Read<ushort>(OffsetTable.ioTableAddress + (eaPage * sizeof(ushort)));
            return ioPage != 0xFFFF;
        }

        internal uint IoToEa(uint io)
        {
            uint ea_page_table = OffsetTable.eaTableAddress;
            uint io_page = io >> 20;
            uint ea_page = _memoryManager.Read<ushort>(ea_page_table + (io_page * sizeof(ushort)));
            return (ea_page << 20) | (io & 0xFFFFF);
        }

        internal uint EaToIo(uint ea)
        {
            uint io_page_table = OffsetTable.ioTableAddress;
            uint ea_page = ea >> 20;
            uint io_page = _memoryManager.Read<ushort>(io_page_table + (ea_page * sizeof(ushort)));
            return (io_page << 20) | (ea & 0xFFFFF);
        }

        internal ulong CellGcmSetDefaultCommandBuffer(PPUThread ppu)
        {
            _memoryManager.Write<uint>(DefaultCtxPtr, _rsx.CtxPtr);
            Console.WriteLine($"[CellGcmSys] cellGcmSetDefaultCommandBuffer");
            return CELL_OK;
        }

        internal MemoryManager.MemoryBlock CreateRSXCallback()
        {
            const uint li = 0x39600000 | RSXCallbackSysID;       //li r11, RSXCallbackSysID
            const uint sc = 0x44000002;                          //syscall
            const uint blr = 0x4e800020;                         //return
            const uint totalInstructions = 4;

            MemoryManager.MemoryBlock callback = _memoryManager.AllocateVirtualMemory(totalInstructions * sizeof(uint));
            uint address = (uint)callback.VirtualAddress;

            _memoryManager.Write<uint>(address + 0, address + 4); //Write the address in the first word, the ppu code loads this word and jumps to its value
            _memoryManager.Write<uint>(address + 4, li);
            _memoryManager.Write<uint>(address + 8, sc);
            _memoryManager.Write<uint>(address + 12, blr);
            return callback;
        }

        internal ulong CellGcmCallback(PPUThread ppu)
        {
            //CellGcmCallback syscall 

            GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(ARG0);
            //Console.WriteLine($"CellGcmCallback");

            // Flush RSX remaining commands (if any)

            uint ctx_current = _memoryManager.ReadStruct<CellGcmCtxData>(ctx_ptr).Current;

            // Console.WriteLine($"Flushing RSX!");

            WorkItem rsxFlush = new(() =>
            {
                _rsx.SetPut(_rsx.AddressToOffset(ctx_current));
                // Console.WriteLine($"[RSX] Flush Call!");
                _rsx.RunCommandBuffer();
            });

            _rsx.EnqueueCommand(rsxFlush);

            // Wait
            rsxFlush.Done.Wait();

            // Console.WriteLine($"Flushing RSX Done!");

            // RSX should be waiting for PUT now, but We might need the RSX to do this too, similar to above
            uint newBegin;
            uint newEnd;

            unsafe
            {
                CellGcmCtxData* ctx = _memoryManager.GetPtr(ctx_ptr); // Ctx argument
                CellGcmConfig* config = _memoryManager.GetPtr(_rsx.ConfigPtr);
                CellGcmCtrlRegister* ctrl = _memoryManager.GetPtr(_rsx.CtrlPtr);

                // Go to next command buffer if we did not reach the last one, otherwise reset to the beginning
                uint currentCommandBuffer = (ctx->Begin - (config->IO_Addr + 4096)) / COMMAND_BUFFER_SIZE;
                uint nextCommandBuffer = currentCommandBuffer + 1;

                // Console.WriteLine($"CURRENT COMMAND BUFFER: {currentCommandBuffer}");

                if (nextCommandBuffer < TotalCommandBuffers)
                {
                    //Advance by one command buffer
                    newBegin = (nextCommandBuffer * COMMAND_BUFFER_SIZE) + config->IO_Addr + 4096;
                    newEnd = ctx->Begin + COMMAND_BUFFER_SIZE - 4;

                    //  Console.WriteLine($"NEXT COMMAND BUFFER: {nextCommandBuffer}");

                }
                else
                {
                    //Reset
                    newBegin = config->IO_Addr + 4096;
                    newEnd = config->IO_Addr + COMMAND_BUFFER_SIZE - 4;
                }

                // Inject a Jump command to the new location in the current stream
                uint jump = 0x20000000 | _rsx.AddressToOffset(newBegin);
                _memoryManager.Write<uint>(ctx_current, jump);

                // Update context struct
                ctx->Begin = newBegin;
                ctx->End = newEnd;
                ctx->Current = ctx->Begin;
            }

            //  Console.WriteLine($"Jump at: 0x{old_current:X} -- offset 0x{PS3Sharp.RSX.AddressToOffset(old_current):X}");
            //  Console.WriteLine($"Jump get: 0x{ctrl.Get:X} -- put 0x{ctrl.Put:X}");    
            return CELL_OK;
        }

        internal ulong CellGcmGetConfiguration(PPUThread ppu)
        {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetConfiguration");

            GuestPtr<CellGcmConfig> gcmConfigPtr = ppu.ReadArg<GuestPtr<CellGcmConfig>>(ARG0);

            //Ensure it was allocated
            if (_rsx.ConfigPtr.IsNullPtr)
            {
                throw new Exception($"[CellGcmSys] cellGcmGetConfiguration: config block was not allocated!");
            }


            // Copy the struct
            unsafe
            {
                CellGcmConfig* rsxConfig = _memoryManager.GetPtr(_rsx.ConfigPtr);
                CellGcmConfig* destConfig = _memoryManager.GetPtr(gcmConfigPtr);
                *destConfig = *rsxConfig;
            }

            return CELL_OK;
        }

        internal ulong CellGcmGetControlRegister(PPUThread ppu)
        {
            uint ctr_ptr = ppu.ReadArg<uint>(ARG0);
            uint ret = _rsx.CtrlPtr.Address;
            //  Console.WriteLine($"[CellGcmSys] cellGcmGetControlRegister ret = {ret:X8}");
            return ret; //Address goes to R3
        }

        internal ulong CellGcmAddressToOffset(PPUThread ppu)
        {
            uint address = ppu.ReadArg<uint>(ARG0);
            uint offset_ptr = ppu.ReadArg<uint>(ARG1);
            uint offset = _rsx.AddressToOffset(address);
            _memoryManager.Write<uint>(offset_ptr, offset);

            //  Console.WriteLine($"[CellGcmSys] cellGcmAddressToOffset address = {address:X8} offset_ptr = {offset_ptr:X8} ret = {offset:X8}");
            return CELL_OK;
        }

        internal ulong CellGcmGetLabelAddress(PPUThread ppu)
        {
            uint index = ppu.ReadArg<uint>(ARG0);
            uint ret = _rsx.LabelsPtr + (index * 0x10);
            //Console.WriteLine($"[CellGcmSys] cellGcmGetLabelAddress index = {index:X8} ret = {ret:X8}");
            return ret;
        }

        internal ulong CellGcmSetFlipMode(PPUThread ppu)
        {
            //Console.WriteLine($"[CellGcmSys] cellGcmSetFlipMode");
            uint ret = ppu.ReadArg<uint>(ARG0);
            return ret; //No return ?
        }

        internal ulong CellGcmSetDisplayBuffer(PPUThread ppu)
        {
            //const u8 bufferId,const u32 offset,const u32 pitch,const u32 width,const u32 height
            byte bufferId = ppu.ReadArg<byte>(ARG0);
            uint offset = ppu.ReadArg<uint>(ARG1);
            uint pitch = ppu.ReadArg<uint>(ARG2);
            uint width = ppu.ReadArg<uint>(ARG3);
            uint height = ppu.ReadArg<uint>(ARG4);

            _rsx.EnqueueCommand(new WorkItem(() =>
            {
                _rsx.FrameBuffers[bufferId].Address = _rsx.VRAM_Ptr.Address + offset;
                _rsx.FrameBuffers[bufferId].Pitch = pitch;
                _rsx.FrameBuffers[bufferId].Width = width;
                _rsx.FrameBuffers[bufferId].Height = height;

                if (bufferId > 1)
                {
                    //Need to handle more than 2 framebuffers ...
                    throw new Exception($"[CellGcmSys] Set Display Buffer: {bufferId}");
                }
            }));

            Console.WriteLine($"[CellGcmSys] cellGcmSetDisplayBuffer bufferId = {bufferId} offset = 0x{offset:X} pitch = 0x{pitch:X} width = {width} height = {height}");
            return CELL_OK;
        }

        internal ulong CellGcmResetFlipStatus(PPUThread ppu)
        {
            //Console.WriteLine($"[CellGcmSys] CellGcmResetFlipStatus");
            //Interlocked.Exchange<uint>(ref FlipStatus, 1);
            Interlocked.Exchange<ulong>(ref FlipStatus, 1);

            return CELL_OK;
        }

        internal ulong CellGcmGetFlipStatus(PPUThread ppu)
        {
            //Console.WriteLine($"[CellGcmSys] CellGcmGetFlipStatus: {FlipStatus}");
            return Interlocked.Read(ref FlipStatus);
        }

        internal ulong CellGcmSetFlip(PPUThread ppu)
        {
            //TODO
            GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(ARG0); // Ctx argument
            byte bufferId = ppu.ReadArg<byte>(ARG1);

            unsafe
            {
                CellGcmCtxData* ctx = _memoryManager.GetPtr(ctx_ptr);

                // Ensure enough space in the current command buffer
                if (ctx->Current + 8 >= ctx->End)
                {
                    CellGcmCallback(ppu);
                }

                // Inject a flip command
                uint flipCommand = RSXCommand.GCM_FLIP_COMMAND | (1 << RSXCommand.RSX_METHOD_COUNT_SHIFT); // 1 Argument
                _memoryManager.Write<uint>(ctx->Current, flipCommand);
                _memoryManager.Write<uint>(ctx->Current + 4, bufferId);
                ctx->Current += 8;
            }

            //Console.WriteLine($"[CellGcmSys] CellGcmSetFlip bufferId = {bufferId}");
            return CELL_OK;
        }

        internal ulong CellGcmSetWaitFlip(PPUThread ppu)
        {
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

        internal ulong CellGcmGetTiledPitchSize(PPUThread ppu)
        {
            Console.WriteLine($"[CellGcmSys] CellGcmGetTiledPitchSize");

            uint size = ppu.ReadArg<uint>(ARG0);
            int length = TILED_PITCHES.Length - 1;

            for (int i = 0; i < length; i++)
            {
                uint tile_pitch = TILED_PITCHES[i];
                uint next_tile_pitch = TILED_PITCHES[i + 1];

                if (size > tile_pitch && size <= next_tile_pitch)
                {
                    return next_tile_pitch;
                }
            }

            return CELL_OK;
        }

        internal ulong CellGcmSetTileInfo(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetTileInfo");
            return CELL_OK;
        }

        internal ulong CellGcmBindTile(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmBindTile");
            return CELL_OK;
        }

        internal ulong CellGcmSetTile(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetTile");
            return CELL_OK;
        }

        internal ulong CellGcmBindZcull(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmBindZcull");
            return CELL_OK;
        }

        internal ulong CellGcmSetDebugOutputLevel(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] CellGcmSetDebugOutputLevel");
            return CELL_OK;
        }

        internal ulong CellGcmMapMainMemory(PPUThread ppu)
        {
            //TODO

            GuestVoidPtr address = ppu.ReadArg<GuestVoidPtr>(ARG0);
            uint size = ppu.ReadArg<uint>(ARG1);
            GuestPtr<uint> offset_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG2);

            Console.WriteLine($"[CellGcmSys] cellGcmMapMainMemory -- !!");

            uint pageCount = size >> 20;

            uint freePages = 0;
            uint offs;

            for (offs = 0; offs < (MemoryManager.RAM_SIZE >> 20); offs++)
            {
                if (!IsIoOffsMapped(offs << 20))
                {
                    freePages++;
                }
                else
                {
                    freePages = 0;
                }


                if (freePages == pageCount)
                {
                    offs -= freePages - 1;

                    for (uint i = 0; i < pageCount; i++)
                    {
                        MapEaIo(address + (i << 20), (offs + i) << 20);
                    }

                    break;
                }
            }

            _memoryManager.Write<uint>(offset_ptr, offs << 20);
            return CELL_OK;
        }

        internal ulong CellGcmMapEaIoAddress(PPUThread ppu)
        {
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] cellGcmMapEaIoAddress");

            uint ea = ppu.ReadArg<uint>(ARG0);
            uint io = ppu.ReadArg<uint>(ARG1);
            uint size = ppu.ReadArg<uint>(ARG2);

            uint pageCount = size >> 20;

            for (uint i = 0; i < pageCount; i++)
            {
                uint pageOffset = i << 20;
                MapEaIo(ea + pageOffset, io + pageOffset);
            }

            return CELL_OK;
        }

        internal ulong CellGcmMapEaIoAddressWithFlags(PPUThread ppu)
        {
            // We ignore flags for now
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] CellGcmMapEaIoAddressWithFlags -> CellGcmMapEaIoAddress");
            return CellGcmMapEaIoAddress(ppu);
        }


        internal ulong CellGcmGetOffsetTable(PPUThread ppu)
        {
            GuestPtr<CellGcmOffsetTable> offset_table_ptr = ppu.ReadArg<GuestPtr<CellGcmOffsetTable>>(ARG0);
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] cellGcmGetOffsetTable");

            // Copy our internal offset table
            unsafe
            {
                *_memoryManager.GetPtr(offset_table_ptr) = OffsetTable;
            }

            return CELL_OK;
        }

        internal ulong CellGcmSetSecondVFrequency(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetSecondVFrequency");
            return CELL_OK;
        }

        internal ulong CellGcmSetVBlankFrequency(PPUThread ppu)
        {
            uint freq = ppu.ReadArg<uint>(ARG0);
            Console.WriteLine($"[CellGcmSys] cellGcmSetVBlankFrequency");
            return CELL_OK;
        }

        internal ulong CellGcmSetVBlankHandler(PPUThread ppu)
        { // very important
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetVBlankHandler");
            VblankHandler = ppu.ReadArg<GuestVoidPtr>(ARG0);

            // throw new NotImplementedException();
            return CELL_OK;
        }

        internal ulong CellGcmSetFlipHandler(PPUThread ppu)
        { // very important
            Console.WriteLine($"[CellGcmSys] cellGcmSetFlipHandler");
            FlipHandler = ppu.ReadArg<GuestVoidPtr>(ARG0);
            return CELL_OK;
        }

        internal ulong CellGcmGetLastFlipTime(PPUThread ppu)
        {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetLastFlipTime");
            return _rsx.LastFlipTime;
        }

        internal ulong CellGcmGetLastSecondVTime(PPUThread ppu)
        {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetLastSecondVTime");
            return CELL_OK;
        }

        internal ulong CellGcmGetDefaultCommandWordSize(PPUThread ppu)
        {
            Console.WriteLine($"[CellGcmSys] cellGcmGetDefaultCommandWordSize");
            return DEFAULT_COMMAND_WORD_SIZE;
        }

        internal ulong CellGcmGetDefaultSegmentWordSize(PPUThread ppu)
        {
            Console.WriteLine($"[CellGcmSys] cellGcmGetDefaultSegmentWordSize");
            return DEFAULT_SEGMENT_WORD_SIZE;
        }
    }
}
