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

        internal const uint CELL_GAME_PARAMID_TITLE = 0;
        internal const uint CELL_GAME_PARAMID_TITLE_DEFAULT = 1;
        internal const uint CELL_GAME_PARAMID_TITLE_JAPANESE = 2;
        internal const uint CELL_GAME_PARAMID_TITLE_ENGLISH = 3;
        internal const uint CELL_GAME_PARAMID_TITLE_FRENCH = 4;
        internal const uint CELL_GAME_PARAMID_TITLE_SPANISH = 5;
        internal const uint CELL_GAME_PARAMID_TITLE_GERMAN = 6;
        internal const uint CELL_GAME_PARAMID_TITLE_ITALIAN = 7;
        internal const uint CELL_GAME_PARAMID_TITLE_DUTCH = 8;
        internal const uint CELL_GAME_PARAMID_TITLE_PORTUGUESE = 9;
        internal const uint CELL_GAME_PARAMID_TITLE_RUSSIAN = 10;
        internal const uint CELL_GAME_PARAMID_TITLE_KOREAN = 11;
        internal const uint CELL_GAME_PARAMID_TITLE_CHINESE_T = 12;
        internal const uint CELL_GAME_PARAMID_TITLE_CHINESE_S = 13;
        internal const uint CELL_GAME_PARAMID_TITLE_FINNISH = 14;
        internal const uint CELL_GAME_PARAMID_TITLE_SWEDISH = 15;
        internal const uint CELL_GAME_PARAMID_TITLE_DANISH = 16;
        internal const uint CELL_GAME_PARAMID_TITLE_NORWEGIAN = 17;
        internal const uint CELL_GAME_PARAMID_TITLE_POLISH = 18;
        internal const uint CELL_GAME_PARAMID_TITLE_PORTUGUESE_BRAZIL = 19;
        internal const uint CELL_GAME_PARAMID_TITLE_ENGLISH_UK = 20;
        internal const uint CELL_GAME_PARAMID_TITLE_TURKISH = 21;

        internal const uint CELL_GAME_PARAMID_TITLE_ID = 100;
        internal const uint CELL_GAME_PARAMID_VERSION = 101;
        internal const uint CELL_GAME_PARAMID_PARENTAL_LEVEL = 102;
        internal const uint CELL_GAME_PARAMID_RESOLUTION = 103;
        internal const uint CELL_GAME_PARAMID_SOUND_FORMAT = 104;
        internal const uint CELL_GAME_PARAMID_PS3_SYSTEM_VER = 105;
        internal const uint CELL_GAME_PARAMID_APP_VER = 106;

        internal const uint CELL_GAME_ERROR_NOTPATCH = 0x8002CB27;

        internal static readonly ImmutableDictionary<uint, string> CellGameParamId = new Dictionary<uint, string>()
        {
            [CELL_GAME_PARAMID_TITLE] = "TITLE",
            [CELL_GAME_PARAMID_TITLE_DEFAULT] = "TITLE",
            [CELL_GAME_PARAMID_TITLE_JAPANESE] = "TITLE_00",
            [CELL_GAME_PARAMID_TITLE_ENGLISH] = "TITLE_01",
            [CELL_GAME_PARAMID_TITLE_FRENCH] = "TITLE_02",
            [CELL_GAME_PARAMID_TITLE_SPANISH] = "TITLE_03",
            [CELL_GAME_PARAMID_TITLE_GERMAN] = "TITLE_04",
            [CELL_GAME_PARAMID_TITLE_ITALIAN] = "TITLE_05",
            [CELL_GAME_PARAMID_TITLE_DUTCH] = "TITLE_06",
            [CELL_GAME_PARAMID_TITLE_PORTUGUESE] = "TITLE_07",
            [CELL_GAME_PARAMID_TITLE_RUSSIAN] = "TITLE_08",
            [CELL_GAME_PARAMID_TITLE_KOREAN] = "TITLE_09",
            [CELL_GAME_PARAMID_TITLE_CHINESE_T] = "TITLE_10",
            [CELL_GAME_PARAMID_TITLE_CHINESE_S] = "TITLE_11",
            [CELL_GAME_PARAMID_TITLE_FINNISH] = "TITLE_12",
            [CELL_GAME_PARAMID_TITLE_SWEDISH] = "TITLE_13",
            [CELL_GAME_PARAMID_TITLE_DANISH] = "TITLE_14",
            [CELL_GAME_PARAMID_TITLE_NORWEGIAN] = "TITLE_15",
            [CELL_GAME_PARAMID_TITLE_POLISH] = "TITLE_16",
            [CELL_GAME_PARAMID_TITLE_PORTUGUESE_BRAZIL] = "TITLE_17",
            [CELL_GAME_PARAMID_TITLE_ENGLISH_UK] = "TITLE_18",
            [CELL_GAME_PARAMID_TITLE_TURKISH] = "TITLE_19",

            [CELL_GAME_PARAMID_TITLE_ID] = "TITLE_ID",
            [CELL_GAME_PARAMID_VERSION] = "VERSION",
            [CELL_GAME_PARAMID_PS3_SYSTEM_VER] = "PS3_SYSTEM_VER",
            [CELL_GAME_PARAMID_APP_VER] = "APP_VER",

            [CELL_GAME_PARAMID_PARENTAL_LEVEL] = "PARENTAL_LEVEL",
            [CELL_GAME_PARAMID_RESOLUTION] = "RESOLUTION",
            [CELL_GAME_PARAMID_SOUND_FORMAT] = "SOUND_FORMAT",
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
            GuestPtr<uint> typePtr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<uint> attribPtr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            GuestPtr<CellGameContentSize> sizePtr = ppu.ReadArg<GuestPtr<CellGameContentSize>>(ARG2);
            GuestVoidPtr dirPtr = ppu.ReadArg<GuestVoidPtr>(ARG3);

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

            return CELL_OK;
        }

        internal ulong CellGameContentPermit(PPUThread ppu)
        {
            uint contentDirPtr = ppu.ReadArg<uint>(ARG0);
            uint UsrDirPtr = ppu.ReadArg<uint>(ARG1);

            // Strings are null terminated
            byte[] ContentChars = Encoding.UTF8.GetBytes(GuestContentDirectory);
            byte[] UsrDirChars = Encoding.UTF8.GetBytes(GuestUSRDIR);
            _memoryManager.CopyBufferToVirtualMemory(ContentChars, contentDirPtr);
            _memoryManager.CopyBufferToVirtualMemory(UsrDirChars, UsrDirPtr);

            Console.WriteLine($"[CellGame] cellGameContentPermit path = {GuestContentDirectory}");
            return CELL_OK;
        }

        internal ulong CellGameGetParamInt(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> value_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);

            string key = CellGameParamId[id];

            if (_fileSystemManager.TryGetCurrentSFO(out SFO? sfo))
            {
                if (sfo!.UIntLookup(key, out uint value))
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

            return CELL_OK;
        }

        internal ulong CellGameGetParamString(PPUThread ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> bufferPtr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            int bufferSize = ppu.ReadArg<int>(ARG2);

            _memoryManager.ClearMemory(bufferPtr, bufferSize);
            string key = CellGameParamId[id];

            if (_fileSystemManager.TryGetCurrentSFO(out SFO? sfo))
            {
                if (sfo!.StringLookup(key, out string value))
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

            return CELL_OK;
        }

        internal ulong CellGameDataCheck(PPUThread ppu)
        {
            Console.WriteLine($"[CellGame] cellGameDataCheck");
            return CELL_OK;
        }

        internal ulong CellGameGetDiscContentInfoUpdatePath(PPUThread ppu)
        {
            Console.WriteLine($"[CellGame] cellGameGetDiscContentInfoUpdatePath");
            return CELL_OK;
        }

        internal ulong CellGamePatchCheck(PPUThread ppu)
        {
            Console.WriteLine($"[CellGame] CellGamePatchCheck");
            return CELL_GAME_ERROR_NOTPATCH;
        }
    }
}