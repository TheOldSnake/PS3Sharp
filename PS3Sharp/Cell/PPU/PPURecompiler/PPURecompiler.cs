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

        public struct X64Block {
            public delegate* unmanaged[Stdcall]<void> Code;
            public int IsSyscallExit;
        }

        private const int Bits = 16;
        private const int Size = 1 << Bits;
        private const uint Mask = Size - 1;
        private static X64Block*[] _blocks = new X64Block*[Size];

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

            // Allocate executable memory
            void* destMemory = AllocateExecutableMemory(nativeCode.Length);

            // Copy the code
            Span<byte> destSpan = new Span<byte>(destMemory, nativeCode.Length);
            nativeCode.CopyTo(destSpan);

            // Set the address
            nativeBlock->Code = (delegate* unmanaged[Stdcall]<void>)destMemory;
        }

        public static void* AllocateExecutableMemory(int size) {
            void* memory = VirtualAlloc(null, size, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
            return memory;
        }

        public static void FreeExecutableMemory(void* memory, int size) {
            VirtualFree(memory, size, MEM_RELEASE);
        }
    }
}
