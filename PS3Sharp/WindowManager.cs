using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PS3Sharp.Cell.PPU.PPUInterpreter;

namespace PS3Sharp
{
    internal static class WindowManager
    {
        internal static bool IsInitialized { get; private set; }
        internal static GameWindow WindowInstance { get; private set; }
        private static Vector2i CurrentWindowSize;

        internal static bool IsRunning => WindowInstance != null && !WindowInstance.IsExiting && WindowInstance.Exists;

        internal static void InitializeWindow()
        {
            GLFWProvider.CheckForMainThread = false;
            NativeWindowSettings nativeSettings = new()
            {
                APIVersion = Version.Parse("4.6.0"),
                ClientSize = new Vector2i(1280, 720),
                Title = $"OpenGL | {PS3Sharp.SFO?.Title} [{PS3Sharp.SFO?.TitleID}]",
            };

            nativeSettings.Location = GetCenterOfScreen(nativeSettings.ClientSize);
            WindowInstance = new GameWindow(GameWindowSettings.Default, nativeSettings)
            {
                VSync = VSyncMode.Off,
                UpdateFrequency = 0
            };

            WindowInstance.MakeCurrent();

            IsInitialized = true;
        }

        internal static Vector2i GetCenterOfScreen(Vector2i size)
        {
            //Get screen resolution 
            VideoMode videoMode = Monitors.GetPrimaryMonitor().CurrentVideoMode;
            int width = videoMode.Width;
            int height = videoMode.Height;
            int newX = (width - size.X) / 2;
            int newY = (height - size.Y) / 2;
            return new Vector2i(newX, newY);
        }

        internal static JoystickState? GetJoystickState()
        {
            if (IsInitialized)
            {
                if (WindowInstance.JoystickStates != null)
                {
                    return WindowInstance.JoystickStates[0];
                }
            }

            return null;
        }

        internal static void ProcessEvents()
        {
            WindowInstance.ProcessEvents(0.0);
            if (WindowInstance.IsExiting)
            {
                Environment.Exit(0);
            }

            if (WindowInstance.Size != CurrentWindowSize)
            {
                CurrentWindowSize = WindowInstance.Size;
                GL.Viewport(0, 0, CurrentWindowSize.X, CurrentWindowSize.Y);
            }

            if (WindowInstance.IsKeyDown(Keys.L))
            {
                PPUInterpreter.Log = !PPUInterpreter.Log;
                Thread.Sleep(100);
                //Console.WriteLine("Enabled log");
            }
        }
    }
}
