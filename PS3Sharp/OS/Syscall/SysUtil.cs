using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

using System.Runtime.InteropServices;
using OpenTK.Graphics.ES11;

namespace PS3Sharp.OS.Syscall
{
    public static class SysUtil
    {
        // [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        // public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        public const uint CELL_SYSUTIL_ERROR_VALUE = 0x8002B102;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellAudioState : IPS3Struct
        {
            public byte State;
            public byte Encoder;
            public InlineData.InlineArray6<byte> Reserved;
            public BigEndian<uint> DownMixer;
            public CellAudioOutSoundMode SoundMode;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellAudioOutSoundMode : IPS3Struct
        {
            public byte Type;
            public byte Channel;
            public byte Fs;
            public byte Reserved;
            public BigEndian<uint> Layout;
        }

        public struct SysUtilCallback
        {
            public GuestVoidPtr AddressPtr;
            public GuestVoidPtr UserDataPtr;
        }


        // Should probably move these somewhere else
        public enum SYSTEM_PARAM : uint
        {
            // Integers
            CELL_SYSUTIL_SYSTEMPARAM_ID_LANG = 0x0111,
            CELL_SYSUTIL_SYSTEMPARAM_ID_ENTER_BUTTON_ASSIGN = 0x0112,
            CELL_SYSUTIL_SYSTEMPARAM_ID_DATE_FORMAT = 0x0114,
            CELL_SYSUTIL_SYSTEMPARAM_ID_TIME_FORMAT = 0x0115,
            CELL_SYSUTIL_SYSTEMPARAM_ID_TIMEZONE = 0x0116,
            CELL_SYSUTIL_SYSTEMPARAM_ID_SUMMERTIME = 0x0117,
            CELL_SYSUTIL_SYSTEMPARAM_ID_GAME_PARENTAL_LEVEL = 0x0121,
            CELL_SYSUTIL_SYSTEMPARAM_ID_GAME_PARENTAL_LEVEL0_RESTRICT = 0x0123,
            CELL_SYSUTIL_SYSTEMPARAM_ID_CURRENT_USER_HAS_NP_ACCOUNT = 0x0141,
            CELL_SYSUTIL_SYSTEMPARAM_ID_CAMERA_PLFREQ = 0x0151,
            CELL_SYSUTIL_SYSTEMPARAM_ID_PAD_RUMBLE = 0x0152,
            CELL_SYSUTIL_SYSTEMPARAM_ID_KEYBOARD_TYPE = 0x0153,
            CELL_SYSUTIL_SYSTEMPARAM_ID_JAPANESE_KEYBOARD_ENTRY_METHOD = 0x0154,
            CELL_SYSUTIL_SYSTEMPARAM_ID_CHINESE_KEYBOARD_ENTRY_METHOD = 0x0155,
            CELL_SYSUTIL_SYSTEMPARAM_ID_PAD_AUTOOFF = 0x0156,

            // Strings
            CELL_SYSUTIL_SYSTEMPARAM_ID_NICKNAME = 0x0113,
            CELL_SYSUTIL_SYSTEMPARAM_ID_CURRENT_USERNAME = 0x0131,
        }

        public enum SYSTEM_LANG : uint
        {
            CELL_SYSUTIL_LANG_JAPANESE = 0,
            CELL_SYSUTIL_LANG_ENGLISH_US = 1,
            CELL_SYSUTIL_LANG_FRENCH = 2,
            CELL_SYSUTIL_LANG_SPANISH = 3,
            CELL_SYSUTIL_LANG_GERMAN = 4,
            CELL_SYSUTIL_LANG_ITALIAN = 5,
            CELL_SYSUTIL_LANG_DUTCH = 6,
            CELL_SYSUTIL_LANG_PORTUGUESE_PT = 7,
            CELL_SYSUTIL_LANG_RUSSIAN = 8,
            CELL_SYSUTIL_LANG_KOREAN = 9,
            CELL_SYSUTIL_LANG_CHINESE_T = 10,
            CELL_SYSUTIL_LANG_CHINESE_S = 11,
            CELL_SYSUTIL_LANG_FINNISH = 12,
            CELL_SYSUTIL_LANG_SWEDISH = 13,
            CELL_SYSUTIL_LANG_DANISH = 14,
            CELL_SYSUTIL_LANG_NORWEGIAN = 15,
            CELL_SYSUTIL_LANG_POLISH = 16,
            CELL_SYSUTIL_LANG_PORTUGUESE_BR = 17,
            CELL_SYSUTIL_LANG_ENGLISH_GB = 18,
        }

        public enum ENTER_BUTTON : uint
        {
            CELL_SYSUTIL_ENTER_BUTTON_ASSIGN_CIRCLE = 0,
            CELL_SYSUTIL_ENTER_BUTTON_ASSIGN_CROSS = 1,
        }

        public enum DATE_FMT : uint
        {
            CELL_SYSUTIL_DATE_FMT_YYYYMMDD = 0,
            CELL_SYSUTIL_DATE_FMT_DDMMYYYY = 1,
            CELL_SYSUTIL_DATE_FMT_MMDDYYYY = 2,
        }

        public enum TIME_FMT : uint
        {
            CELL_SYSUTIL_TIME_FMT_CLOCK12 = 0,
            CELL_SYSUTIL_TIME_FMT_CLOCK24 = 1,
        }

        public enum PARENTAL_LEVEL : uint
        {
            CELL_SYSUTIL_GAME_PARENTAL_OFF = 0,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL01 = 1,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL02 = 2,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL03 = 3,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL04 = 4,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL05 = 5,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL06 = 6,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL07 = 7,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL08 = 8,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL09 = 9,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL10 = 10,
            CELL_SYSUTIL_GAME_PARENTAL_LEVEL11 = 11,
        }

        public enum PAD_RUMBLE : uint
        {
            CELL_SYSUTIL_PAD_RUMBLE_OFF = 0,
            CELL_SYSUTIL_PAD_RUMBLE_ON = 1,
        }

        public enum CAMERA_PLFREQ : uint
        {
            CELL_SYSUTIL_CAMERA_PLFREQ_DISABLED = 0,
            CELL_SYSUTIL_CAMERA_PLFREQ_50HZ = 1,
            CELL_SYSUTIL_CAMERA_PLFREQ_60HZ = 2,
            CELL_SYSUTIL_CAMERA_PLFREQ_DEVCIE_DEPEND = 4,
        }

        public enum STRING_SIZES : uint
        {
            CELL_SYSUTIL_SYSTEMPARAM_NICKNAME_SIZE = 0x80,
            CELL_SYSUTIL_SYSTEMPARAM_CURRENT_USERNAME_SIZE = 0x40
        }


        public static ulong CellSysutilRegisterCallback(PPU ppu)
        {
            //TODO
            uint index = ppu.ReadArg<uint>(ARG0);
            GuestVoidPtr address_ptr = ppu.ReadArg<GuestVoidPtr>(ARG1); //this is a pointer to the address, not the code
            GuestVoidPtr userdata_ptr = ppu.ReadArg<GuestVoidPtr>(ARG2);

            if (index > 3)
            {
                return CELL_SYSUTIL_ERROR_VALUE;
            }

            ppu.SysUtilCallbacks[index] = new SysUtilCallback
            {
                AddressPtr = address_ptr,
                UserDataPtr = userdata_ptr
            };

            Console.WriteLine($"[CellSysutil] cellSysutilRegisterCallback index = {index} - address: 0x{address_ptr:X8}");
            return CELL_OK;
        }

        public static ulong CellSysutilUnregisterCallback(PPU ppu)
        {
            uint index = ppu.ReadArg<uint>(ARG0);

            if (index > 3)
            {
                return CELL_SYSUTIL_ERROR_VALUE;
            }

            ppu.SysUtilCallbacks[index].AddressPtr = 0;
            ppu.SysUtilCallbacks[index].UserDataPtr = 0;

            Console.WriteLine($"[CellSysutil] cellSysutilUnregisterCallback");
            return CELL_OK;
        }

        public static ulong CellSysutilCheckCallback(PPU ppu)
        {
            // i have no idea how this works

            // LittleBigPlanet Seems to need this - other games might crash so idk

            // Console.WriteLine($"[CellSysutil] cellSysutilCheckCallback -> CELL_OK");
            // return CELL_OK;

            Span<ulong> oldArgs = stackalloc ulong[5];
            Span<ulong> args = stackalloc ulong[5];

            args.Clear();
            oldArgs.Clear();

            // Save old arg registers
            oldArgs[0] = ppu.ReadArg<ulong>(ARG0);
            oldArgs[1] = ppu.ReadArg<ulong>(ARG1);
            oldArgs[2] = ppu.ReadArg<ulong>(ARG2);
            oldArgs[3] = ppu.ReadArg<ulong>(ARG3);
            oldArgs[4] = ppu.ReadArg<ulong>(ARG4);

            for (int i = 0; i < ppu.SysUtilCallbacks.Length; i++)
            {
                SysUtilCallback callback = ppu.SysUtilCallbacks[i];
                if (!callback.AddressPtr.IsNullPtr)
                {

                    // Pass the user data as arg0
                    args[0] = callback.UserDataPtr;

                    // Other registers stay unchanged
                    oldArgs.Slice(1).CopyTo(args.Slice(1));

                    //uint pc = MemoryManager.Read<uint>(callback.AddressPtr);
                    //Console.WriteLine($"[{ppu.ThreadName}] [CellSysutil] Executing callback[{i}] addr = 0x{callback.AddressPtr:X8} pc = 0x{pc:X8} arg = 0x{args[0]:X8}");

                    PPUInterpreter.ExecuteFunction(ppu, callback.AddressPtr, toc: true, args);

                    // ?
                    //ppu.SysUtilCallbacks[i].AddressPtr = 0;
                    //ppu.SysUtilCallbacks[i].UserDataPtr = 0;
                }
            }


            return CELL_OK;
        }

        public static ulong CellAudioOutGetSoundAvailability(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellAudioOutGetSoundAvailability");
            const ulong CELL_AUDIO_OUT_PRIMARY = 2;
            return CELL_OK;
        }

        public static ulong CellAudioOutConfigure(PPU ppu)
        {
            //TODO
            const ulong CELL_AUDIO_OUT_ERROR_UNSUPPORTED_AUDIO_OUT = 0x8002b245;
            Console.WriteLine($"[CellSysutil] cellAudioOutConfigure");
            return CELL_OK;
        }

        public static ulong CellAudioOutGetState(PPU ppu)
        {
            //TODO
            const byte CELL_AUDIO_OUT_OUTPUT_STATE_ENABLED = 0;
            const byte CELL_AUDIO_OUT_OUTPUT_STATE_DISABLED = 1;
            const byte CELL_AUDIO_OUT_OUTPUT_STATE_PREPARING = 2;

            GuestPtr<CellAudioState> state_ptr = ppu.ReadArg<GuestPtr<CellAudioState>>(ARG2);

            unsafe
            {
                CellAudioState* state = state_ptr.AsNativePtr();
                state->State = CELL_AUDIO_OUT_OUTPUT_STATE_ENABLED;
            }

            Console.WriteLine($"[CellSysutil] cellAudioOutGetState");
            return CELL_OK;
        }

        public static ulong CellSysutilGetSystemParamInt(PPU ppu)
        {
            uint param = ppu.ReadArg<uint>(ARG0);
            uint value_ptr = ppu.ReadArg<uint>(ARG1);

            Console.WriteLine($"[CellSysutil] cellSysutilGetSystemParamInt param = {(SYSTEM_PARAM)param} ptr = 0x{value_ptr:X8}");

            if (param == 0)
            {
                return CELL_SYSUTIL_ERROR_VALUE;
            }

            SYSTEM_PARAM system_param = (SYSTEM_PARAM)param;
            uint value;

            switch (system_param)
            {
                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_LANG:
                    value = (uint)SYSTEM_LANG.CELL_SYSUTIL_LANG_ENGLISH_US;
                    break;

                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_ENTER_BUTTON_ASSIGN:
                    value = (uint)ENTER_BUTTON.CELL_SYSUTIL_ENTER_BUTTON_ASSIGN_CROSS;
                    break;

                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_DATE_FORMAT:
                    value = (uint)DATE_FMT.CELL_SYSUTIL_DATE_FMT_DDMMYYYY;
                    break;

                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_TIME_FORMAT:
                    value = (uint)TIME_FMT.CELL_SYSUTIL_TIME_FMT_CLOCK24;
                    break;

                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_TIMEZONE:
                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_SUMMERTIME:
                    value = 0;
                    break;

                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_GAME_PARENTAL_LEVEL:
                    value = (uint)PARENTAL_LEVEL.CELL_SYSUTIL_GAME_PARENTAL_OFF;
                    break;

                case SYSTEM_PARAM.CELL_SYSUTIL_SYSTEMPARAM_ID_CURRENT_USER_HAS_NP_ACCOUNT:
                    value = 0;
                    break;


                default:
                    throw new Exception($"[CellSysutil] unknown param = 0x{param:X8}");
            }


            MemoryManager.Write<uint>(value_ptr, value);
            return CELL_OK;
        }

        public static ulong CellSysutilGetSystemParamString(PPU ppu)
        {
            //TODO

            uint param = ppu.ReadArg<uint>(ARG0);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(ARG1);
            uint size = ppu.ReadArg<uint>(ARG2);

            Console.WriteLine($"[CellSysutil] CellSysutilGetSystemParamString param = 0x{param:X8}");

            if (buf.IsNullPtr)
            {
                return CELL_SYSUTIL_ERROR_VALUE;
            }

            byte[] data = Encoding.UTF8.GetBytes("Snake\0");

            MemoryManager.ClearMemory(buf, size);
            MemoryManager.CopyBufferToVirtualMemory(data, buf);

            return CELL_OK;
        }

        public static ulong CellSaveDataUserAutoLoad(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellSaveDataUserAutoLoad");
            return CELL_OK;
        }

        public static ulong CellSaveDataAutoLoad2(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellSaveDataAutoLoad2");
            return CELL_OK;
        }

        public static ulong CellSaveDataAutoSave2(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellSaveDataAutoSave2");
            return CELL_OK;
        }

        public static ulong CellMsgDialogOpen2(PPU ppu)
        {
            //TODO: Implement actual dialog

            uint type = ppu.ReadArg<uint>(ARG0);
            GuestVoidPtr msg_ptr = ppu.ReadArg<GuestVoidPtr>(ARG1);
            GuestVoidPtr callback_ptr = ppu.ReadArg<GuestVoidPtr>(ARG2);
            GuestVoidPtr user_data = ppu.ReadArg<GuestVoidPtr>(ARG3);       //is this a pointer?
            uint param = ppu.ReadArg<uint>(ARG3);

            string msg = MemoryManager.ReadString(msg_ptr);

            Console.WriteLine($"[CellSysutil] cellMsgDialogOpen2: {msg}");

            //Task.Run(() =>
            //{
            //    MessageBox(IntPtr.Zero, msg, "CellMsgDialogOpen2", 0);
            //});

            // Callback  - toc needed ?
            PPUInterpreter.PrintCallstack(ppu);

            PPUInterpreter.ExecuteFunction(ppu, callback_ptr, toc: false, [1, user_data, 0, 0, 0]); //First arg = 1 for "Pressed OK"
            return CELL_OK;
        }

        public static ulong CellMsgDialogProgressBarSetMsg(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellMsgDialogProgressBarSetMsg");
            return CELL_OK;
        }

        public static ulong CellMsgDialogProgressBarInc(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellMsgDialogProgressBarInc");
            return CELL_OK;
        }

        public static ulong CellMsgDialogClose(PPU ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellMsgDialogClose");
            return CELL_OK;
        }

        public static ulong CellSaveDataEnableOverlay(PPU ppu)
        {
            Console.WriteLine($"[CellSysutil] cellSaveDataEnableOverlay");
            return CELL_OK;
        }
    }
}
