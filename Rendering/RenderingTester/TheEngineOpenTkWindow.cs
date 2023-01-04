using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Input;
using JetBrains.Profiler.Api;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TheAvaloniaOpenGL;
using TheEngine.Config;
using TheEngine.Utils;
using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using TextInputEventArgs = OpenTK.Windowing.Common.TextInputEventArgs;

namespace TheEngine;

public class TheEngineOpenTkWindow : GameWindow, IWindowHost
{
    private readonly IGame game;
    private Engine engine = null!;
    private Stopwatch updateStopwatch = new();
    private Stopwatch renderStopwatch = new Stopwatch();
    private bool isMacOS;

    public TheEngineOpenTkWindow(GameWindowSettings gameWindowSettings,
        NativeWindowSettings nativeWindowSettings,
        IGame game) : base(gameWindowSettings, nativeWindowSettings)
    {
        isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        this.game = game;
    }

    protected override void OnLoad()
    {
        IDevice device = new OpenTKDevice();
#if DEBUG && DEBUG_OPENGL
        device = new DebugDevice(device);
#endif
        engine = new Engine(device, new Configuration(), this, false);
        game.Initialize(engine);
        base.OnLoad();
    }

    protected override void OnUnload()
    {
        game.DisposeGame();
        engine.Dispose();
        base.OnUnload();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        engine.TotalTime += args.Time * 1000;
        VSync = VSyncMode.Off;
        engine.statsManager.Counters.FrameTime.Add(args.Time * 1000);
        renderStopwatch.Restart();
        engine.Device.device.Begin();
        engine.renderManager.BeginFrame();
        engine.renderManager.PrepareRendering(0);
        engine.renderManager.RenderOpaque(0);
        engine.Device.device.Debug("  Rendering Game custom");
        game.Render((float)args.Time * 1000);
        engine.renderManager.RenderTransparent(0);
        engine.Device.device.Debug("  Rendering Game custom translucent");
        game.RenderTransparent((float)args.Time * 1000);
        engine.renderManager.RenderPostProcess();
        engine.Device.device.Debug("  Rendering Game custom GUI");
        game.RenderGUI((float)args.Time * 1000);
        engine.RenderGUI();
        engine.Device.device.Debug("  Finalize rendering");
        engine.renderManager.FinalizeRendering(0);
        base.OnRenderFrame(args);
        renderStopwatch.Stop();
        engine.statsManager.Counters.TotalRender.Add(renderStopwatch.Elapsed.Milliseconds);
        renderStopwatch.Restart();
        GL.Finish();
        SwapBuffers();
        renderStopwatch.Stop();
        engine.statsManager.Counters.PresentTime.Add(renderStopwatch.Elapsed.Milliseconds);
        if (stopMeasure)
        {
            MeasureProfiler.StopCollectingData();
            MeasureProfiler.SaveData();
            measure = false;
            stopMeasure = false;
        }
    }

    private KeyboardState? previousState;

