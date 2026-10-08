using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using System.Collections.Concurrent;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal class SysStorage
    {
        internal sealed class StorageDevice
        {
            internal readonly Devices Type;
            internal readonly byte[] Data;

            internal StorageDevice(Devices type, int size)
            {
                Type = type;
                Data = new byte[size];
            }

            internal Span<byte> CreateSpan(int start, int size)
            {
                return new Span<byte>(Data, start, size);
            }
        }

        internal enum Devices : ulong
        {
            ATA_HDD = 0x101000000000007UL,
            BDVD_DRIVE = 0x101000000000006UL,

            PATA0_HDD_DRIVE = 0x101000000000008UL,
            PATA0_BDVD_DRIVE = BDVD_DRIVE,
            PATA1_HDD_DRIVE = ATA_HDD,

            BUILTIN_FLASH = 0x100000000000001UL,
            NAND_FLASH = BUILTIN_FLASH,
            NAND_UNK = 0x100000000000003UL,
            NOR_FLASH = 0x100000000000004UL,

            MEMORY_STICK = 0x103000000000010UL,
            SD_CARD = 0x103000100000010UL,
            COMPACT_FLASH = 0x103000200000010UL,

            USB_MASS_STORAGE_1_BASE = 0x10300000000000AUL,
            USB_MASS_STORAGE_2_BASE = 0x10300000000001FUL,
        }

        internal struct StorageDeviceInfo : IPS3Struct
        {
            internal InlineData.InlineArray32<byte> Name;
            internal BigEndian<uint> Zero;
            internal BigEndian<uint> Zero2;
            internal BigEndian<ulong> SectorCount;
            internal BigEndian<uint> SectorSize;
            internal BigEndian<uint> One;
            internal InlineData.InlineArray8<byte> Flags;
        };

        private readonly MemoryManager _memoryManager;
        private readonly ConcurrentDictionary<uint, StorageDevice> _openDevices = new();

        internal SysStorage(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong SysStorageGetDeviceInfo(PPUThread ppu)
        {
            return CELL_OK;
            ulong device = ppu.ReadArg<ulong>(ARG0);
            GuestPtr<StorageDeviceInfo> info_ptr = ppu.ReadArg<GuestPtr<StorageDeviceInfo>>(ARG1);

            Devices storage = (Devices)(device & 0xFFFFF00FFFFFFFF);
            ulong dev_num = (device >> 32) & 0xFF;

            //Console.WriteLine($"Storage = {storage:X} num = {dev_num}");
            switch (storage)
            {
                case Devices.ATA_HDD:
                    throw new NotImplementedException("todo: ATA_HDD");
                    break;

                case Devices.USB_MASS_STORAGE_1_BASE:
                case Devices.USB_MASS_STORAGE_1_BASE + 1:
                case Devices.USB_MASS_STORAGE_1_BASE + 2:
                case Devices.USB_MASS_STORAGE_1_BASE + 3:
                case Devices.USB_MASS_STORAGE_1_BASE + 4:
                case Devices.USB_MASS_STORAGE_1_BASE + 5:
                    return GetUSBMassStorageInfo(info_ptr, dev_num);


                case Devices.USB_MASS_STORAGE_2_BASE:

                    break;

                default:
                    Console.WriteLine($"SysStorageGetDeviceInfo unknown storage: {storage:X}");
                    break;
            }


            return CELL_OK;
        }


        internal ulong GetUSBMassStorageInfo(GuestPtr<StorageDeviceInfo> info_ptr, ulong dev_num)
        {
            if (dev_num > 0)
            {
                return 0xFFFFFFFFFFFFFFFB; // -5
            }

            unsafe
            {
                StorageDeviceInfo* info = _memoryManager.GetPtr(info_ptr);

                Span<byte> name_string = Encoding.UTF8.GetBytes("unnamed");
                name_string.CopyTo(info->Name);

                info->SectorSize = 0x200;
                info->One = 1;

                info->Flags[1] = 0;
                info->Flags[2] = 1;
                info->Flags[7] = 1;
            }

            return CELL_OK;
        }

        internal ulong SysStorageOpen(PPUThread ppu)
        {
            return CELL_OK;
            ulong dev = ppu.ReadArg<ulong>(ARG0);
            ulong mode = ppu.ReadArg<ulong>(ARG1);
            GuestPtr<uint> fd_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG2);
            ulong flags = ppu.ReadArg<ulong>(ARG3);

            Devices storage = (Devices)(dev & 0xFFFFF00FFFFFFFF);

            int size = 0;

            switch (storage)
            {
                case Devices.USB_MASS_STORAGE_1_BASE:
                case Devices.USB_MASS_STORAGE_1_BASE + 1:
                case Devices.USB_MASS_STORAGE_1_BASE + 2:
                case Devices.USB_MASS_STORAGE_1_BASE + 3:
                case Devices.USB_MASS_STORAGE_1_BASE + 4:
                case Devices.USB_MASS_STORAGE_1_BASE + 5:
                    Console.WriteLine($"SysStorageOpen USB_MASS_STORAGE_1");
                    size = 256 * 1024 * 1024;
                    break;

                case Devices.USB_MASS_STORAGE_2_BASE:

                    break;

                default:
                    Console.WriteLine($"SysStorageOpen unknown storage: {storage:X}");
                    break;
            }

            if (size > 0)
            {
                uint fd = _memoryManager.GetHandle();

                _openDevices.TryAdd(
                      fd,
                      new StorageDevice(storage, size));

                _memoryManager.Write<uint>(fd_ptr, fd);
            }

            return CELL_OK;
        }

        internal ulong SysStorageRead(PPUThread ppu)
        {
            return CELL_OK;

            uint fd = ppu.ReadArg<uint>(ARG0);
            uint mode = ppu.ReadArg<uint>(ARG1);
            uint start_sector = ppu.ReadArg<uint>(ARG2);
            uint num_sectors = ppu.ReadArg<uint>(ARG3);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG4);
            GuestPtr<uint> sectors_read = ppu.ReadArg<GuestPtr<uint>>(ARG5);
            ulong flags = ppu.ReadArg<ulong>(ARG6);


            if (_openDevices.TryGetValue(fd, out StorageDevice? storageDevice))
            {
                int offset = (int)(start_sector * 0x200);
                int size = (int)(num_sectors * 0x200);

                unsafe
                {
                    Span<byte> data = storageDevice.CreateSpan(offset, size);
                    Span<byte> dest = _memoryManager.CreateSpan<byte>(buf, size);
                    dest.Clear();
                    data.CopyTo(dest);
                }


                //Console.WriteLine($"SysStorageRead: start_sector = {start_sector} num_sectors = {num_sectors}");
            }
            else
            {
                return CELL_ESRCH;
                //throw new Exception($"SysStorageRead: Device was not open");
            }

            _memoryManager.Write<uint>(sectors_read, num_sectors);
            return CELL_OK;
        }

        internal ulong SysStorageClose(PPUThread ppu)
        {
            return CELL_OK;
            uint fd = ppu.ReadArg<uint>(ARG0);
            _openDevices.TryRemove(fd, out _);
            return CELL_OK;
        }
    }
}
