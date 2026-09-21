using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Cell.PPU.PPURecompiler.IR;
using PS3Sharp.Cell.PPU.PPURecompiler.x64;
using PS3Sharp.Syscall;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;
using static PS3Sharp.Cell.PPU.PPURecompiler.PPURecompiler;

namespace PS3Sharp.Cell.PPU.PPURecompiler {
    public static unsafe class PPURecompiler {
        //Import needed kernel functions
        [DllImport("kernel32.dll")]
        private static extern void* VirtualAlloc(void* addr, int size, int type, int protect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(void* addr, int size, int new_protect, int* old_protect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFree(void* addr, int size, int type);

        [DllImport("kernel32.dll")]
        private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);
        private const int PAGE_EXECUTE_READWRITE = 0x40;
        private const int MEM_COMMIT = 0x00001000;
        private const int MEM_RELEASE = 0x00008000;
        private const int VirtualAllocSize = 4 * 1024;    // 4 kb

        public struct X64Block {
            public delegate* unmanaged[Stdcall]<void> Code;
            public int IsSyscallExit;
        }

        private const int Bits = 16;
        private const int Size = 1 << Bits;
        private const uint Mask = Size - 1;
        private static X64Block*[] _blocks = new X64Block*[Size];

        private static nuint NextBlockAddress;
        private static nuint CurrentPageStartAddress;
        private static nuint CurrentPageEndAddress;

        private static X64Block* GetBlock(uint virtualAddress) {
            uint index = virtualAddress >> 2;

            uint l1 = index >> Bits;
            uint l2 = index & Mask;

            var page = _blocks[l1];

            return page == null ? null : & page[l2];
        }

        private static X64Block* GetOrCreateBlock(uint virtualAddress) {
            uint index = virtualAddress >> 2;

            uint l1 = index >> Bits;
            uint l2 = index & Mask;

            var page = _blocks[l1];

            if (page == null) {
                page = (X64Block*)NativeMemory.Alloc((nuint)(Size * sizeof(X64Block)));
                NativeMemory.Clear(page, (nuint)(Size * sizeof(X64Block)));
                _blocks[l1] = page;
            }

            return &page[l2];
        }


        public static void RunPPU(PPU ppu) {

            X64Block* block = GetOrCreateBlock(ppu.State->PC);

            if (block->Code == null) {
                CompilePPCBlock(ppu, block);
            }

             Console.WriteLine($"Executing block 0x{ppu.State->PC:X8}");

            block->Code();

            if (block->IsSyscallExit == 1) {
                SyscallHandler.HandleSyscall(ppu, 0);
            }

            //throw new Exception($"Done, exit pc = 0x{ppu.State->PC:X8}");
        }

        private static void CompilePPCBlock(PPU ppu, X64Block* nativeBlock) {
            // Generate IR
            IRBlock block = IRGenerator.GenerateIRBlock(ppu.State->PC, ppu);
            nativeBlock->IsSyscallExit = block.IsSyscallExit;

            // Generate x64
            Span<byte> nativeCode = x64Generator.GenerateNativeCode(block, ppu);

            if (!HasEnoughMemory(nativeCode.Length)) {
                // Allocate executable memory
                AllocateExecutableMemory(nativeCode.Length);
            }

            void* dest = (void*)NextBlockAddress;

            // Copy the code
            int length = nativeCode.Length;
            Span<byte> destSpan = new Span<byte>(dest, length);
            nativeCode.CopyTo(destSpan);

            // Set the address
            nativeBlock->Code = (delegate* unmanaged[Stdcall]<void>)dest;

            // Advance next block address and align to 16-byte
            NextBlockAddress += (nuint)length;
            NextBlockAddress = (nuint)AlignPow2(NextBlockAddress, 16);
        }

        public static void AllocateExecutableMemory(int size) {
            ulong alignedSize = AlignPow2((ulong)size, VirtualAllocSize);
            nuint memory = (nuint)VirtualAlloc(null, (int)alignedSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);

            CurrentPageStartAddress = memory;
            CurrentPageEndAddress = (nuint)(memory + alignedSize);
            NextBlockAddress = CurrentPageStartAddress;
        }

        public static void FreeExecutableMemory(void* memory, int size) {
            VirtualFree(memory, size, MEM_RELEASE);
        }

        private static bool HasEnoughMemory(int size) {
            ulong aligned = AlignPow2(NextBlockAddress + (nuint)size, 16);
            return aligned < CurrentPageEndAddress;
        }

        private static ulong AlignPow2(ulong value, ulong alignment) {
            bool isPowerOf2 = alignment != 0 && (alignment & (alignment - 1)) == 0;
            
            if (!isPowerOf2) {
                throw new Exception($"Alignment is not power of 2");
            }
            
            return (value + alignment - 1) & ~(alignment - 1);
        }
    }
}
