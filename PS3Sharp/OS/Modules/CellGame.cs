using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.FileSystem;
using PS3Sharp.Memory;
using PS3Sharp.SFOLoader;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class CellGame
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CellGameContentSize : IPS3Struct
        {
            internal BigEndian<int> HDDFreeSizeKB;
            internal BigEndian<int> SizeKB;
            internal BigEndian<int> SysSizeKB;
        }

        internal const uint CellGameParamidTitle = 0;
        internal const uint CellGameParamidTitleDefault = 1;
        internal const uint CellGameParamidTitleJapanese = 2;
        internal const uint CellGameParamidTitleEnglish = 3;
        internal const uint CellGameParamidTitleFrench = 4;
        internal const uint CellGameParamidTitleSpanish = 5;
        internal const uint CellGameParamidTitleGerman = 6;
        internal const uint CellGameParamidTitleItalian = 7;
        internal const uint CellGameParamidTitleDutch = 8;
        internal const uint CellGameParamidTitlePortuguese = 9;
        internal const uint CellGameParamidTitleRussian = 10;
        internal const uint CellGameParamidTitleKorean = 11;
        internal const uint CellGameParamidTitleChineseT = 12;
        internal const uint CellGameParamidTitleChineseS = 13;
        internal const uint CellGameParamidTitleFinnish = 14;
        internal const uint CellGameParamidTitleSwedish = 15;
        internal const uint CellGameParamidTitleDanish = 16;
        internal const uint CellGameParamidTitleNorwegian = 17;
        internal const uint CellGameParamidTitlePolish = 18;
        internal const uint CellGameParamidTitlePortugueseBrazil = 19;
        internal const uint CellGameParamidTitleEnglishUk = 20;
        internal const uint CellGameParamidTitleTurkish = 21;

        internal const uint CellGameParamidTitleId = 100;
        internal const uint CellGameParamidVersion = 101;
        internal const uint CellGameParamidParentalLevel = 102;
        internal const uint CellGameParamidResolution = 103;
        internal const uint CellGameParamidSoundFormat = 104;
        internal const uint CellGameParamidPs3SystemVer = 105;
        internal const uint CellGameParamidAppVer = 106;

        internal const uint CellGameErrorNotpatch = 0x8002CB27;

        internal static readonly ImmutableDictionary<uint, string> CellGameParamId = new Dictionary<uint, string>()
        {
            [CellGameParamidTitle] = "TITLE",
            [CellGameParamidTitleDefault] = "TITLE",
            [CellGameParamidTitleJapanese] = "TITLE_00",
            [CellGameParamidTitleEnglish] = "TITLE_01",
            [CellGameParamidTitleFrench] = "TITLE_02",
            [CellGameParamidTitleSpanish] = "TITLE_03",
            [CellGameParamidTitleGerman] = "TITLE_04",
            [CellGameParamidTitleItalian] = "TITLE_05",
            [CellGameParamidTitleDutch] = "TITLE_06",
            [CellGameParamidTitlePortuguese] = "TITLE_07",
            [CellGameParamidTitleRussian] = "TITLE_08",
            [CellGameParamidTitleKorean] = "TITLE_09",
            [CellGameParamidTitleChineseT] = "TITLE_10",
            [CellGameParamidTitleChineseS] = "TITLE_11",
            [CellGameParamidTitleFinnish] = "TITLE_12",
            [CellGameParamidTitleSwedish] = "TITLE_13",
            [CellGameParamidTitleDanish] = "TITLE_14",
            [CellGameParamidTitleNorwegian] = "TITLE_15",
            [CellGameParamidTitlePolish] = "TITLE_16",
            [CellGameParamidTitlePortugueseBrazil] = "TITLE_17",
            [CellGameParamidTitleEnglishUk] = "TITLE_18",
            [CellGameParamidTitleTurkish] = "TITLE_19",

            [CellGameParamidTitleId] = "TITLE_ID",
            [CellGameParamidVersion] = "VERSION",
            [CellGameParamidPs3SystemVer] = "PS3_SYSTEM_VER",
            [CellGameParamidAppVer] = "APP_VER",

            [CellGameParamidParentalLevel] = "PARENTAL_LEVEL",
            [CellGameParamidResolution] = "RESOLUTION",
            [CellGameParamidSoundFormat] = "SOUND_FORMAT",
        }.ToImmutableDictionary();

        private readonly MemoryManager _memoryManager;
        private readonly FileSystemManager _fileSystemManager;

        private string GuestContentDirectory => FileSystemManager.GetGuestPath(_fileSystemManager.CurrentContentDirectory) + '\0';
        private string GuestUSRDIR => FileSystemManager.GetGuestPath(_fileSystemManager.CurrentContentDirectory) + @"/USRDIR" + '\0';

        internal CellGame(MemoryManager memoryManager, FileSystemManager fileSystemManager)
        {
            _memoryManager = memoryManager;
            _fileSystemManager = fileSystemManager;
        }

        internal ulong CellGameBootCheck(PPUThread ppu)
        {
            GuestPtr<BigEndian<uint>> typePtr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<BigEndian<uint>> attribPtr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);
            GuestPtr<CellGameContentSize> sizePtr = ppu.ReadArg<GuestPtr<CellGameContentSize>>(Arg2);
            GuestVoidPtr dirPtr = ppu.ReadArg<GuestVoidPtr>(Arg3);

            if (!dirPtr.IsNullPtr)
            {
                //copy content dir
                byte[] content_chars = Encoding.UTF8.GetBytes(GuestContentDirectory);
                _memoryManager.CopyBufferToVirtualMemory(content_chars, dirPtr);

                Console.WriteLine($"[CellGame] cellGameBootCheck path = {GuestContentDirectory}");
            }

            if (!sizePtr.IsNullPtr)
            {
                string hostDir = _fileSystemManager.CurrentContentDirectory;
                int size = (int)FileSystemManager.GetDirectorySize(hostDir);

                CellGameContentSize sizeStruct = new()
                {
                    HDDFreeSizeKB = 40 * 1024 * 1024 - 256,
                    SizeKB = size,
                    SysSizeKB = 4,
                };

                _memoryManager.Write(sizePtr, sizeStruct);
            }

            return CellOk;
        }

        internal ulong CellGameContentPermit(PPUThread ppu)
        {
            uint contentDirPtr = ppu.ReadArg<uint>(Arg0);
            uint UsrDirPtr = ppu.ReadArg<uint>(Arg1);

            // Strings are null terminated
            byte[] ContentChars = Encoding.UTF8.GetBytes(GuestContentDirectory);
            byte[] UsrDirChars = Encoding.UTF8.GetBytes(GuestUSRDIR);
            _memoryManager.CopyBufferToVirtualMemory(ContentChars, contentDirPtr);
            _memoryManager.CopyBufferToVirtualMemory(UsrDirChars, UsrDirPtr);

            Console.WriteLine($"[CellGame] cellGameContentPermit path = {GuestContentDirectory}");
            return CellOk;
        }

        internal ulong CellGameGetParamInt(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<uint>> value_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);

            string key = CellGameParamId[id];

            if (_fileSystemManager.TryGetCurrentSFO(out SFO? sfo))
            {
                if (sfo!.TryGetUInt(key, out uint value))
                {
                    _memoryManager.Write<BigEndian<uint>>(value_ptr, value);
                }
                else
                {
                    throw new Exception($"[CellGame] CellGameGetParamInt cannot find key = {key}");
                }

                Console.WriteLine($"[CellGame] CellGameGetParamInt key = {key} value = {value}");
            }
            else
            {
                throw new Exception($"[CellGame] Could not get SFO for the current executable");
            }

            return CellOk;
        }

        internal ulong CellGameGetParamString(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(Arg0);
            GuestPtr<BigEndian<uint>> bufferPtr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);
            int bufferSize = ppu.ReadArg<int>(Arg2);

            _memoryManager.ClearMemory(bufferPtr, bufferSize);
            string key = CellGameParamId[id];

            if (_fileSystemManager.TryGetCurrentSFO(out SFO? sfo))
            {
                if (sfo!.TryGetString(key, out string? value))
                {
                    // Add null terminator
                    Span<byte> str = Encoding.UTF8.GetBytes(value + '\0');

                    if (str.Length > bufferSize)
                    {
                        str = str.Slice(0, bufferSize);
                    }

                    // Copy to ptr
                    _memoryManager.CopyBufferToVirtualMemory(str, bufferPtr);

                    Console.WriteLine($"[CellGame] CellGameGetParamString key = {key} value = {value}");
                }
                else
                {
                    throw new Exception($"[CellGame] CellGameGetParamString cannot find key = {key}");
                }
            }
            else
            {
                throw new Exception($"[CellGame] Could not get SFO for the current executable");
            }

            return CellOk;
        }

        internal ulong CellGameDataCheck(PPUThread ppu)
        {
            Console.WriteLine($"[CellGame] cellGameDataCheck");
            return CellOk;
        }

        internal ulong CellGameGetDiscContentInfoUpdatePath(PPUThread ppu)
        {
            Console.WriteLine($"[CellGame] cellGameGetDiscContentInfoUpdatePath");
            return CellOk;
        }

        internal ulong CellGamePatchCheck(PPUThread ppu)
        {
            Console.WriteLine($"[CellGame] CellGamePatchCheck");
            return CellGameErrorNotpatch;
        }
    }
}