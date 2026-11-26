using OpenTK.Windowing.GraphicsLibraryFramework;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.GuestMemory;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal sealed class CellPad
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CellPadInfo2 : IPS3Struct
        {
            internal BigEndian<uint> MaxConnect;
            internal BigEndian<uint> NowConnect;
            internal BigEndian<uint> SystemInfo;

            internal InlineData.InlineArray7<BigEndian<uint>> PortStatus;
            internal InlineData.InlineArray7<BigEndian<uint>> PortSetting;
            internal InlineData.InlineArray7<BigEndian<uint>> DeviceCapability;
            internal InlineData.InlineArray7<BigEndian<uint>> DeviceType;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellPadData : IPS3Struct
        {
            internal BigEndian<int> Len;
            internal InlineData.InlineArray64<BigEndian<ushort>> Buttons; // This probably stores the button states for all connected controllers
        }

        internal const uint CellPadErrorNoDevice = 0x80121107;
        internal const uint CellPadMaxPortNum = 7;
        internal const uint CellPadMaxCodes = 64;

        // Button bit positions
        // Index 2
        internal const uint CellPadCtrlSelect = 0;
        internal const uint CellPadCtrlL3 = 1;
        internal const uint CellPadCtrlR3 = 2;
        internal const uint CellPadCtrlStart = 3;
        internal const uint CellPadCtrlUp = 4;
        internal const uint CellPadCtrlRight = 5;
        internal const uint CellPadCtrlDown = 6;
        internal const uint CellPadCtrlLeft = 7;

        // Index 3
        internal const uint CellPadCtrlL2 = 0 + 16;
        internal const uint CellPadCtrlR2 = 1 + 16;
        internal const uint CellPadCtrlL1 = 2 + 16;
        internal const uint CellPadCtrlR1 = 3 + 16;
        internal const uint CellPadCtrlTriangle = 4 + 16;
        internal const uint CellPadCtrlCircle = 5 + 16;
        internal const uint CellPadCtrlCross = 6 + 16;
        internal const uint CellPadCtrlSquare = 7 + 16;

        // For XInput:
        // Left X, Left Y, Right X, Right Y
        private static ReadOnlySpan<int> XInputAxis => [0, 1, 2, 3];

        private static readonly ImmutableDictionary<uint, uint> XInputDictionary = new Dictionary<uint, uint>()
        {
               {0, CellPadCtrlCross},       // X
               {1, CellPadCtrlCircle},      // Circle
               {2, CellPadCtrlSquare},      // Square
               {3, CellPadCtrlTriangle},    // Triangle
               {4, CellPadCtrlL1},          // L1
               {5, CellPadCtrlR1},          // R1
               {6, CellPadCtrlSelect},      // Select
               {7, CellPadCtrlStart},       // Start
               {8, CellPadCtrlL3},          // L3
               {9, CellPadCtrlR3},          // R3
               {10, CellPadCtrlUp},         // Pad up
               {11, CellPadCtrlRight},      // Pad right
               {12, CellPadCtrlDown},       // Pad down
               {13, CellPadCtrlLeft},       // Pad Left

               // L2 is Axis 4
               // R2 is Axis 5 
        }.ToImmutableDictionary();

        private readonly MemoryManager _memoryManager;
        private readonly Func<JoystickState?> _joyStickReader;

        internal CellPad(MemoryManager memoryManager, Func<JoystickState?> joyStickReader)
        {
            _memoryManager = memoryManager;
            _joyStickReader = joyStickReader;
        }

        internal ulong CellPadInit(PPUThread ppu)
        {
            //Console.WriteLine($"[CellPad] cellPadInit");
            return CellOk;
        }

        internal ulong CellPadSetPortSetting(PPUThread ppu)
        {
            //Console.WriteLine($"[CellPad] cellPadSetPortSetting");
            return CellOk;
        }

        internal ulong CellPadGetData(PPUThread ppu)
        {
            uint port = ppu.ReadArg<uint>(Arg0);
            GuestPtr<CellPadData> data_ptr = ppu.ReadArg<GuestPtr<CellPadData>>(Arg1);

            if (port != 0 || _joyStickReader() == null)
            {
                return CellPadErrorNoDevice;
            }

            CellPadData data = new()
            {
                Len = 24
            };

            WriteButtons(ref data);

            //..??
            data.Buttons[0] = 0;
            int v = ((data.Len / 2) & 0xf) | (7 << 4);
            data.Buttons[1] = (ushort)v;

            _memoryManager.Write(data_ptr, data);
            return CellOk;
        }

        internal ulong CellPadGetInfo(PPUThread ppu)
        {
            return CellOk;
        }

        internal ulong CellPadGetInfo2(PPUThread ppu)
        {
            GuestPtr<CellPadInfo2> info_ptr = ppu.ReadArg<GuestPtr<CellPadInfo2>>(Arg0);
            uint nowConnected = _joyStickReader() == null ? 0U : 1U;

            CellPadInfo2 info = new()
            {
                MaxConnect = CellPadMaxPortNum,
                NowConnect = nowConnected, // Must set to 1 if we are emulating a controller
                SystemInfo = 0
            };

            for (int i = 0; i < CellPadMaxPortNum; i++)
            {
                info.PortStatus[i] = 0; //Not connected
                info.PortSetting[i] = 0;
                info.DeviceCapability[i] = 0;
                info.DeviceType[i] = 0;
            }

            if (nowConnected == 1)
            {
                // Set the state of 1 controller
                info.PortStatus[0] = 1;             // Connected
                info.PortSetting[0] = 0x6;
                info.DeviceCapability[0] = 0x1F;    // Standard
                info.DeviceType[0] = 0;
            }

            _memoryManager.Write(info_ptr, info);
            return CellOk;
        }

        private void WriteButtons(ref CellPadData cellPadData)
        {
            // Loop over the controller's buttons, write to cellPadData

            JoystickState? controllerState = _joyStickReader();

            if (controllerState == null)
            {
                return;
            }

            for (uint i = 0; i < controllerState.ButtonCount; i++)
            {
                if (XInputDictionary.TryGetValue(i, out uint value))
                {
                    bool pressed = controllerState.IsButtonDown((int)i);
                    SetButton(value, pressed, ref cellPadData);
                }
            }

            // Get L2 and R2
            (bool l2_pressed, bool r2_pressed) = GetL2R2(controllerState);
            SetButton(CellPadCtrlL2, l2_pressed, ref cellPadData);
            SetButton(CellPadCtrlR2, r2_pressed, ref cellPadData);

            // Write Left/Right stick positions
            (byte leftX, byte leftY, byte rightX, byte rightY) = GetStickPosition(controllerState, XInputAxis);

            cellPadData.Buttons[4] = rightX;
            cellPadData.Buttons[5] = rightY;
            cellPadData.Buttons[6] = leftX;
            cellPadData.Buttons[7] = leftY;
        }

        private void SetButton(uint buttonBit, bool isPressed, ref CellPadData cellPadData)
        {
            // Buttons are stored in index 2 and 3
            bool isIndex3 = buttonBit >= 16;
            int idx = 2;

            if (isIndex3)
            {
                idx = 3;
                buttonBit -= 16;
            }

            ushort currentState = cellPadData.Buttons[idx];
            int bitValue = 1 << (int)buttonBit;

            if (isPressed)
            {
                // Set 
                currentState |= (ushort)(bitValue);
            }
            else
            {
                // Clear
                currentState &= (ushort)(~bitValue);
            }

            cellPadData.Buttons[idx] = currentState;
        }

        private (bool, bool) GetL2R2(JoystickState controller)
        {
            float l2 = controller.GetAxis(4);
            float r2 = controller.GetAxis(5);

            // PS3's L2/R2 are not triggers
            // just report pressed for any level > -1
            return (l2 > -1.0f, r2 > -1.0f);
        }

        private (byte, byte, byte, byte) GetStickPosition(JoystickState controller, ReadOnlySpan<int> axis)
        {
            float leftX = controller.GetAxis(axis[0]);
            float leftY = controller.GetAxis(axis[1]);
            float rightX = controller.GetAxis(axis[2]);
            float rightY = controller.GetAxis(axis[3]);

            byte leftX_normalized = NormalizeAxis(leftX);
            byte leftY_normalized = NormalizeAxis(leftY);

            byte rightX_normalized = NormalizeAxis(rightX);
            byte rightY_normalized = NormalizeAxis(rightY);

            return (leftX_normalized, leftY_normalized, rightX_normalized, rightY_normalized);
        }

        private byte NormalizeAxis(float value)
        {
            // Convert [-1, 1] to [0, 255]
            return (byte)((value + 1f) * 0.5f * 0xFF);
        }
    }
}
