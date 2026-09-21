using PS3Sharp.Cell.PPU.PPURecompiler.IR;
using PS3Sharp.Cell.PPU.PPURecompiler.x64;
using PS3Sharp.Syscall;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.PPU.PPURecompiler.IR.IRGenerator;

namespace PS3Sharp.Cell.PPU.PPURecompiler {
    public unsafe class PPURecompiler : IDisposable {
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

        private readonly List<(nuint Address, ulong Size)> AllocatedPages = [];

        public struct X64Block {
            public delegate* unmanaged[Stdcall]<void> Code;
            public int IsSyscallExit;
        }

        private const int Bits = 16;
        private const int Size = 1 << Bits;
        private const uint Mask = Size - 1;
        private X64Block*[] _blocks = new X64Block*[Size];

        private nuint NextBlockAddress;
        private nuint CurrentPageStartAddress;
        private nuint CurrentPageEndAddress;
        private bool disposedValue;

        private X64Block* GetBlock(uint virtualAddress) {
            uint index = virtualAddress >> 2;

            uint l1 = index >> Bits;
            uint l2 = index & Mask;

            var page = _blocks[l1];

            return page == null ? null : & page[l2];
        }

        private X64Block* GetOrCreateBlock(uint virtualAddress) {
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


        public void RunPPU(PPU ppu) {

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

        private void CompilePPCBlock(PPU ppu, X64Block* nativeBlock) {
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

        private void AllocateExecutableMemory(int size) {
            ulong alignedSize = AlignPow2((ulong)size, VirtualAllocSize);
            nuint memory = (nuint)VirtualAlloc(null, (int)alignedSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);

            if (memory == 0) {
                throw new Win32Exception();
            }

            CurrentPageStartAddress = memory;
            CurrentPageEndAddress = (nuint)(memory + alignedSize);
            NextBlockAddress = CurrentPageStartAddress;
            AllocatedPages.Add((CurrentPageStartAddress, alignedSize));
        }

        private bool HasEnoughMemory(int size) {
            ulong aligned = AlignPow2(NextBlockAddress + (nuint)size, 16);
            return aligned < CurrentPageEndAddress;
        }

        private ulong AlignPow2(ulong value, ulong alignment) {
            bool isPowerOf2 = alignment != 0 && (alignment & (alignment - 1)) == 0;
            
            if (!isPowerOf2) {
                throw new Exception($"Alignment is not power of 2");
            }
            
            return (value + alignment - 1) & ~(alignment - 1);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null

                // Free executable pages
                foreach (var allocation in AllocatedPages) {
                    bool ok = VirtualFree((void*)allocation.Address, (int)allocation.Size, MEM_RELEASE);

                    if (!ok) {
                        throw new Win32Exception();
                    }
                }

                // Free block structs
                foreach (var block in _blocks) {
                    if (block != null) {
                        NativeMemory.Free(block);
                    }
                }

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PPURecompiler()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
