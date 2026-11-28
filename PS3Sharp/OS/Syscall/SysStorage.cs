using PS3Sharp.Cell;
using PS3Sharp.Cell.Ppu;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using System.Collections.Concurrent;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysStorage
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
            AtaHdd = 0x101000000000007UL,
            BdvdDrive = 0x101000000000006UL,

            Pata0HddDrive = 0x101000000000008UL,
            Pata0BdvdDrive = BdvdDrive,
            Pata1HddDrive = AtaHdd,

            BuiltinFlash = 0x100000000000001UL,
            NandFlash = BuiltinFlash,
            NandUnk = 0x100000000000003UL,
            NorFlash = 0x100000000000004UL,

            MemoryStick = 0x103000000000010UL,
            SdCard = 0x103000100000010UL,
            CompactFlash = 0x103000200000010UL,

            UsbMassStorage1Base = 0x10300000000000AUL,
            UsbMassStorage2Base = 0x10300000000001FUL,
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

        internal ulong SysStorageGetDeviceInfo(PpuThread ppu)
        {
            return CellOk;
            ulong device = ppu.ReadArg<ulong>(Arg0);
            GuestPtr<StorageDeviceInfo> info_ptr = ppu.ReadArg<GuestPtr<StorageDeviceInfo>>(Arg1);

            Devices storage = (Devices)(device & 0xFFFFF00FFFFFFFF);
            ulong dev_num = (device >> 32) & 0xFF;

            //Console.WriteLine($"Storage = {storage:X} num = {dev_num}");
            switch (storage)
            {
                case Devices.AtaHdd:
                    throw new NotImplementedException("todo: ATA_HDD");
                    break;

                case Devices.UsbMassStorage1Base:
                case Devices.UsbMassStorage1Base + 1:
                case Devices.UsbMassStorage1Base + 2:
                case Devices.UsbMassStorage1Base + 3:
                case Devices.UsbMassStorage1Base + 4:
                case Devices.UsbMassStorage1Base + 5:
                    return GetUSBMassStorageInfo(info_ptr, dev_num);


                case Devices.UsbMassStorage2Base:

                    break;

                default:
                    Console.WriteLine($"SysStorageGetDeviceInfo unknown storage: {storage:X}");
                    break;
            }


            return CellOk;
        }


        internal ulong GetUSBMassStorageInfo(GuestPtr<StorageDeviceInfo> info_ptr, ulong dev_num)
        {
            if (dev_num > 0)
            {
                return 0xFFFFFFFFFFFFFFFB; // -5
            }

            StorageDeviceInfo info = new();

            Span<byte> name_string = Encoding.UTF8.GetBytes("unnamed");
            name_string.CopyTo(info.Name);

            info.SectorSize = 0x200;
            info.One = 1;

            info.Flags[1] = 0;
            info.Flags[2] = 1;
            info.Flags[7] = 1;

            _memoryManager.Write(info_ptr, info);
            return CellOk;
        }

        internal ulong SysStorageOpen(PpuThread ppu)
        {
            return CellOk;
            ulong dev = ppu.ReadArg<ulong>(Arg0);
            ulong mode = ppu.ReadArg<ulong>(Arg1);
            GuestPtr<BigEndian<uint>> fd_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg2);
            ulong flags = ppu.ReadArg<ulong>(Arg3);

            Devices storage = (Devices)(dev & 0xFFFFF00FFFFFFFF);

            int size = 0;

            switch (storage)
            {
                case Devices.UsbMassStorage1Base:
                case Devices.UsbMassStorage1Base + 1:
                case Devices.UsbMassStorage1Base + 2:
                case Devices.UsbMassStorage1Base + 3:
                case Devices.UsbMassStorage1Base + 4:
                case Devices.UsbMassStorage1Base + 5:
                    Console.WriteLine($"SysStorageOpen USB_MASS_STORAGE_1");
                    size = 256 * 1024 * 1024;
                    break;

                case Devices.UsbMassStorage2Base:

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

                _memoryManager.Write<BigEndian<uint>>(fd_ptr, fd);
            }

            return CellOk;
        }

        internal ulong SysStorageRead(PpuThread ppu)
        {
            return CellOk;

            uint fd = ppu.ReadArg<uint>(Arg0);
            uint mode = ppu.ReadArg<uint>(Arg1);
            uint start_sector = ppu.ReadArg<uint>(Arg2);
            uint num_sectors = ppu.ReadArg<uint>(Arg3);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(Arg4);
            GuestPtr<BigEndian<uint>> sectors_read = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg5);
            ulong flags = ppu.ReadArg<ulong>(Arg6);


            if (_openDevices.TryGetValue(fd, out StorageDevice? storageDevice))
            {
                int offset = (int)(start_sector * 0x200);
                int size = (int)(num_sectors * 0x200);
                Span<byte> data = storageDevice.CreateSpan(offset, size);
                _memoryManager.WriteArray(buf, data);

                //Console.WriteLine($"SysStorageRead: start_sector = {start_sector} num_sectors = {num_sectors}");
            }
            else
            {
                return CellESrch;
                //throw new Exception($"SysStorageRead: Device was not open");
            }

            _memoryManager.Write<BigEndian<uint>>(sectors_read, num_sectors);
            return CellOk;
        }

        internal ulong SysStorageClose(PpuThread ppu)
        {
            return CellOk;
            uint fd = ppu.ReadArg<uint>(Arg0);
            _openDevices.TryRemove(fd, out _);
            return CellOk;
        }
    }
}
