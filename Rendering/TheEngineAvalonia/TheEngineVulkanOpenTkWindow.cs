using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Vulkan;
using TheEngine;
using TheEngine.Config;
using TheEngine.Input;
using TheEngine.Vulkan;
using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using TextInputEventArgs = OpenTK.Windowing.Common.TextInputEventArgs;

namespace TheEngine;

/// <summary>
/// Standalone Vulkan window host: a NoAPI GLFW window (OpenTK NativeWindow) with a manual
/// game loop. GLFW only provides the surface; presentation is the Vulkan backend's job.
/// </summary>
public class TheEngineVulkanOpenTkWindow : NativeWindow, IWindowHost
{
    private readonly IGame game;
    private Engine engine = null!;
    private GameRunner gameRunner = null!;
    private readonly bool isMacOS;

    public TheEngineVulkanOpenTkWindow(NativeWindowSettings nativeWindowSettings, IGame game)
        : base(nativeWindowSettings)
    {
        isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        this.game = game;
        UpdateSizes(true);
    }

    private void UpdateSizes(bool getDpi = false)
    {
        // the engine works in physical pixels (the GL host multiplied logical size by the
        // monitor scale on macOS); the framebuffer size is exactly that
        WindowWidth = FramebufferSize.X;
        WindowHeight = FramebufferSize.Y;
        if (getDpi)
        {
            // this is expensive so only do it once
            TryGetCurrentMonitorScale(out var scaleX, out _);
            DpiScaling = scaleX;
        }
    }

    public unsafe void Run()
    {
        var ctx = new VulkanContext();
        GLFW.InitVulkanLoader(ctx.LoaderVkGetInstanceProcAddr);
        ctx.CreateInstance(GLFW.GetRequiredInstanceExtensions());

        VulkanContext.Check(
            (Result)GLFW.CreateWindowSurface(new VkHandle((nint)ctx.Instance.Handle), WindowPtr, null, out var surfaceHandle),
            "glfwCreateWindowSurface");
        var surface = new SurfaceKHR((ulong)surfaceHandle.Handle);

        ctx.PickDeviceAndCreate(surface);
        Console.WriteLine($"Vulkan device: {ctx.DeviceName}");

        var backend = new VulkanRenderBackend(ctx, surface, this);
        // native top-down memory: the final swapchain blit no longer needs to flip
        engine = new Engine(backend, new Configuration(), this, false);
        gameRunner = new GameRunner(engine);
        // events are pumped here, inside the frame, not at the top of the loop: SyncInputState runs
        // after BeginFrame's blocking fence/present waits, so polling now (instead of before the
        // block) picks up input that arrived during the wait - up to a full vsync fresher
        gameRunner.SyncInputState += () =>
        {
            NewInputFrame();
            ProcessWindowEvents(false);
            UpdateSizes();
            UpdateKeyboard();
            UpdateMouse();
        };

        var stopwatch = Stopwatch.StartNew();
        double previous = 0;
        while (!GLFW.WindowShouldClose(WindowPtr))
        {
            var now = stopwatch.Elapsed.TotalSeconds;
            var delta = (float)(now - previous);
            previous = now;
            if (!gameRunner.NextFrame(delta, game))
            {
                break;
            }
        }

        game.DisposeGame();
        engine.Dispose();
    }

    private Dictionary<Keys, Key> KeyMapping = new()
    {
        { Keys.Space, Key.Space },
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

    private bool pendingLeftMouseDown;
    private bool pendingRightMouseDown;
    private bool pendingMiddleMouseDown;
    private bool pendingLeftMouseUp;
    private bool pendingRightMouseUp;
    private bool pendingMiddleMouseUp;
    private MouseState mouseState = null!;

    private void UpdateMouse()
    {
        mouseState = MouseState;
        var isLeftDown = mouseState.IsButtonDown(MouseButton.Left);
        var isRightDown = mouseState.IsButtonDown(MouseButton.Right);
        var isMiddleDown = mouseState.IsButtonDown(MouseButton.Middle);
        // MouseUp takes the full still-held button state (that's the engine protocol)
        var stillDown = (isLeftDown ? Input.MouseButton.Left : Input.MouseButton.None) |
                        (isRightDown ? Input.MouseButton.Right : Input.MouseButton.None) |
                        (isMiddleDown ? Input.MouseButton.Middle : Input.MouseButton.None);
        if (pendingLeftMouseDown)
        {
            engine.inputManager.mouse.MouseDown(Input.MouseButton.Left);
            pendingLeftMouseDown = false;
        }
        else if (pendingLeftMouseUp)
        {
            engine.inputManager.mouse.MouseUp(stillDown & ~Input.MouseButton.Left);
            pendingLeftMouseUp = false;
        }

        if (pendingRightMouseDown)
        {
            engine.inputManager.mouse.MouseDown(Input.MouseButton.Right);
            pendingRightMouseDown = false;
        }
        else if (pendingRightMouseUp)
        {
            engine.inputManager.mouse.MouseUp(stillDown & ~Input.MouseButton.Right);
            pendingRightMouseUp = false;
        }

        if (pendingMiddleMouseDown)
        {
            engine.inputManager.mouse.MouseDown(Input.MouseButton.Middle);
            pendingMiddleMouseDown = false;
        }
        else if (pendingMiddleMouseUp)
        {
            engine.inputManager.mouse.MouseUp(stillDown & ~Input.MouseButton.Middle);
            pendingMiddleMouseUp = false;
        }

        engine.inputManager.mouse.MouseWheel(new Vector2(mouseState.ScrollDelta.X, mouseState.ScrollDelta.Y));

        if (isMacOS)
            engine.inputManager.mouse.PointerMoved(mouseState.Position.X, mouseState.Position.Y, WindowWidth / DpiScaling, WindowHeight / DpiScaling);
        else
            engine.inputManager.mouse.PointerMoved(mouseState.Position.X, mouseState.Position.Y, WindowWidth, WindowHeight);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButton.Left)
            pendingLeftMouseDown = true;
        else if (e.Button == MouseButton.Right)
            pendingRightMouseDown = true;
        else if (e.Button == MouseButton.Middle)
            pendingMiddleMouseDown = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButton.Left)
            pendingLeftMouseUp = true;
        else if (e.Button == MouseButton.Right)
            pendingRightMouseUp = true;
        else if (e.Button == MouseButton.Middle)
            pendingMiddleMouseUp = true;
    }

    private static Keys[] StaticCachedKeys = Enum.GetValues<Keys>();
    private List<char> pressedKeys = new();

    private void UpdateKeyboard()
    {
        foreach (var key in StaticCachedKeys)
        {
            if (key == Keys.Unknown)
                continue;

            if (KeyboardState.IsKeyPressed(key) && KeyMapping.TryGetValue(key, out var avaKey))
                engine.inputManager.keyboard.KeyDown(avaKey);
            else if (KeyboardState.IsKeyReleased(key) && KeyMapping.TryGetValue(key, out avaKey))
                engine.inputManager.keyboard.KeyUp(avaKey);
        }

        for (int i = 0; i < pressedKeys.Count; i++)
            engine.inputManager.keyboard.OnTextInput(pressedKeys[i]);
        pressedKeys.Clear();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        pressedKeys.Add((char)e.Unicode);
    }

    public float WindowWidth { get; private set; }
    public float WindowHeight { get; private set; }
    public float DpiScaling { get; private set; }
}
