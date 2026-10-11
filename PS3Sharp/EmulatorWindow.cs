using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.FileSystem;

namespace PS3Sharp
{
    internal class EmulatorWindow : IDisposable
    {
        internal GameWindow? GameWindow { get; private set; }
        internal GLRenderer? GLRenderer { get; private set; }
        internal bool IsWindowCreated => GameWindow != null;
        internal Func<JoystickState?> JoystickReader => GetJoystickState;

        private readonly ThreadManager _threadManager;
        private readonly FileSystemManager _fileSystemManager;
        private readonly object _windowLock = new();
        private Vector2i _currentWindowSize;

        internal EmulatorWindow(ThreadManager threadManager, FileSystemManager fileSystemManager)
        {
            _threadManager = threadManager;
            _fileSystemManager = fileSystemManager;
        }

        internal void CreateWindow()
        {
            // Note: the caller thread of this function must be the thread that calls GL (in this case RSX thread)

            lock (_windowLock)
            {
                if (IsWindowCreated)
                {
                    throw new InvalidOperationException($"[EmulatorWindow] Window is already created");
                }

                GLFWProvider.CheckForMainThread = false;
                string title = _fileSystemManager.GetCurrentTitleString();
                string titleID = _fileSystemManager.GetCurrentTitleID();
                NativeWindowSettings nativeSettings = CreateNativeWindowSettings(title, titleID);
                GameWindow = CreateGameWindow(VSyncMode.Off, 0, nativeSettings);
                GameWindow.MakeCurrent();
                GLRenderer = new(GameWindow);
            }
        }

        internal void ProcessEvents()
        {
            bool isShuttingDown = false;

            lock (_windowLock)
            {
                GameWindow!.ProcessEvents(0.0);

                isShuttingDown = GameWindow.IsExiting;

                if (GameWindow.Size != _currentWindowSize)
                {
                    _currentWindowSize = GameWindow.Size;
                    GL.Viewport(0, 0, _currentWindowSize.X, _currentWindowSize.Y);
                }

                if (GameWindow.IsKeyDown(Keys.L))
                {
                    PPUInterpreter.Log = !PPUInterpreter.Log;
                    Thread.Sleep(100);
                    // Console.WriteLine("Enabled log");
                }
            }

            // Important:
            // If we need to shutdown we must do the request after releasing the lock 
            if (isShuttingDown)
            {
                _threadManager.RequestShutdown();
            }
        }

        private JoystickState? GetJoystickState()
        {
            lock (_windowLock)
            {
                if (!IsWindowCreated)
                {
                    return null;
                }

                if (GameWindow!.JoystickStates != null)
                {
                    // Return a copy of the state
                    return GameWindow.JoystickStates[0]?.GetSnapshot();
                }

                return null;
            }
        }

        private static Vector2i GetCenterOfScreen(Vector2i size)
        {
            // Get screen resolution 
            VideoMode videoMode = Monitors.GetPrimaryMonitor().CurrentVideoMode;
            int width = videoMode.Width;
            int height = videoMode.Height;
            int newX = (width - size.X) / 2;
            int newY = (height - size.Y) / 2;
            return new Vector2i(newX, newY);
        }

        private static NativeWindowSettings CreateNativeWindowSettings(string title, string titleID)
        {
            Version version = Version.Parse("4.6.0");
            Vector2i size = new(1280, 720);
            Vector2i location = GetCenterOfScreen(size);

            string id = string.IsNullOrEmpty(titleID) ? string.Empty : $"[{titleID}]";
            string windowTitle = $"OpenGL | {title} {id}";

            NativeWindowSettings nativeSettings = new()
            {
                APIVersion = version,
                ClientSize = size,
                Location = location,
                Title = windowTitle,
            };

            return nativeSettings;
        }

        private static GameWindow CreateGameWindow(VSyncMode vsyncMode, double updateFrequency, NativeWindowSettings nativeSettings)
        {
            GameWindow gameWindow = new(GameWindowSettings.Default, nativeSettings)
            {
                VSync = vsyncMode,
                UpdateFrequency = updateFrequency
            };

            return gameWindow;
        }

        public void Dispose()
        {
            // TODO
            GLRenderer?.Dispose();
            GameWindow?.Dispose();
        }
    }
}
