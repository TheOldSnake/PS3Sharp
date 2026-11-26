using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.RSX;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.Cell.RSX.RealitySynthesizer;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellGcmSys
    {
        internal struct CellGcmOffsetTable : IPS3Struct
        {
            internal BigEndianGuestPtr<BigEndian<ushort>> ioTableAddress;
            internal BigEndianGuestPtr<BigEndian<ushort>> eaTableAddress;
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
        internal const uint DefaultSegmentWordSize = 0x2000;
        internal const uint CommandBufferSize = 32 * 1024;
        internal const uint DefaultCommandWordSize = 0x20000;

        private readonly MemoryManager _memoryManager;
        private RealitySynthesizer _rsx;

        internal CellGcmOffsetTable OffsetTable { get; set; }
        internal GuestPtr<CellGcmCtxData> DefaultCtxPtr { get; set; }

        internal ulong FlipStatus;
        internal uint TotalCommandBuffers { get; private set; }
        internal GuestVoidPtr VblankHandler { get; private set; }
        internal GuestVoidPtr FlipHandler { get; private set; }

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
            uint ctx_ptr = ppu.ReadArg<uint>(Arg0);
            uint cmd_size = ppu.ReadArg<uint>(Arg1);
            uint io_size = ppu.ReadArg<uint>(Arg2);
            uint io_addr = ppu.ReadArg<uint>(Arg3);

            // Can _CellGcmInitBody get called more than once?
            if (_rsx.MemoryAllocated)
            { 
                throw new Exception($"RSX memory already allocated"); //or we can initialize without allocating?
            }

            // Allocate RSX memory and registers
            _rsx.AllocateRSXMemory();
            DefaultCtxPtr = ctx_ptr;

            // Create a PPU thread for RSX callbacks
            ulong r2 = ppu.ReadArg<ulong>(2); //Register 2, not ARG2
            _rsx.CreateRSXPPUThread(r2, ppu.SyscallDispatcher);

            // Prepare callback
            MemoryManager.MemoryBlock callback = CreateRSXCallback();

            // Intialize CellGcmConfig
            CellGcmConfig initialConfig = new()
            {
                Local_Size = VramSize,
                Local_Addr = _rsx.VRAMPtr.Address,
                IO_Addr = io_addr,
                IO_Size = io_size,
                MemFreq = 650000000,
                CoreFreq = 500000000
            };
           
            _memoryManager.Write(_rsx.ConfigPtr, initialConfig);

            // Intialize CellGcmCtxData
            CellGcmCtxData initialCtx = new()
            {
                Begin = io_addr + 4096,
                End = io_addr + CommandBufferSize - 4,
                Current = io_addr + 4096,
                Callback = callback.VirtualAddress
            };

            _memoryManager.Write(_rsx.CtxPtr, initialCtx);

            //Initialize offset table
            InitOffsetTable(ref initialConfig);

            //Write the CellGcmCtxData memory address to ctx_ptr
            _memoryManager.Write<BigEndian<uint>>(ctx_ptr, _rsx.CtxPtr.Address);

            //Start RSX thread
            _rsx.StartThread();
            TotalCommandBuffers = cmd_size / CommandBufferSize;

            Console.WriteLine($"[CellGcmSys] _cellGcmInitBody ctx = {ctx_ptr:X8} cmd_size = {cmd_size:X} io_size = {io_size:X} io_addr = {io_addr:X8} numberOfCommandBuffers = {TotalCommandBuffers}");
            return CellOk;
        }

        internal void InitOffsetTable(ref CellGcmConfig config)
        {
            const uint size = (3072 + 512) * sizeof(ushort);
            MemoryManager.MemoryBlock block = _memoryManager.Allocator.AllocateVirtualMemory(size);
            _memoryManager.ClearMemory(block.VirtualAddress, (int)block.Size, 0xFF); // The table is initialized to all FFs

            OffsetTable = new()
            {
                ioTableAddress = block.VirtualAddress,
                eaTableAddress = block.VirtualAddress + (3072 * sizeof(ushort))
            };

            for (uint i = 0; i < (config.IO_Size >> 20); i++)
            {
                MapEaIo(config.IO_Addr + (i << 20), i << 20);
            }
        }

        internal void MapEaIo(uint ea, uint io)
        {
            uint ioPage = io >> 20;
            uint eaPage = ea >> 20;

            _memoryManager.Write<BigEndian<ushort>>(OffsetTable.eaTableAddress + (ioPage * sizeof(ushort)), (ushort)eaPage);
            _memoryManager.Write<BigEndian<ushort>>(OffsetTable.ioTableAddress + (eaPage * sizeof(ushort)), (ushort)ioPage);
        }

        internal bool IsIoOffsMapped(uint io)
        {
            uint ioPage = io >> 20;
            uint eaPage = _memoryManager.Read<BigEndian<ushort>>(OffsetTable.eaTableAddress + (ioPage * sizeof(ushort)));
            return eaPage != 0xFFFF;
        }

        internal bool IsEaOffsMapped(uint ea)
        {
            uint eaPage = ea >> 20;
            uint ioPage = _memoryManager.Read<BigEndian<ushort>>(OffsetTable.ioTableAddress + (eaPage * sizeof(ushort)));
            return ioPage != 0xFFFF;
        }

        internal uint IoToEa(uint io)
        {
            uint ea_page_table = OffsetTable.eaTableAddress;
            uint io_page = io >> 20;
            uint ea_page = _memoryManager.Read<BigEndian<ushort>>(ea_page_table + (io_page * sizeof(ushort)));
            return (ea_page << 20) | (io & 0xFFFFF);
        }

        internal uint EaToIo(uint ea)
        {
            uint io_page_table = OffsetTable.ioTableAddress;
            uint ea_page = ea >> 20;
            uint io_page = _memoryManager.Read<BigEndian<ushort>>(io_page_table + (ea_page * sizeof(ushort)));
            return (io_page << 20) | (ea & 0xFFFFF);
        }

        internal ulong CellGcmSetDefaultCommandBuffer(PPUThread ppu)
        {
            uint ptr = _rsx.CtxPtr;
            _memoryManager.Write<BigEndian<uint>>(DefaultCtxPtr, ptr);
            Console.WriteLine($"[CellGcmSys] cellGcmSetDefaultCommandBuffer");
            return CellOk;
        }

        internal MemoryManager.MemoryBlock CreateRSXCallback()
        {
            const uint li = 0x39600000 | RSXCallbackSysID;       //li r11, RSXCallbackSysID
            const uint sc = 0x44000002;                          //syscall
            const uint blr = 0x4e800020;                         //return
            const uint totalInstructions = 4;

            MemoryManager.MemoryBlock callback = _memoryManager.Allocator.AllocateVirtualMemory(totalInstructions * sizeof(uint));
            uint address = (uint)callback.VirtualAddress;

            _memoryManager.Write<BigEndian<uint>>(address + 0, address + 4); //Write the address in the first word, the ppu code loads this word and jumps to its value
            _memoryManager.Write<BigEndian<uint>>(address + 4, li);
            _memoryManager.Write<BigEndian<uint>>(address + 8, sc);
            _memoryManager.Write<BigEndian<uint>>(address + 12, blr);
            return callback;
        }

        internal ulong CellGcmCallback(PPUThread ppu)
        {
            //CellGcmCallback syscall 

            GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(Arg0);
            //Console.WriteLine($"CellGcmCallback");

            // Flush RSX remaining commands (if any)

            uint ctx_current = _memoryManager.Read<CellGcmCtxData>(ctx_ptr).Current;

            // Console.WriteLine($"Flushing RSX!");

            WorkItem rsxFlush = new(() =>
            {
                _rsx.SetPut(_rsx.AddressToOffset(ctx_current));
                _rsx.RunCommandBuffer();
            });

            _rsx.EnqueueCommand(rsxFlush);

            // Wait
            rsxFlush.Done.Wait();

            // Console.WriteLine($"Flushing RSX Done!");

            // RSX should be waiting for PUT now, but We might need the RSX to do this too, similar to above
            uint newBegin;
            uint newEnd;

            CellGcmCtxData ctx = _memoryManager.Read<CellGcmCtxData>(ctx_ptr); // Ctx argument
            CellGcmConfig config = _memoryManager.Read<CellGcmConfig>(_rsx.ConfigPtr);
            CellGcmCtrlRegister ctrl = _memoryManager.Read<CellGcmCtrlRegister>(_rsx.CtrlPtr);

            // Go to next command buffer if we did not reach the last one, otherwise reset to the beginning
            uint currentCommandBuffer = (ctx.Begin - (config.IO_Addr + 4096)) / CommandBufferSize;
            uint nextCommandBuffer = currentCommandBuffer + 1;

            // Console.WriteLine($"CURRENT COMMAND BUFFER: {currentCommandBuffer}");

            if (nextCommandBuffer < TotalCommandBuffers)
            {
                // Advance by one command buffer
                newBegin = (nextCommandBuffer * CommandBufferSize) + config.IO_Addr + 4096;
                newEnd = ctx.Begin + CommandBufferSize - 4;
            }
            else
            {
                // Reset
                newBegin = config.IO_Addr + 4096;
                newEnd = config.IO_Addr + CommandBufferSize - 4;
            }

            // Inject a Jump command to the new location in the current stream
            uint jump = 0x20000000 | _rsx.AddressToOffset(newBegin);
            _memoryManager.Write<BigEndian<uint>>(ctx_current, jump);

            // Update context struct
            ctx.Begin = newBegin;
            ctx.End = newEnd;
            ctx.Current = ctx.Begin;
            _memoryManager.Write(ctx_ptr, ctx);

            return CellOk;
        }

        internal ulong CellGcmGetConfiguration(PPUThread ppu)
        {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetConfiguration");

            GuestPtr<CellGcmConfig> gcmConfigPtr = ppu.ReadArg<GuestPtr<CellGcmConfig>>(Arg0);

            //Ensure it was allocated
            if (_rsx.ConfigPtr.IsNullPtr)
            {
                throw new Exception($"[CellGcmSys] cellGcmGetConfiguration: config block was not allocated!");
            }


            // Copy the struct
            CellGcmConfig rsxConfig = _memoryManager.Read<CellGcmConfig>(_rsx.ConfigPtr);
            _memoryManager.Write(gcmConfigPtr, rsxConfig);

            return CellOk;
        }

        internal ulong CellGcmGetControlRegister(PPUThread ppu)
        {
            uint ctr_ptr = ppu.ReadArg<uint>(Arg0);
            uint ret = _rsx.CtrlPtr.Address;
            //  Console.WriteLine($"[CellGcmSys] cellGcmGetControlRegister ret = {ret:X8}");
            return ret; //Address goes to R3
        }

        internal ulong CellGcmAddressToOffset(PPUThread ppu)
        {
            uint address = ppu.ReadArg<uint>(Arg0);
            uint offset_ptr = ppu.ReadArg<uint>(Arg1);
            uint offset = _rsx.AddressToOffset(address);
            _memoryManager.Write<BigEndian<uint>>(offset_ptr, offset);

            //  Console.WriteLine($"[CellGcmSys] cellGcmAddressToOffset address = {address:X8} offset_ptr = {offset_ptr:X8} ret = {offset:X8}");
            return CellOk;
        }

        internal ulong CellGcmGetLabelAddress(PPUThread ppu)
        {
            uint index = ppu.ReadArg<uint>(Arg0);
            uint ret = _rsx.LabelsPtr + (index * 0x10);
            //Console.WriteLine($"[CellGcmSys] cellGcmGetLabelAddress index = {index:X8} ret = {ret:X8}");
            return ret;
        }

        internal ulong CellGcmSetFlipMode(PPUThread ppu)
        {
            //Console.WriteLine($"[CellGcmSys] cellGcmSetFlipMode");
            uint ret = ppu.ReadArg<uint>(Arg0);
            return ret; //No return ?
        }

        internal ulong CellGcmSetDisplayBuffer(PPUThread ppu)
        {
            //const u8 bufferId,const u32 offset,const u32 pitch,const u32 width,const u32 height
            byte bufferId = ppu.ReadArg<byte>(Arg0);
            uint offset = ppu.ReadArg<uint>(Arg1);
            uint pitch = ppu.ReadArg<uint>(Arg2);
            uint width = ppu.ReadArg<uint>(Arg3);
            uint height = ppu.ReadArg<uint>(Arg4);

            _rsx.EnqueueCommand(new WorkItem(() =>
            {
                _rsx.FrameBuffers[bufferId].Address = _rsx.VRAMPtr.Address + offset;
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
            return CellOk;
        }

        internal ulong CellGcmResetFlipStatus(PPUThread ppu)
        {
            //Console.WriteLine($"[CellGcmSys] CellGcmResetFlipStatus");
            //Interlocked.Exchange<uint>(ref FlipStatus, 1);
            Interlocked.Exchange<ulong>(ref FlipStatus, 1);

            return CellOk;
        }

        internal ulong CellGcmGetFlipStatus(PPUThread ppu)
        {
            //Console.WriteLine($"[CellGcmSys] CellGcmGetFlipStatus: {FlipStatus}");
            return Interlocked.Read(ref FlipStatus);
        }

        internal ulong CellGcmSetFlip(PPUThread ppu)
        {
            //TODO
            GuestPtr<CellGcmCtxData> ctx_ptr = ppu.ReadArg<GuestPtr<CellGcmCtxData>>(Arg0); // Ctx argument
            byte bufferId = ppu.ReadArg<byte>(Arg1);

            CellGcmCtxData ctx = _memoryManager.Read<CellGcmCtxData>(ctx_ptr);

            // Ensure enough space in the current command buffer
            if (ctx.Current + 8 >= ctx.End)
            {
                CellGcmCallback(ppu);
            }

            // Inject a flip command
            uint flipCommand = RSXCommand.GcmFlipCommand | (1 << RSXCommand.RsxMethodCountShift); // 1 Argument
            _memoryManager.Write<BigEndian<uint>>(ctx.Current, flipCommand);
            _memoryManager.Write<BigEndian<uint>>(ctx.Current + 4, bufferId);
            ctx.Current += 8;

            _memoryManager.Write<CellGcmCtxData>(ctx_ptr, ctx);

            //Console.WriteLine($"[CellGcmSys] CellGcmSetFlip bufferId = {bufferId}");
            return CellOk;
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
            return CellOk;
        }

        internal ulong CellGcmGetTiledPitchSize(PPUThread ppu)
        {
            Console.WriteLine($"[CellGcmSys] CellGcmGetTiledPitchSize");

            uint size = ppu.ReadArg<uint>(Arg0);
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

            return CellOk;
        }

        internal ulong CellGcmSetTileInfo(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetTileInfo");
            return CellOk;
        }

        internal ulong CellGcmBindTile(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmBindTile");
            return CellOk;
        }

        internal ulong CellGcmSetTile(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetTile");
            return CellOk;
        }

        internal ulong CellGcmBindZcull(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmBindZcull");
            return CellOk;
        }

        internal ulong CellGcmSetDebugOutputLevel(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] CellGcmSetDebugOutputLevel");
            return CellOk;
        }

        internal ulong CellGcmMapMainMemory(PPUThread ppu)
        {
            //TODO

            GuestVoidPtr address = ppu.ReadArg<GuestVoidPtr>(Arg0);
            uint size = ppu.ReadArg<uint>(Arg1);
            GuestPtr<BigEndian<uint>> offset_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg2);

            Console.WriteLine($"[CellGcmSys] cellGcmMapMainMemory -- !!");

            uint pageCount = size >> 20;

            uint freePages = 0;
            uint offs;

            for (offs = 0; offs < (MemoryConstants.RamSize >> 20); offs++)
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

            _memoryManager.Write<BigEndian<uint>>(offset_ptr, offs << 20);
            return CellOk;
        }

        internal ulong CellGcmMapEaIoAddress(PPUThread ppu)
        {
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] cellGcmMapEaIoAddress");

            uint ea = ppu.ReadArg<uint>(Arg0);
            uint io = ppu.ReadArg<uint>(Arg1);
            uint size = ppu.ReadArg<uint>(Arg2);

            uint pageCount = size >> 20;

            for (uint i = 0; i < pageCount; i++)
            {
                uint pageOffset = i << 20;
                MapEaIo(ea + pageOffset, io + pageOffset);
            }

            return CellOk;
        }

        internal ulong CellGcmMapEaIoAddressWithFlags(PPUThread ppu)
        {
            // We ignore flags for now
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] CellGcmMapEaIoAddressWithFlags . CellGcmMapEaIoAddress");
            return CellGcmMapEaIoAddress(ppu);
        }

        internal ulong CellGcmGetOffsetTable(PPUThread ppu)
        {
            GuestPtr<CellGcmOffsetTable> offset_table_ptr = ppu.ReadArg<GuestPtr<CellGcmOffsetTable>>(Arg0);
            Console.WriteLine($"[{ppu.ThreadName}] [CellGcmSys] cellGcmGetOffsetTable");

            // Copy our internal offset table
            _memoryManager.Write(offset_table_ptr, OffsetTable);
            return CellOk;
        }

        internal ulong CellGcmSetSecondVFrequency(PPUThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetSecondVFrequency");
            return CellOk;
        }

        internal ulong CellGcmSetVBlankFrequency(PPUThread ppu)
        {
            uint freq = ppu.ReadArg<uint>(Arg0);
            Console.WriteLine($"[CellGcmSys] cellGcmSetVBlankFrequency");
            return CellOk;
        }

        internal ulong CellGcmSetVBlankHandler(PPUThread ppu)
        { // very important
            //TODO
            Console.WriteLine($"[CellGcmSys] cellGcmSetVBlankHandler");
            VblankHandler = ppu.ReadArg<GuestVoidPtr>(Arg0);

            // throw new NotImplementedException();
            return CellOk;
        }

        internal ulong CellGcmSetFlipHandler(PPUThread ppu)
        { // very important
            Console.WriteLine($"[CellGcmSys] cellGcmSetFlipHandler");
            FlipHandler = ppu.ReadArg<GuestVoidPtr>(Arg0);
            return CellOk;
        }

        internal ulong CellGcmGetLastFlipTime(PPUThread ppu)
        {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetLastFlipTime");
            return _rsx.LastFlipTime;
        }

        internal ulong CellGcmGetLastSecondVTime(PPUThread ppu)
        {
            // Console.WriteLine($"[CellGcmSys] cellGcmGetLastSecondVTime");
            return CellOk;
        }

        internal ulong CellGcmGetDefaultCommandWordSize(PPUThread ppu)
        {
            Console.WriteLine($"[CellGcmSys] cellGcmGetDefaultCommandWordSize");
            return DefaultCommandWordSize;
        }

        internal ulong CellGcmGetDefaultSegmentWordSize(PPUThread ppu)
        {
            Console.WriteLine($"[CellGcmSys] cellGcmGetDefaultSegmentWordSize");
            return DefaultSegmentWordSize;
        }
    }
}
