using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Semaphore;

namespace PS3Sharp.OS.Syscall
{
    internal class SysSemaphore
    {
        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, LV2Semaphore> _semaphores = [];

        internal SysSemaphore(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysSemaphoreCreate(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> semaphore_id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<SysSemaphoreAttribute> semaphore_attribute_ptr = ppu.ReadArg<GuestPtr<SysSemaphoreAttribute>>(Arg1);
            uint initialCount = ppu.ReadArg<uint>(Arg2);
            uint maxCount = ppu.ReadArg<uint>(Arg3);

            SysSemaphoreAttribute attribute = _memoryManager.Read<SysSemaphoreAttribute>(semaphore_attribute_ptr);
            LV2Semaphore semaphore = new((int)initialCount, (int)maxCount, attribute, _memoryManager);
            AddSemaphore(semaphore.Handle, semaphore);

            _memoryManager.Write<BigEndian<uint>>(semaphore_id_ptr, semaphore.Handle);
            return CellOk;
        }

        internal ulong SysSemaphoreDestroy(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);

            RemoveSemaphore(handle);

            return CellOk;
        }

        internal ulong SysSemaphoreWait(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);
            ulong timeout_us = ppu.ReadArg<ulong>(Arg1);

            // Console.WriteLine($"[{ppu.ThreadName}] [SysSemaphore] SysSemaphoreWait id = 0x{handle:X8} timeout = {timeout_us}us");

            ulong result;
            if (TryGetSemaphore(handle, out LV2Semaphore? semaphore))
            {
                result = semaphore.Wait(ppu, timeout_us);
            }
            else
            {
                result = CellESrch;
            }

            // Console.WriteLine($"[{ppu.ThreadName}] [SysSemaphore] SysSemaphoreWait result = {CellErrorNames[result]}");
            return result;
        }

        internal ulong SysSemaphoreTryWait(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);

            if (TryGetSemaphore(handle, out LV2Semaphore? semaphore))
            {
                return semaphore.TryWait();
            }
            else
            {
                return CellESrch;
            }
        }

        internal ulong SysSemaphorePost(PPUThread ppu)
        {
            uint handle = ppu.ReadArg<uint>(Arg0);
            uint count = ppu.ReadArg<uint>(Arg1);

            ulong result;

            if (TryGetSemaphore(handle, out LV2Semaphore? semaphore))
            {
                result = semaphore.Release((int)count);
            }
            else
            {
                result = CellESrch;
            }

            //  Console.WriteLine($"[{ppu.ThreadName}] [SysSemaphore] SysSemaphorePost id = 0x{handle:X8} count = {count} result = {CellErrorNames[result]}");
            return result;
        }

        internal bool TryGetSemaphore(uint id, [NotNullWhen(true)] out LV2Semaphore? semaphore) => _semaphores.TryGetValue(id, out semaphore);

        private void AddSemaphore(uint id, LV2Semaphore semaphore)
        {
            bool success = _semaphores.TryAdd(id, semaphore);

            if (!success)
            {
                throw new Exception($"[SysSemaphore] Could not add semaphore to dictionary");
            }
        }

        private void RemoveSemaphore(uint id)
        {
            bool success = _semaphores.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[SysSemaphore] Could not remove semaphore from dictionary");
            }
        }
    }
}
