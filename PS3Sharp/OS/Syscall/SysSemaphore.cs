using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using System.Collections.Concurrent;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;
using static PS3Sharp.OS.LV2.LV2Semaphore;

namespace PS3Sharp.OS.Syscall
{
    internal static class SysSemaphore
    {
        private static ConcurrentDictionary<uint, LV2Semaphore> _semaphores = [];

        internal static bool GetSemaphore(uint id, out LV2Semaphore? semaphore) => _semaphores.TryGetValue(id, out semaphore);

        internal static void AddSemaphore(uint id, LV2Semaphore semaphore)
        {
            bool success = _semaphores.TryAdd(id, semaphore);

            if (!success)
            {
                throw new Exception($"[SysSemaphore] Could not add semaphore to dictionary");
            }
        }

        internal static void RemoveSemaphore(uint id)
        {
            bool success = _semaphores.TryRemove(id, out _);

            if (!success)
            {
                throw new Exception($"[SysSemaphore] Could not remove semaphore from dictionary");
            }
        }

        internal static ulong SysSemaphoreCreate(PPU ppu)
        {
            GuestPtr<uint> semaphore_id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<SysSemaphoreAttribute> semaphore_attribute_ptr = ppu.ReadArg<GuestPtr<SysSemaphoreAttribute>>(ARG1);
            uint initialCount = ppu.ReadArg<uint>(ARG2);
            uint maxCount = ppu.ReadArg<uint>(ARG3);

            SysSemaphoreAttribute attribute = semaphore_attribute_ptr.Read();
            LV2Semaphore semaphore = new((int)initialCount, (int)maxCount, attribute);
            AddSemaphore(semaphore.Handle, semaphore);

            MemoryManager.Write<uint>(semaphore_id_ptr, semaphore.Handle);
            return CELL_OK;
        }

        internal static ulong SysSemaphoreDestroy(PPU ppu)
        {
            uint handle = ppu.ReadArg<uint>(ARG0);

            RemoveSemaphore(handle);

            return CELL_OK;
        }

        internal static ulong SysSemaphoreWait(PPU ppu)
        {
            uint handle = ppu.ReadArg<uint>(ARG0);
            ulong timeout_us = ppu.ReadArg<ulong>(ARG1);

            // Console.WriteLine($"[{ppu.ThreadName}] [SysSemaphore] SysSemaphoreWait id = 0x{handle:X8} timeout = {timeout_us}us");

            ulong result;
            if (GetSemaphore(handle, out LV2Semaphore? semaphore))
            {
                result = semaphore.Wait(timeout_us);

            }
            else
            {
                result = CELL_ESRCH;
            }

            //  Console.WriteLine($"[{ppu.ThreadName}] [SysSemaphore] SysSemaphoreWait result = {CellErrorNames[result]}");

            return result;
        }

        internal static ulong SysSemaphoreTryWait(PPU ppu)
        {
            uint handle = ppu.ReadArg<uint>(ARG0);

            if (GetSemaphore(handle, out LV2Semaphore? semaphore))
            {
                return semaphore.TryWait();

            }
            else
            {
                return CELL_ESRCH;
            }
        }

        internal static ulong SysSemaphorePost(PPU ppu)
        {
            uint handle = ppu.ReadArg<uint>(ARG0);
            uint count = ppu.ReadArg<uint>(ARG1);

            ulong result;
            if (GetSemaphore(handle, out LV2Semaphore? semaphore))
            {
                result = semaphore.Release((int)count);

            }
            else
            {
                result = CELL_ESRCH;
            }

            //  Console.WriteLine($"[{ppu.ThreadName}] [SysSemaphore] SysSemaphorePost id = 0x{handle:X8} count = {count} result = {CellErrorNames[result]}");
            return result;
        }
    }
}
