using PS3Sharp.Cell;
using PS3Sharp.Cell.Ppu;
using PS3Sharp.Cell.Ppu.Interpreter;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using System.Runtime.InteropServices;
using System.Text;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Syscall
{
    internal sealed class SysUtil
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CellAudioState : IPS3Struct
        {
            internal byte State;
            internal byte Encoder;
            internal InlineData.InlineArray6<byte> Reserved;
            internal BigEndian<uint> DownMixer;
            internal CellAudioOutSoundMode SoundMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellAudioOutSoundMode : IPS3Struct
        {
            internal byte Type;
            internal byte Channel;
            internal byte Fs;
            internal byte Reserved;
            internal BigEndian<uint> Layout;
        }

        internal struct SysUtilCallback
        {
            internal GuestVoidPtr AddressPtr;
            internal GuestVoidPtr UserDataPtr;
        }

        // Should probably move these somewhere else
        internal enum SystemParam : uint
        {
            // Integers
            CellSysUtilSystemParamIdLang = 0x0111,
            CellSysUtilSystemParamIdEnterButtonAssign = 0x0112,
            CellSysUtilSystemParamIdDateFormat = 0x0114,
            CellSysUtilSystemParamIdTimeFormat = 0x0115,
            CellSysUtilSystemParamIdTimezone = 0x0116,
            CellSysUtilSystemParamIdSummertime = 0x0117,
            CellSysUtilSystemParamIdGameParentalLevel = 0x0121,
            CellSysUtilSystemParamIdGameParentalLevel0Restrict = 0x0123,
            CellSysUtilSystemParamIdCurrentUserHasNpAccount = 0x0141,
            CellSysUtilSystemParamIdCameraPlfreq = 0x0151,
            CellSysUtilSystemParamIdPadRumble = 0x0152,
            CellSysUtilSystemParamIdKeyboardType = 0x0153,
            CellSysUtilSystemParamIdJapaneseKeyboardEntryMethod = 0x0154,
            CellSysUtilSystemParamIdChineseKeyboardEntryMethod = 0x0155,
            CellSysUtilSystemParamIdPadAutooff = 0x0156,

            // Strings
            CellSysUtilSystemParamIdNickName = 0x0113,
            CellSysUtilSystemParamIdCurrentUserName = 0x0131,
        }

        internal enum SystemLang : uint
        {
            CellSysutilLangJapanese = 0,
            CellSysutilLangEnglishUs = 1,
            CellSysutilLangFrench = 2,
            CellSysutilLangSpanish = 3,
            CellSysutilLangGerman = 4,
            CellSysutilLangItalian = 5,
            CellSysutilLangDutch = 6,
            CellSysutilLangPortuguesePt = 7,
            CellSysutilLangRussian = 8,
            CellSysutilLangKorean = 9,
            CellSysutilLangChineseT = 10,
            CellSysutilLangChineseS = 11,
            CellSysutilLangFinnish = 12,
            CellSysutilLangSwedish = 13,
            CellSysutilLangDanish = 14,
            CellSysutilLangNorwegian = 15,
            CellSysutilLangPolish = 16,
            CellSysutilLangPortugueseBr = 17,
            CellSysutilLangEnglishGb = 18,
        }

        internal enum EnterButton : uint
        {
            CellSysutilEnterButtonAssignCircle = 0,
            CellSysutilEnterButtonAssignCross = 1,
        }

        internal enum DateFmt : uint
        {
            CellSysutilDateFmtYyyymmdd = 0,
            CellSysutilDateFmtDdmmyyyy = 1,
            CellSysutilDateFmtMmddyyyy = 2,
        }

        internal enum TimeFmt : uint
        {
            CellSysutilTimeFmtClock12 = 0,
            CellSysutilTimeFmtClock24 = 1,
        }

        internal enum ParentalLevel : uint
        {
            CellSysutilGameParentalOff = 0,
            CellSysutilGameParentalLevel01 = 1,
            CellSysutilGameParentalLevel02 = 2,
            CellSysutilGameParentalLevel03 = 3,
            CellSysutilGameParentalLevel04 = 4,
            CellSysutilGameParentalLevel05 = 5,
            CellSysutilGameParentalLevel06 = 6,
            CellSysutilGameParentalLevel07 = 7,
            CellSysutilGameParentalLevel08 = 8,
            CellSysutilGameParentalLevel09 = 9,
            CellSysutilGameParentalLevel10 = 10,
            CellSysutilGameParentalLevel11 = 11,
        }

        internal enum PadRumble : uint
        {
            CellSysutilPadRumbleOff = 0,
            CellSysutilPadRumbleOn = 1,
        }

        internal enum CameraPlfreq : uint
        {
            CellSysutilCameraPlfreqDisabled = 0,
            CellSysutilCameraPlfreq50Hz = 1,
            CellSysutilCameraPlfreq60Hz = 2,
            CellSysutilCameraPlfreqDevcieDepend = 4,
        }

        internal enum StringSizes : uint
        {
            CellSysutilSystemparamNicknameSize = 0x80,
            CellSysutilSystemparamCurrentUsernameSize = 0x40
        }

        internal const uint CellSysutilErrorValue = 0x8002B102;
        private readonly MemoryManager _memoryManager;

        internal SysUtil(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
        }

        internal ulong CellSysutilRegisterCallback(PpuThread ppu)
        {
            //TODO
            uint index = ppu.ReadArg<uint>(Arg0);
            GuestVoidPtr address_ptr = ppu.ReadArg<GuestVoidPtr>(Arg1); //this is a pointer to the address, not the code
            GuestVoidPtr userdata_ptr = ppu.ReadArg<GuestVoidPtr>(Arg2);

            if (index > 3)
            {
                return CellSysutilErrorValue;
            }

            ppu.SysUtilCallbacks[index] = new SysUtilCallback
            {
                AddressPtr = address_ptr,
                UserDataPtr = userdata_ptr
            };

            Console.WriteLine($"[CellSysutil] cellSysutilRegisterCallback index = {index} - address: 0x{address_ptr:X8}");
            return CellOk;
        }

        internal ulong CellSysutilUnregisterCallback(PpuThread ppu)
        {
            uint index = ppu.ReadArg<uint>(Arg0);

            if (index > 3)
            {
                return CellSysutilErrorValue;
            }

            ppu.SysUtilCallbacks[index].AddressPtr = 0;
            ppu.SysUtilCallbacks[index].UserDataPtr = 0;

            Console.WriteLine($"[CellSysutil] cellSysutilUnregisterCallback");
            return CellOk;
        }

        internal ulong CellSysutilCheckCallback(PpuThread ppu)
        {
            // i have no idea how this works

            // LittleBigPlanet Seems to need this - other games might crash so idk

            // Console.WriteLine($"[CellSysutil] cellSysutilCheckCallback -> CELL_OK");
            // return CellOk;

            Span<ulong> oldArgs = stackalloc ulong[5];
            Span<ulong> args = stackalloc ulong[5];

            args.Clear();
            oldArgs.Clear();

            // Save old arg registers
            oldArgs[0] = ppu.ReadArg<ulong>(Arg0);
            oldArgs[1] = ppu.ReadArg<ulong>(Arg1);
            oldArgs[2] = ppu.ReadArg<ulong>(Arg2);
            oldArgs[3] = ppu.ReadArg<ulong>(Arg3);
            oldArgs[4] = ppu.ReadArg<ulong>(Arg4);

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

                    PpuInterpreter.ExecuteFunction(ppu, callback.AddressPtr, toc: true, args);

                    // ?
                    //ppu.SysUtilCallbacks[i].AddressPtr = 0;
                    //ppu.SysUtilCallbacks[i].UserDataPtr = 0;
                }
            }


            return CellOk;
        }

        internal ulong CellAudioOutGetSoundAvailability(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellAudioOutGetSoundAvailability");
            const ulong CellAudioOutPrimary = 2;
            return CellAudioOutPrimary;
        }

        internal ulong CellAudioOutConfigure(PpuThread ppu)
        {
            //TODO
            const ulong CellAudioOutErrorUnsupportedAudioOut = 0x8002b245;
            Console.WriteLine($"[CellSysutil] cellAudioOutConfigure");
            return CellOk;
        }

        internal ulong CellAudioOutGetState(PpuThread ppu)
        {
            //TODO
            const byte CellAudioOutOutputStateEnabled = 0;
            const byte CellAudioOutOutputStateDisabled = 1;
            const byte CellAudioOutOutputStatePreparing = 2;

            GuestPtr<CellAudioState> state_ptr = ppu.ReadArg<GuestPtr<CellAudioState>>(Arg2);

            CellAudioState state = _memoryManager.Read<CellAudioState>(state_ptr);
            state.State = CellAudioOutOutputStateEnabled;
            _memoryManager.Write(state_ptr, state);

            Console.WriteLine($"[CellSysutil] cellAudioOutGetState");
            return CellOk;
        }

        internal ulong CellSysutilGetSystemParamInt(PpuThread ppu)
        {
            uint param = ppu.ReadArg<uint>(Arg0);
            uint value_ptr = ppu.ReadArg<uint>(Arg1);

            Console.WriteLine($"[CellSysutil] cellSysutilGetSystemParamInt param = {(SystemParam)param} ptr = 0x{value_ptr:X8}");

            if (param == 0)
            {
                return CellSysutilErrorValue;
            }

            SystemParam system_param = (SystemParam)param;
            uint value;

            switch (system_param)
            {
                case SystemParam.CellSysUtilSystemParamIdLang:
                    value = (uint)SystemLang.CellSysutilLangEnglishUs;
                    break;

                case SystemParam.CellSysUtilSystemParamIdEnterButtonAssign:
                    value = (uint)EnterButton.CellSysutilEnterButtonAssignCross;
                    break;

                case SystemParam.CellSysUtilSystemParamIdDateFormat:
                    value = (uint)DateFmt.CellSysutilDateFmtDdmmyyyy;
                    break;

                case SystemParam.CellSysUtilSystemParamIdTimeFormat:
                    value = (uint)TimeFmt.CellSysutilTimeFmtClock24;
                    break;

                case SystemParam.CellSysUtilSystemParamIdTimezone:
                case SystemParam.CellSysUtilSystemParamIdSummertime:
                    value = 0;
                    break;

                case SystemParam.CellSysUtilSystemParamIdGameParentalLevel:
                    value = (uint)ParentalLevel.CellSysutilGameParentalOff;
                    break;

                case SystemParam.CellSysUtilSystemParamIdCurrentUserHasNpAccount:
                    value = 0;
                    break;

                default:
                    throw new Exception($"[CellSysutil] unknown param = 0x{param:X8}");
            }


            _memoryManager.Write<BigEndian<uint>>(value_ptr, value);
            return CellOk;
        }

        internal ulong CellSysutilGetSystemParamString(PpuThread ppu)
        {
            //TODO

            uint param = ppu.ReadArg<uint>(Arg0);
            GuestVoidPtr buf = ppu.ReadArg<GuestVoidPtr>(Arg1);
            int size = ppu.ReadArg<int>(Arg2);

            Console.WriteLine($"[CellSysutil] CellSysutilGetSystemParamString param = 0x{param:X8}");

            if (buf.IsNullPtr)
            {
                return CellSysutilErrorValue;
            }

            byte[] data = Encoding.UTF8.GetBytes("Snake\0");

            _memoryManager.ClearVirtualMemory(buf, size);
            _memoryManager.CopyBufferToVirtualMemory(data, buf);

            return CellOk;
        }

        internal ulong CellSaveDataUserAutoLoad(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellSaveDataUserAutoLoad");
            return CellOk;
        }

        internal ulong CellSaveDataAutoLoad2(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellSaveDataAutoLoad2");
            return CellOk;
        }

        internal ulong CellSaveDataAutoSave2(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellSaveDataAutoSave2");
            return CellOk;
        }

        internal ulong CellMsgDialogOpen2(PpuThread ppu)
        {
            //TODO: Implement actual dialog

            uint type = ppu.ReadArg<uint>(Arg0);
            GuestVoidPtr msg_ptr = ppu.ReadArg<GuestVoidPtr>(Arg1);
            GuestVoidPtr callback_ptr = ppu.ReadArg<GuestVoidPtr>(Arg2);
            GuestVoidPtr user_data = ppu.ReadArg<GuestVoidPtr>(Arg3);       //is this a pointer?
            uint param = ppu.ReadArg<uint>(Arg3);

            string msg = _memoryManager.ReadString(msg_ptr);
            Console.WriteLine($"[CellSysutil] cellMsgDialogOpen2: {msg}");

            // Callback - toc needed ?
            PpuInterpreter.ExecuteFunction(ppu, callback_ptr, toc: true, [1, user_data, 0, 0, 0]); // First arg = 1 for "Pressed OK"
            return CellOk;
        }

        internal ulong CellMsgDialogProgressBarSetMsg(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellMsgDialogProgressBarSetMsg");
            return CellOk;
        }

        internal ulong CellMsgDialogProgressBarInc(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellMsgDialogProgressBarInc");
            return CellOk;
        }

        internal ulong CellMsgDialogClose(PpuThread ppu)
        {
            //TODO
            Console.WriteLine($"[CellSysutil] cellMsgDialogClose");
            return CellOk;
        }

        internal ulong CellSaveDataEnableOverlay(PpuThread ppu)
        {
            Console.WriteLine($"[CellSysutil] cellSaveDataEnableOverlay");
            return CellOk;
        }
    }
}
