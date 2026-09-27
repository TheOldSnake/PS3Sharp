using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{

    public static class CellGame
    {

        static string ContentDirectory => PS3Sharp.ContentDir.Replace("\\", "/") + "\0";
        static string USRDIR => PS3Sharp.ContentDir.Replace("\\", "/") + @"/USRDIR" + "\0";

        public const uint CELL_GAME_PARAMID_TITLE = 0;
        public const uint CELL_GAME_PARAMID_TITLE_DEFAULT = 1;
        public const uint CELL_GAME_PARAMID_TITLE_JAPANESE = 2;
        public const uint CELL_GAME_PARAMID_TITLE_ENGLISH = 3;
        public const uint CELL_GAME_PARAMID_TITLE_FRENCH = 4;
        public const uint CELL_GAME_PARAMID_TITLE_SPANISH = 5;
        public const uint CELL_GAME_PARAMID_TITLE_GERMAN = 6;
        public const uint CELL_GAME_PARAMID_TITLE_ITALIAN = 7;
        public const uint CELL_GAME_PARAMID_TITLE_DUTCH = 8;
        public const uint CELL_GAME_PARAMID_TITLE_PORTUGUESE = 9;
        public const uint CELL_GAME_PARAMID_TITLE_RUSSIAN = 10;
        public const uint CELL_GAME_PARAMID_TITLE_KOREAN = 11;
        public const uint CELL_GAME_PARAMID_TITLE_CHINESE_T = 12;
        public const uint CELL_GAME_PARAMID_TITLE_CHINESE_S = 13;
        public const uint CELL_GAME_PARAMID_TITLE_FINNISH = 14;
        public const uint CELL_GAME_PARAMID_TITLE_SWEDISH = 15;
        public const uint CELL_GAME_PARAMID_TITLE_DANISH = 16;
        public const uint CELL_GAME_PARAMID_TITLE_NORWEGIAN = 17;
        public const uint CELL_GAME_PARAMID_TITLE_POLISH = 18;
        public const uint CELL_GAME_PARAMID_TITLE_PORTUGUESE_BRAZIL = 19;
        public const uint CELL_GAME_PARAMID_TITLE_ENGLISH_UK = 20;
        public const uint CELL_GAME_PARAMID_TITLE_TURKISH = 21;

        public const uint CELL_GAME_PARAMID_TITLE_ID = 100;
        public const uint CELL_GAME_PARAMID_VERSION = 101;
        public const uint CELL_GAME_PARAMID_PARENTAL_LEVEL = 102;
        public const uint CELL_GAME_PARAMID_RESOLUTION = 103;
        public const uint CELL_GAME_PARAMID_SOUND_FORMAT = 104;
        public const uint CELL_GAME_PARAMID_PS3_SYSTEM_VER = 105;
        public const uint CELL_GAME_PARAMID_APP_VER = 106;

        public const uint CELL_GAME_ERROR_NOTPATCH = 0x8002CB27;

        public static readonly Dictionary<uint, string> CellGameParamId = new() {
            { CELL_GAME_PARAMID_TITLE,                   "TITLE"          },
            { CELL_GAME_PARAMID_TITLE_DEFAULT,           "TITLE"          },
            { CELL_GAME_PARAMID_TITLE_JAPANESE,          "TITLE_00"       },
            { CELL_GAME_PARAMID_TITLE_ENGLISH,           "TITLE_01"       },
            { CELL_GAME_PARAMID_TITLE_FRENCH,            "TITLE_02"       },
            { CELL_GAME_PARAMID_TITLE_SPANISH,           "TITLE_03"       },
            { CELL_GAME_PARAMID_TITLE_GERMAN,            "TITLE_04"       },
            { CELL_GAME_PARAMID_TITLE_ITALIAN,           "TITLE_05"       },
            { CELL_GAME_PARAMID_TITLE_DUTCH,             "TITLE_06"       },
            { CELL_GAME_PARAMID_TITLE_PORTUGUESE,        "TITLE_07"       },
            { CELL_GAME_PARAMID_TITLE_RUSSIAN,           "TITLE_08"       },
            { CELL_GAME_PARAMID_TITLE_KOREAN,            "TITLE_09"       },
            { CELL_GAME_PARAMID_TITLE_CHINESE_T,         "TITLE_10"       },
            { CELL_GAME_PARAMID_TITLE_CHINESE_S,         "TITLE_11"       },
            { CELL_GAME_PARAMID_TITLE_FINNISH,           "TITLE_12"       },
            { CELL_GAME_PARAMID_TITLE_SWEDISH,           "TITLE_13"       },
            { CELL_GAME_PARAMID_TITLE_DANISH,            "TITLE_14"       },
            { CELL_GAME_PARAMID_TITLE_NORWEGIAN,         "TITLE_15"       },
            { CELL_GAME_PARAMID_TITLE_POLISH,            "TITLE_16"       },
            { CELL_GAME_PARAMID_TITLE_PORTUGUESE_BRAZIL, "TITLE_17"       },
            { CELL_GAME_PARAMID_TITLE_ENGLISH_UK,        "TITLE_18"       },
            { CELL_GAME_PARAMID_TITLE_TURKISH,           "TITLE_19"       },

            { CELL_GAME_PARAMID_TITLE_ID,                "TITLE_ID"       },
            { CELL_GAME_PARAMID_VERSION,                 "VERSION"        },
            { CELL_GAME_PARAMID_PS3_SYSTEM_VER,          "PS3_SYSTEM_VER" },
            { CELL_GAME_PARAMID_APP_VER,                 "APP_VER"        },

            { CELL_GAME_PARAMID_PARENTAL_LEVEL,          "PARENTAL_LEVEL" },
            { CELL_GAME_PARAMID_RESOLUTION,              "RESOLUTION"     },
            { CELL_GAME_PARAMID_SOUND_FORMAT,            "SOUND_FORMAT"   },
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct CellGameContentSize : IPS3Struct
        {
            public BigEndian<int> HDDFreeSizeKB;
            public BigEndian<int> SizeKB;
            public BigEndian<int> SysSizeKB;
        }

        public static ulong CellGameBootCheck(PPU ppu)
        {
            GuestPtr<uint> type_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<uint> attrib_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            GuestPtr<CellGameContentSize> size_ptr = ppu.ReadArg<GuestPtr<CellGameContentSize>>(ARG2);
            GuestVoidPtr dir_ptr = ppu.ReadArg<GuestVoidPtr>(ARG3);

            if (!dir_ptr.IsNullPtr)
            {
                //copy content dir
                byte[] content_chars = Encoding.UTF8.GetBytes(ContentDirectory);
                MemoryManager.CopyBufferToVirtualMemory(content_chars, dir_ptr);

                Console.WriteLine($"[CellGame] cellGameBootCheck path = {ContentDirectory}");
            }

            if (!size_ptr.IsNullPtr)
            {
                string host_dir = PS3Sharp.FileSystemBase + PS3Sharp.ContentDir;
                int size = (int)FileSystem.FileSystemManager.GetDirectorySize(host_dir);

                unsafe
                {
                    CellGameContentSize* size_struct = size_ptr.AsNativePtr();
                    size_struct->HDDFreeSizeKB = 40 * 1024 * 1024 - 256;
                    size_struct->SizeKB = size;
                    size_struct->SysSizeKB = 4;
                }
            }

            return CELL_OK;
        }

        public static ulong CellGameContentPermit(PPU ppu)
        {
            uint content_dir_ptr = ppu.ReadArg<uint>(ARG0);
            uint usr_dir_ptr = ppu.ReadArg<uint>(ARG1);

            byte[] content_chars = Encoding.UTF8.GetBytes(ContentDirectory);
            byte[] usr_dir_chars = Encoding.UTF8.GetBytes(USRDIR);
            MemoryManager.CopyBufferToVirtualMemory(content_chars, content_dir_ptr);
            MemoryManager.CopyBufferToVirtualMemory(usr_dir_chars, usr_dir_ptr);

            //null terminate strings
            //MemoryManager.Write<byte>(content_dir_ptr + (uint)content_chars.Length, 0);
            //MemoryManager.Write<byte>(usr_dir_ptr + (uint)usr_dir_chars.Length, 0);

            Console.WriteLine($"[CellGame] cellGameContentPermit path = {ContentDirectory}");
            return CELL_OK;
        }

        public static ulong CellGameGetParamInt(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> value_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);

            string key = CellGameParamId[id];

            if (PS3Sharp.SFO.UIntLookup(key, out uint value))
            {
                MemoryManager.Write<uint>(value_ptr, value);
            }
            else
            {
                throw new Exception($"[CellGame] CellGameGetParamInt cannot find key = {key}");
            }

            Console.WriteLine($"[CellGame] CellGameGetParamInt key = {key} value = {value}");
            return CELL_OK;
        }

        public static ulong CellGameGetParamString(PPU ppu)
        {
            uint id = ppu.ReadArg<uint>(ARG0);
            GuestPtr<uint> buffer_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);
            uint bufferSize = ppu.ReadArg<uint>(ARG2);

            string key = CellGameParamId[id];


            if (PS3Sharp.SFO.StringLookup(key, out string value))
            {
                byte[] str = Encoding.UTF8.GetBytes(value + "\0");

                for (uint i = 0; i < bufferSize; i++)
                {
                    uint offset = buffer_ptr + i;
                    MemoryManager.Write<byte>(offset, str[i]);
                    if (str[i] == 0)
                    {
                        break;
                    }
                }

            }
            else
            {
                throw new Exception($"[CellGame] CellGameGetParamString cannot find key = {key}");
            }


            Console.WriteLine($"[CellGame] CellGameGetParamString key = {key} value = {value}");
            return CELL_OK;
        }

        public static ulong CellGameDataCheck(PPU ppu)
        {
            Console.WriteLine($"[CellGame] cellGameDataCheck");
            return CELL_OK;
        }

        public static ulong CellGameGetDiscContentInfoUpdatePath(PPU ppu)
        {
            Console.WriteLine($"[CellGame] cellGameGetDiscContentInfoUpdatePath");
            return CELL_OK;
        }

        public static ulong CellGamePatchCheck(PPU ppu)
        {
            Console.WriteLine($"[CellGame] CellGamePatchCheck");
            return CELL_GAME_ERROR_NOTPATCH;
        }
    }
}
