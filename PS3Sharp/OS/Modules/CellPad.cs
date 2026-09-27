using OpenTK.Windowing.GraphicsLibraryFramework;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class CellPad
    {
        public const uint CELL_PAD_ERROR_NO_DEVICE = 0x80121107;
        public const uint CELL_PAD_MAX_PORT_NUM = 7;
        public const uint CELL_PAD_MAX_CODES = 64;

        // Button bit positions
        // Index 2
        public const uint CELL_PAD_CTRL_SELECT = 0;
        public const uint CELL_PAD_CTRL_L3 = 1;
        public const uint CELL_PAD_CTRL_R3 = 2;
        public const uint CELL_PAD_CTRL_START = 3;
        public const uint CELL_PAD_CTRL_UP = 4;
        public const uint CELL_PAD_CTRL_RIGHT = 5;
        public const uint CELL_PAD_CTRL_DOWN = 6;
        public const uint CELL_PAD_CTRL_LEFT = 7;

        // Index 3
        public const uint CELL_PAD_CTRL_L2 = 0 + 16;
        public const uint CELL_PAD_CTRL_R2 = 1 + 16;
        public const uint CELL_PAD_CTRL_L1 = 2 + 16;
        public const uint CELL_PAD_CTRL_R1 = 3 + 16;
        public const uint CELL_PAD_CTRL_TRIANGLE = 4 + 16;
        public const uint CELL_PAD_CTRL_CIRCLE = 5 + 16;
        public const uint CELL_PAD_CTRL_CROSS = 6 + 16;
        public const uint CELL_PAD_CTRL_SQUARE = 7 + 16;

        [StructLayout(LayoutKind.Sequential)]
        public struct CellPadInfo2 : IPS3Struct
        {
            public BigEndian<uint> MaxConnect;
            public BigEndian<uint> NowConnect;
            public BigEndian<uint> SystemInfo;

            public InlineData.InlineArray7<BigEndian<uint>> PortStatus;
            public InlineData.InlineArray7<BigEndian<uint>> PortSetting;
            public InlineData.InlineArray7<BigEndian<uint>> DeviceCapability;
            public InlineData.InlineArray7<BigEndian<uint>> DeviceType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CellPadData : IPS3Struct
        {
            public BigEndian<int> Len;
            public InlineData.InlineArray64<BigEndian<ushort>> Buttons; // This probably stores the button states for all connected controllers
        }

        public static ulong CellPadInit(PPU ppu)
        {
            //Console.WriteLine($"[CellPad] cellPadInit");
            return CELL_OK;
        }

        public static ulong CellPadSetPortSetting(PPU ppu)
        {
            //Console.WriteLine($"[CellPad] cellPadSetPortSetting");
            return CELL_OK;
        }

        public static ulong CellPadGetData(PPU ppu)
        {
            uint port = ppu.ReadArg<uint>(ARG0);
            GuestPtr<CellPadData> data_ptr = ppu.ReadArg<GuestPtr<CellPadData>>(ARG1);

            if (port != 0 || WindowManager.GetJoystickState() == null)
            {
                return CELL_PAD_ERROR_NO_DEVICE;
            }

            unsafe
            {
                CellPadData* data = data_ptr.AsNativePtr();
                data->Len = 24;

                WriteButtons(data_ptr);

                //..??
                data->Buttons[0] = 0;
                int v = ((data->Len / 2) & 0xf) | (7 << 4);
                data->Buttons[1] = (ushort)v;
            }


            return CELL_OK;
        }

        public static ulong CellPadGetInfo(PPU ppu)
        {
            return CELL_OK;
        }


        public static ulong CellPadGetInfo2(PPU ppu)
        {
            GuestPtr<CellPadInfo2> info_ptr = ppu.ReadArg<GuestPtr<CellPadInfo2>>(ARG0);
            uint nowConnected = WindowManager.GetJoystickState() == null ? 0U : 1U;

            unsafe
            {
                CellPadInfo2* info = info_ptr.AsNativePtr();
                info->MaxConnect = CELL_PAD_MAX_PORT_NUM;
                info->NowConnect = nowConnected; // Must set to 1 if we are emulating a controller
                info->SystemInfo = 0;

                for (int i = 0; i < CELL_PAD_MAX_PORT_NUM; i++)
                {
                    info->PortStatus[i] = 0; //Not connected
                    info->PortSetting[i] = 0;
                    info->DeviceCapability[i] = 0;
                    info->DeviceType[i] = 0;
                }

                if (nowConnected == 1)
                {
                    // Set the state of 1 controller
                    info->PortStatus[0] = 1;             // Connected
                    info->PortSetting[0] = 0x6;
                    info->DeviceCapability[0] = 0x1F;    // Standard
                    info->DeviceType[0] = 0;
                }
            }

            //  Console.WriteLine($"[CellPad] cellPadGetInfo2");
            return CELL_OK;
        }

        private static unsafe void WriteButtons(GuestPtr<CellPadData> cellPadData_ptr)
        {
            // Loop over the controller's buttons, write to cellPadData

            JoystickState? controllerState = WindowManager.GetJoystickState();

            if (controllerState == null)
            {
                return;
            }

            for (uint i = 0; i < controllerState.ButtonCount; i++)
            {
                if (XInputDictionary.TryGetValue(i, out uint value))
                {
                    bool pressed = controllerState.IsButtonDown((int)i);
                    SetButton(value, pressed, cellPadData_ptr);
                }
            }

            // Get L2 and R2
            (bool l2_pressed, bool r2_pressed) = GetL2R2(controllerState);
            SetButton(CELL_PAD_CTRL_L2, l2_pressed, cellPadData_ptr);
            SetButton(CELL_PAD_CTRL_R2, r2_pressed, cellPadData_ptr);

            // Write Left/Right stick positions
            (byte leftX, byte leftY, byte rightX, byte rightY) = GetStickPosition(controllerState, XInputAxis);

            unsafe
            {
                CellPadData* cellPadData = cellPadData_ptr.AsNativePtr();
                cellPadData->Buttons[4] = rightX;
                cellPadData->Buttons[5] = rightY;
                cellPadData->Buttons[6] = leftX;
                cellPadData->Buttons[7] = leftY;
            }
        }

        private static void SetButton(uint buttonBit, bool isPressed, GuestPtr<CellPadData> cellPadData_ptr)
        {
            // Buttons are stored in index 2 and 3
            bool isIndex3 = buttonBit >= 16;
            int idx = 2;

            if (isIndex3)
            {
                idx = 3;
                buttonBit -= 16;
            }

            unsafe
            {
                CellPadData* cellPadData = cellPadData_ptr.AsNativePtr();
                ushort currentState = cellPadData->Buttons[idx];
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

                cellPadData->Buttons[idx] = currentState;
            }
        }

        private static (bool, bool) GetL2R2(JoystickState controller)
        {
            float l2 = controller.GetAxis(4);
            float r2 = controller.GetAxis(5);

            // PS3's L2/R2 are not triggers
            // just report pressed for any level > -1
            return (l2 > -1.0f, r2 > -1.0f);
        }

        private static (byte, byte, byte, byte) GetStickPosition(JoystickState controller, int[] axis)
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

        private static byte NormalizeAxis(float value)
        {
            // Convert [-1, 1] to [0, 255]
            return (byte)((value + 1f) * 0.5f * 0xFF);
        }


        // For XInput:
        // Left X, Left Y, Right X, Right Y
        private static readonly int[] XInputAxis = [0, 1, 2, 3];

        private static readonly Dictionary<uint, uint> XInputDictionary = new() {
               {0, CELL_PAD_CTRL_CROSS},       // X
               {1, CELL_PAD_CTRL_CIRCLE},      // Circle
               {2, CELL_PAD_CTRL_SQUARE},      // Square
               {3, CELL_PAD_CTRL_TRIANGLE},    // Triangle
               {4, CELL_PAD_CTRL_L1},          // L1
               {5, CELL_PAD_CTRL_R1},          // R1
               {6, CELL_PAD_CTRL_SELECT},      // Select
               {7, CELL_PAD_CTRL_START},       // Start
               {8, CELL_PAD_CTRL_L3},          // L3
               {9, CELL_PAD_CTRL_R3},          // R3
               {10, CELL_PAD_CTRL_UP},         // Pad up
               {11, CELL_PAD_CTRL_RIGHT},      // Pad right
               {12, CELL_PAD_CTRL_DOWN},       // Pad down
               {13, CELL_PAD_CTRL_LEFT},       // Pad Left

               // L2 is Axis 4
               // R2 is Axis 5 
            };
    }
}