    private Dictionary<Keys, Key> KeyMapping = new()
    {
        { Keys.Space, Key.Space },
        //{ Keys.Apostrophe, Key.Apostrophe },
        { Keys.Comma, Key.OemComma },
        { Keys.Minus, Key.OemMinus },
        { Keys.Period, Key.OemPeriod },
        { Keys.Slash, Key.DbeNoCodeInput },
        { Keys.D0, Key.D0 },
        { Keys.D1, Key.D1 },
        { Keys.D2, Key.D2 },
        { Keys.D3, Key.D3 },
        { Keys.D4, Key.D4 },
        { Keys.D5, Key.D5 },
        { Keys.D6, Key.D6 },
        { Keys.D7, Key.D7 },
        { Keys.D8, Key.D8 },
        { Keys.D9, Key.D9 },
        { Keys.Semicolon, Key.OemSemicolon },
        { Keys.Equal, Key.OemPlus },
        { Keys.A, Key.A },
        { Keys.B, Key.B },
        { Keys.C, Key.C },
        { Keys.D, Key.D },
        { Keys.E, Key.E },
        { Keys.F, Key.F },
        { Keys.G, Key.G },
        { Keys.H, Key.H },
        { Keys.I, Key.I },
        { Keys.J, Key.J },
        { Keys.K, Key.K },
        { Keys.L, Key.L },
        { Keys.M, Key.M },
        { Keys.N, Key.N },
        { Keys.O, Key.O },
        { Keys.P, Key.P },
        { Keys.Q, Key.Q },
        { Keys.R, Key.R },
        { Keys.S, Key.S },
        { Keys.T, Key.T },
        { Keys.U, Key.U },
        { Keys.V, Key.V },
        { Keys.W, Key.W },
        { Keys.X, Key.X },
        { Keys.Y, Key.Y },
        { Keys.Z, Key.Z },
        { Keys.LeftBracket, Key.OemOpenBrackets },
        { Keys.Backslash, Key.OemBackslash },
        { Keys.RightBracket, Key.OemCloseBrackets },
        { Keys.GraveAccent, Key.DbeNoCodeInput },
        { Keys.Escape, Key.Escape },
        { Keys.Enter, Key.Enter },
        { Keys.Tab, Key.Tab },
        { Keys.Backspace, Key.Back },
        { Keys.Insert, Key.Insert },
        { Keys.Delete, Key.Delete },
        { Keys.Right, Key.Right },
        { Keys.Left, Key.Left },
        { Keys.Down, Key.Down },
        { Keys.Up, Key.Up },
        { Keys.PageUp, Key.PageUp },
        { Keys.PageDown, Key.PageDown },
        { Keys.Home, Key.Home },
        { Keys.End, Key.End },
        { Keys.CapsLock, Key.CapsLock },
        { Keys.ScrollLock, Key.Scroll },
        { Keys.NumLock, Key.NumLock },
        { Keys.PrintScreen, Key.PrintScreen },
        { Keys.Pause, Key.Pause },
        { Keys.F1, Key.F1 },
        { Keys.F2, Key.F2 },
        { Keys.F3, Key.F3 },
        { Keys.F4, Key.F4 },
        { Keys.F5, Key.F5 },
        { Keys.F6, Key.F6 },
        { Keys.F7, Key.F7 },
        { Keys.F8, Key.F8 },
        { Keys.F9, Key.F9 },
        { Keys.F10, Key.F10 },
        { Keys.F11, Key.F11 },
        { Keys.F12, Key.F12 },
        { Keys.F13, Key.F13 },
        { Keys.F14, Key.F14 },
        { Keys.F15, Key.F15 },
        { Keys.F16, Key.F16 },
        { Keys.F17, Key.F17 },
        { Keys.F18, Key.F18 },
        { Keys.F19, Key.F19 },
        { Keys.F20, Key.F20 },
        { Keys.F21, Key.F21 },
        { Keys.F22, Key.F22 },
        { Keys.F23, Key.F23 },
        { Keys.F24, Key.F24 },
        { Keys.F25, Key.F24 },
        { Keys.KeyPad0, Key.NumPad0 },
        { Keys.KeyPad1, Key.NumPad1 },
        { Keys.KeyPad2, Key.NumPad2 },
        { Keys.KeyPad3, Key.NumPad3 },
        { Keys.KeyPad4, Key.NumPad4 },
        { Keys.KeyPad5, Key.NumPad5 },
        { Keys.KeyPad6, Key.NumPad6 },
        { Keys.KeyPad7, Key.NumPad7 },
        { Keys.KeyPad8, Key.NumPad8 },
        { Keys.KeyPad9, Key.NumPad9 },
        { Keys.KeyPadDecimal, Key.Decimal },
        { Keys.KeyPadDivide, Key.Divide },
        { Keys.KeyPadMultiply, Key.Multiply },
        { Keys.KeyPadSubtract, Key.Subtract },
        { Keys.KeyPadAdd, Key.Add },
        { Keys.KeyPadEnter, Key.ImeAccept },
        { Keys.KeyPadEqual, Key.DbeNoCodeInput },
        { Keys.LeftShift, Key.LeftShift },
        { Keys.LeftControl, Key.LeftCtrl },
        { Keys.LeftAlt, Key.LeftAlt },
        { Keys.LeftSuper, Key.DbeNoCodeInput },
        { Keys.RightShift, Key.RightShift },
        { Keys.RightControl, Key.RightCtrl },
        { Keys.RightAlt, Key.RightAlt },
        { Keys.RightSuper, Key.DbeNoCodeInput },
        { Keys.Menu, Key.DbeNoCodeInput },
    };

