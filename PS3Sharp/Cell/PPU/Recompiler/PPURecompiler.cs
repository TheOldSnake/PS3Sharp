using PS3Sharp.Cell.PPU.Recompiler.IR;
using PS3Sharp.Cell.PPU.Recompiler.x64;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using static PS3Sharp.Cell.PPU.PPUThread;
using static PS3Sharp.Cell.PPU.Recompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.Recompiler
{
    internal unsafe sealed class PPURecompiler : IDisposable
    {
        internal struct X64Block
        {
            internal delegate* unmanaged<PPUState*, void*, void> Code;
            internal int ByteCount;
            internal int IsSyscallExit;
        }

        private const int PAGE_BITS = 16;
        private const int PAGE_SIZE = 1 << PAGE_BITS;
        private const uint PAGE_MASK = (1 << PAGE_BITS) - 1;

        private static readonly bool IsRecompilerEnabled = false; // TODO make this configurable
        internal static bool IsRecompilerUsable => IsPlatformSupported() && IsRecompilerEnabled;

        private readonly List<VirtualMemoryHandle?> _executablePages = [];
        private readonly NativeMemoryHandle?[] _memoryBlocksTable = new NativeMemoryHandle[PAGE_SIZE];

        private readonly IRGenerator _irGenerator = new();
        private readonly X64Generator _x64Generator = new();

        private nint _nextBlockAddress;
        private nint _currentPageStartAddress;
        private nint _currentPageEndAddress;

        internal ulong NumberOfCompiledBlocks { get; private set; }
        internal ulong NumberOfExecutedBlocks { get; private set; }
        internal ulong TotalAllocatedPages { get; private set; }

        private X64Block* GetOrCreateBlock(uint virtualAddress)
        {
            // The smallest block is 4 bytes (one instruction)
            uint index = virtualAddress >> 2;

            // 2 Levels look up
            uint pageIndex = index >> PAGE_BITS;
            uint blockIndex = index & PAGE_MASK;

            // Look up the page
            NativeMemoryHandle? page = _memoryBlocksTable[pageIndex];

            // If not already allocated, allocate the page
            if (page == null)
            {
                nuint size = (nuint)(PAGE_SIZE * sizeof(X64Block));
                page = new(size);
                _memoryBlocksTable[pageIndex] = page;
            }

            // Look up into the page
            X64Block* pageBase = (X64Block*)page.DangerousGetHandle();
            X64Block* block = &pageBase[blockIndex];

            return block;
        }

        internal void RunPPU(PPUThread ppu)
        {
            // As of now this code is NOT thread safe
            X64Block* block = GetOrCreateBlock(ppu.State->PC);

            if (block->Code == null)
            {
                CompilePPCBlock(ppu, block);
            }

            // Console.WriteLine($"Executing block 0x{ppu.State->PC:X8} at: 0x{(nint)block->Code:X16}");

            PPUState* state = ppu.State;
            void* memory = ppu.MemoryManager.GetGuestMemoryBase();
            block->Code(state, memory);

            if (block->IsSyscallExit != 0)
            {
                ppu.SyscallDispatcher.DispatchSyscall(ppu, 0);
            }

            //throw new Exception($"Done, exit pc = 0x{ppu.State->PC:X8}");
        }

        private void CompilePPCBlock(PPUThread ppu, X64Block* nativeBlock)
        {
            // Generate IR
            IRBlock block = _irGenerator.GenerateIRBlock(ppu.State->PC, ppu.MemoryManager);
            nativeBlock->IsSyscallExit = block.IsSyscallExit;

            // Generate x64
            Span<byte> nativeCode = _x64Generator.GenerateNativeCode(block);
            int byteCount = nativeCode.Length;

            // Allocate more memory if we don't have enough
            if (!HasEnoughMemory(byteCount))
            {
                AllocateExecutableMemory(byteCount);
            }

            // Copy the code
            void* dest = (void*)_nextBlockAddress;
            Span<byte> destSpan = new(dest, byteCount);
            nativeCode.CopyTo(destSpan);

            // Set the address and size
            nativeBlock->Code = (delegate* unmanaged<PPUState*, void*, void>)dest;
            nativeBlock->ByteCount = byteCount;

            // Advance next block address and align to 16-byte
            _nextBlockAddress += byteCount;
            _nextBlockAddress = (nint)Utility.AlignPowerOfTwo((nuint)_nextBlockAddress, 16);

            // Debug
            // X64Generator.PrintNativeCode(nativeCode, (nint)dest);
        }

        private void AllocateExecutableMemory(int size)
        {
            ulong alignedSize = Utility.AlignPowerOfTwo((ulong)size, PAGE_SIZE);

            // Create VirtualMemoryHandle
            // Uses MEM_RESERVE by default
            VirtualMemoryHandle memoryHandle = new((nuint)alignedSize, NativeMethods.WIN_API_PAGE_EXECUTE_READWRITE);
            nint memory = memoryHandle.DangerousGetHandle();

            // Commit
            NativeMethods.VirtualAlloc(memory, (nuint)alignedSize, NativeMethods.WIN_API_MEM_COMMIT, NativeMethods.WIN_API_PAGE_EXECUTE_READWRITE);

            // Clear
            NativeMemory.Clear((void*)memory, (nuint)alignedSize);

            _currentPageStartAddress = memory;
            _currentPageEndAddress = memory + (nint)alignedSize;
            _nextBlockAddress = _currentPageStartAddress;
            _executablePages.Add(memoryHandle);
            TotalAllocatedPages++;
        }

        private bool HasEnoughMemory(int size)
        {
            ulong aligned = Utility.AlignPowerOfTwo((nuint)_nextBlockAddress + (nuint)size, 16);
            return aligned < (nuint)_currentPageEndAddress;
        }

        private static bool IsPlatformSupported()
        {
            bool isWindows64bit = OperatingSystem.IsWindows() && RuntimeInformation.OSArchitecture == Architecture.X64;
            bool hasSSE = Sse.IsSupported;
            bool hasSSE2 = Sse2.IsSupported;
            bool hasAVX = Avx.IsSupported;
            bool hasBMI2 = Bmi2.IsSupported;
            bool isSupported = isWindows64bit && hasSSE && hasSSE2 && hasAVX && hasBMI2;
            return isSupported;
        }

        public void Dispose()
        {
            Console.WriteLine($"[PPURecompiler] Disposing Recompiler");

            // Free executable pages
            for (int i = 0; i < _executablePages.Count; i++)
            {
                VirtualMemoryHandle? virtualAlloc = _executablePages[i];
                virtualAlloc?.Dispose();
            }

            _executablePages.Clear();

            // Free block structs
            for (int i = 0; i < _memoryBlocksTable.Length; i++)
            {
                NativeMemoryHandle? block = _memoryBlocksTable[i];
                block?.Dispose();
                _memoryBlocksTable[i] = null;
            }

            _currentPageStartAddress = 0;
            _currentPageEndAddress = 0;
            _nextBlockAddress = 0;
        }
    }
}
