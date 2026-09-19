using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Unicode;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall {
    public static class SysStorage {
        public enum Devices : ulong {
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


        private static readonly ConcurrentDictionary<uint, StorageDevice> _openDevices = new();

        public class StorageDevice {
            public readonly Devices Type;
            public readonly byte[] Data;

            public StorageDevice(Devices type, int size) {
                Type = type;
                Data = new byte[size];
            }

            public Span<byte> CreateSpan(int start, int size) { 
                return new Span<byte>(Data, start, size);
            }
        }

        public struct StorageDeviceInfo : IPS3Struct {
            public InlineData.InlineArray32<byte> Name;        
            public BigEndian<uint> Zero;       
            public BigEndian<uint> Zero2;       
            public BigEndian<ulong> SectorCount;
            public BigEndian<uint> SectorSize; 
            public BigEndian<uint> One;          
            public InlineData.InlineArray8<byte> Flags;          
        };

        public static ulong SysStorageGetDeviceInfo(PPU ppu) {
            return CELL_OK;
            ulong device = ppu.ReadArg<ulong>(ARG0);
            GuestPtr<StorageDeviceInfo> info_ptr = ppu.ReadArg<GuestPtr<StorageDeviceInfo>>(ARG1);

            Devices storage = (Devices)(device & 0xFFFFF00FFFFFFFF);
            ulong dev_num = (device >> 32) & 0xFF;

            //Console.WriteLine($"Storage = {storage:X} num = {dev_num}");
            switch (storage) {
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


        public static ulong GetUSBMassStorageInfo(GuestPtr<StorageDeviceInfo> info_ptr, ulong dev_num) {
            if(dev_num > 0) { 
                return 0xFFFFFFFFFFFFFFFB; // -5
            }

            unsafe {
                StorageDeviceInfo* info = info_ptr.AsNativePtr();

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

        public static ulong SysStorageOpen(PPU ppu) {
            return CELL_OK;
            ulong dev = ppu.ReadArg<ulong>(ARG0);
            ulong mode = ppu.ReadArg<ulong>(ARG1);
            GuestPtr<uint> fd_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG2);
            ulong flags = ppu.ReadArg<ulong>(ARG3);

            Devices storage = (Devices)(dev & 0xFFFFF00FFFFFFFF);

            int size = 0;

            switch (storage) {
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

            if (size > 0) {
                uint fd = MemoryManager.GetHandle();

                _openDevices.TryAdd(
                      fd,
                      new StorageDevice(storage, size));

                MemoryManager.Write<uint>(fd_ptr, fd);
            }

            return CELL_OK;
        }

        public static ulong SysStorageRead(PPU ppu) {
            return CELL_OK;

            uint fd = ppu.ReadArg<uint>(ARG0);
            uint mode = ppu.ReadArg<uint>(ARG1);
            uint start_sector = ppu.ReadArg<uint>(ARG2);
            uint num_sectors = ppu.ReadArg<uint>(ARG3);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG4);
            GuestPtr<uint> sectors_read = ppu.ReadArg<GuestPtr<uint>>(ARG5);
            ulong flags = ppu.ReadArg<ulong>(ARG6);


            if (_openDevices.TryGetValue(fd, out StorageDevice? storageDevice)) {
                int offset = (int)(start_sector * 0x200);
                int size = (int)(num_sectors * 0x200);

                unsafe {
                    Span<byte> data = storageDevice.CreateSpan(offset, size);
                    Span<byte> dest = MemoryManager.CreateSpan<byte>(buf, size);
                    dest.Clear();
                    data.CopyTo(dest);
                }

  
                //Console.WriteLine($"SysStorageRead: start_sector = {start_sector} num_sectors = {num_sectors}");
            } else {
                return CELL_ESRCH;
                //throw new Exception($"SysStorageRead: Device was not open");
            }

            MemoryManager.Write<uint>(sectors_read, num_sectors);
            return CELL_OK;
        }

        public static ulong SysStorageClose(PPU ppu) {
            return CELL_OK;
            uint fd = ppu.ReadArg<uint>(ARG0);
            _openDevices.TryRemove(fd, out _);
            return CELL_OK;
        }
    }
}