    private bool measure;
    private bool stopMeasure;
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (measure && engine.inputManager.keyboard.JustReleased(Key.H))
            stopMeasure = true;
        if (engine.inputManager.keyboard.JustPressed(Key.H))
        {
            MeasureProfiler.StartCollectingData();
            measure = true;
            if (!engine.inputManager.keyboard.IsDown(Key.LeftCtrl))
                stopMeasure = true;
        }
        updateStopwatch.Restart();
        engine.inputManager.PostUpdate();
        engine.inputManager.Update((float)args.Time * 1000);
        UpdateKeyboard();
        UpdateMouse();
        
        if (engine.inputManager.Keyboard.JustPressed(Key.R))
        {
            if (engine.Device.device is DebugDevice debug)
            {
                var file = new FileInfo("render_debug.txt");
                File.WriteAllLines(file.FullName, debug.commands);
                Console.WriteLine("Log written to " + file.FullName);
            }
        }
        
        engine.UpdateGui((float)args.Time);
        game?.Update((float)args.Time * 1000);
        engine.renderManager.UpdateTransforms();
            
        base.OnUpdateFrame(args);
        updateStopwatch.Stop();
        engine.statsManager.Counters.UpdateTime.Add(updateStopwatch.Elapsed.TotalMilliseconds);
    }

    private bool wasLeftDown = false;
    private bool wasRightDown = false;
    private void UpdateMouse()
    {
        mouseState = MouseState;
        var isLeftDown = mouseState.IsButtonDown(MouseButton.Left);
        var isRightDown = mouseState.IsButtonDown(MouseButton.Right);
        if (isLeftDown && !wasLeftDown)
            engine.inputManager.mouse.MouseDown(Input.MouseButton.Left);
        if (!isLeftDown && wasLeftDown)
            engine.inputManager.mouse.MouseUp(isRightDown ? Input.MouseButton.Right : Input.MouseButton.None);
        
        if (isRightDown && !wasRightDown)
            engine.inputManager.mouse.MouseDown(Input.MouseButton.Right);
        if (!isRightDown && wasRightDown)
            engine.inputManager.mouse.MouseUp(isLeftDown ? Input.MouseButton.Left : Input.MouseButton.None);
        
        engine.inputManager.mouse.MouseWheel(new Vector2(mouseState.ScrollDelta.X, mouseState.ScrollDelta.Y));

        if (isMacOS)
            engine.inputManager.mouse.PointerMoved(mouseState.Position.X, mouseState.Position.Y, WindowWidth / DpiScaling, WindowHeight / DpiScaling);
        else
            engine.inputManager.mouse.PointerMoved(mouseState.Position.X, mouseState.Position.Y, WindowWidth, WindowHeight);

        wasLeftDown = isLeftDown;
        wasRightDown = isRightDown;
    }

    private static Keys[] StaticCachedKeys = Enum.GetValues<Keys>();
    private List<char> pressedKeys = new();
    private MouseState mouseState = null!;

    private void UpdateKeyboard()
    {
        var keys = KeyboardState.GetSnapshot();
        if (previousState != null)
        {
            foreach (var key in StaticCachedKeys)
            {
                if (key == Keys.Unknown)
                    continue;
                
                var isDown = keys.IsKeyDown(key);
                var wasDown = previousState.IsKeyDown(key);
                if (!wasDown && isDown && KeyMapping.TryGetValue(key, out var avaKey))
                    engine.inputManager.keyboard.KeyDown(avaKey);
                else if (wasDown && !isDown && KeyMapping.TryGetValue(key, out avaKey))
                    engine.inputManager.keyboard.KeyUp(avaKey);
            }
        }
        
        for (int i = 0; i < pressedKeys.Count; i++)
        {
            char c = pressedKeys[i];
            engine.inputManager.keyboard.OnTextInput(c);
        }
        pressedKeys.Clear();
        previousState = keys;
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        pressedKeys.Add((char)e.Unicode);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        TryGetCurrentMonitorScale(out var scaleX, out var scaleY);
        DpiScaling = scaleX;
        // for some reason, Windows gives different measurements than macOS
        // macOS provides "scaled" values, while Windows provides "unscaled" values
        // i.e. for x2 scaling and 3000x1500 window, macOS gives 1500x750, while Windows gives 3000x1500
        if (isMacOS)
        {
            WindowWidth = e.Width * scaleX;
            WindowHeight = e.Height * scaleY;
        }
        else
        {
            WindowWidth = e.Width;
            WindowHeight = e.Height;
        }
        base.OnResize(e);   
    }

    public float WindowWidth { get; private set; }
    public float WindowHeight { get; private set; }
    public float DpiScaling { get; private set; }
}