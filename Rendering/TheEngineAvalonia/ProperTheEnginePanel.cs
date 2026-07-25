using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using GpuInterop;
using TheEngine;
using TheEngine.Config;
using TheEngine.Utils;
using TheEngine.Vulkan;
using MouseButton = TheEngine.Input.MouseButton;
using Point = Avalonia.Point;
using InteropVulkanContext = GpuInterop.VulkanDemo.VulkanContext;
using InteropVulkanSwapchain = GpuInterop.VulkanDemo.VulkanSwapchain;
using EngineVulkanContext = TheEngine.Vulkan.VulkanContext;

namespace TheEngine;

/// <summary>
/// Hosts <see cref="IGame"/> on TheEngine, rendering through Avalonia's composition GPU interop: the
/// engine renders into an offscreen Vulkan image that is handed to the compositor (no native child
/// window, no VK_KHR_surface swapchain). This is the working replacement for <c>NativeTheEnginePanel</c>.
///
/// Wiring: the copied GpuInterop <see cref="InteropVulkanContext"/> creates the Vulkan instance/device
/// matched to the compositor's GPU (with the engine's required 1.3 features merged in). The engine's
/// <see cref="EngineVulkanContext"/> adopts that device and a <see cref="VulkanRenderBackend"/> drives
/// it with an <see cref="ExternalImagePresentTarget"/>. Each frame the interop swapchain hands out an
/// exportable image, the engine renders its final pass into it, and the swapchain presents it to the
/// compositor. Input plumbing mirrors NativeTheEnginePanel.
/// </summary>
public class ProperTheEnginePanel : DrawingSurfaceDemoBase, IWindowHost
{
    private Engine? engine;
    private GameRunner? gameRunner;
    private InteropVulkanContext? interopCtx;
    private EngineVulkanContext? engineCtx;
    private InteropVulkanSwapchain? interopSwapchain;
    private ExternalImagePresentTarget externalTarget = new();

    private readonly Stopwatch sw = new();
    private int frame;

    private PixelSize lastPixelSize = new(1, 1);

    static ProperTheEnginePanel()
    {
        FocusableProperty.OverrideDefaultValue<ProperTheEnginePanel>(true);
    }

    public ProperTheEnginePanel()
    {
        Focusable = true;
    }

    // A bare Control has no hit-test geometry, so pointer events never reach it (the old panel was a
    // Panel with a transparent Background). A transparent fill gives the whole bounds a hit region;
    // it sits behind the composition surface visual and doesn't obscure the rendered image.
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
    }

    // ------------------------------------------------------------- IWindowHost / IRenderLoopHost

    // the engine sizes its internal render targets / camera from these; return the interop image size
    // (DIPs * render scaling) so they match the image the compositor displays.
    public float WindowWidth => lastPixelSize.Width;
    public float WindowHeight => lastPixelSize.Height;
    public float DpiScaling => Bounds.Width > 0 ? (float)(lastPixelSize.Width / Bounds.Width) : 1f;

    // ------------------------------------------------------------- Game property

    private IGame? game;
    private bool gameDisposeRequested;

    public static readonly DirectProperty<ProperTheEnginePanel, IGame?> GameProperty =
        AvaloniaProperty.RegisterDirect<ProperTheEnginePanel, IGame?>(nameof(Game), o => o.Game, (o, v) => o.Game = v);

    public IGame? Game
    {
        get => game;
        set
        {
            if (game != null)
                game.RequestDispose -= GameOnRequestDispose;
            SetAndRaise(GameProperty, ref game, value);
            if (game != null)
                game.RequestDispose += GameOnRequestDispose;
        }
    }

    private void GameOnRequestDispose()
    {
        if (game != null)
            game.RequestDispose -= GameOnRequestDispose;
        gameDisposeRequested = true;
        // the render loop is paused while detached, so nothing would consume the flag; the request
        // comes from the UI thread (document close), so it is safe to tear down inline here
        if (VisualRoot == null)
            DisposeGameAndGraphics();
    }

    private void DisposeGameAndGraphics()
    {
        gameDisposeRequested = false;
        var disposingGame = game;
        game = null;
        disposingGame?.DisposeGame();
        // flush continuations posted to the game loop during the dispose sequence -
        // NextFrame won't run again, so anything left queued would hang its awaiters
        gameRunner?.DrainPendingWork();
        TearDownGraphics();
    }

    // ------------------------------------------------------------- graphics lifecycle

    protected override (bool success, string info) InitializeGraphicsResources(Compositor compositor,
        CompositionDrawingSurface compositionDrawingSurface, ICompositionGpuInterop gpuInterop)
    {
        try
        {
            var (ctx, info) = InteropVulkanContext.TryCreate(gpuInterop);
            if (ctx == null)
                return (false, info);
            interopCtx = ctx;

            engineCtx = new EngineVulkanContext(ctx.Api, ctx.Instance, ctx.PhysicalDevice, ctx.Device, ctx.Queue, ctx.QueueFamilyIndex);

            // seed the size from the current bounds so the engine's initial render targets aren't 1x1
            if (Bounds.Width >= 1 && Bounds.Height >= 1)
                lastPixelSize = new PixelSize((int)Bounds.Width, (int)Bounds.Height);

            var backend = new VulkanRenderBackend(engineCtx, externalTarget, this);
            engine = new Engine(backend, new Configuration(), this, false);
            gameRunner = new GameRunner(engine);
            gameRunner.SyncInputState += SyncInputState;

            interopSwapchain = new InteropVulkanSwapchain(ctx, gpuInterop, compositionDrawingSurface);

            sw.Restart();
            return (true, info);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, e.ToString());
        }
    }

    // detaching (hiding the tab, docking elsewhere) must not destroy the engine; the game asks for
    // destruction itself via IGame.RequestDispose when its document is closed
    protected override bool FreeResourcesOnDetach => false;

    protected override void RenderFrame(PixelSize size)
    {
        if (engine == null || gameRunner == null || interopSwapchain == null)
            return;

        if (gameDisposeRequested)
        {
            DisposeGameAndGraphics();
            return;
        }

        if (game == null)
            return;

        try
        {
            var delta = (float)sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            lastPixelSize = size;
            engine.statsManager.PixelSize = new Vector2(size.Width, size.Height);

            using (interopSwapchain.BeginDraw(size, out var image))
            {
                externalTarget.SetImage(image.InternalHandle,
                    new Silk.NET.Vulkan.ImageView(image.ViewHandle),
                    new Silk.NET.Vulkan.Extent2D((uint)image.Size.Width, (uint)image.Size.Height),
                    image.Format);
                gameRunner.NextFrame(delta / 1000.0f, game);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            frame++;
        }
    }

    protected override void FreeGraphicsResources()
    {
        var disposingGame = game;
        game = null;
        disposingGame?.DisposeGame();
        gameRunner?.DrainPendingWork();

        var eng = engine;
        engine = null;
        gameRunner = null;
        var sc = interopSwapchain;
        interopSwapchain = null;
        var ic = interopCtx;
        interopCtx = null;
        engineCtx = null;

        DisposeResourcesAsync(eng, sc, ic);
    }

    // mirrors GpuInterop's VulkanResources.DisposeAsync ordering: the engine (and its adopted context,
    // which leaves the device alive) first, then the interop swapchain images, then the device/instance.
    private static async void DisposeResourcesAsync(Engine? eng, InteropVulkanSwapchain? sc, InteropVulkanContext? ic)
    {
        try
        {
            eng?.Dispose();
            if (sc != null)
                await sc.DisposeAsync();
            ic?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override void OnInfo(string info) => Console.WriteLine($"[ProperTheEnginePanel] {info}");

    // ------------------------------------------------------------- input

    private void SyncInputState()
    {
        var inputs = SwapKeyEvent();
        foreach (var e in inputs)
        {
            switch (e.type)
            {
                case InputEvent.Type.Letter:
                    engine!.inputManager.keyboard.OnTextInput(e.enterLetter);
                    break;
                case InputEvent.Type.Up:
                    engine!.inputManager.keyboard.KeyUp(e.key);
                    break;
                case InputEvent.Type.Down:
                    engine!.inputManager.keyboard.KeyDown(e.key);
                    break;
                case InputEvent.Type.MouseMove:
                    engine!.inputManager.mouse.PointerMoved(e.mouseMoveOrWheel.X, e.mouseMoveOrWheel.Y, e.mouseMoveOrWheel.Z, e.mouseMoveOrWheel.W);
                    break;
                case InputEvent.Type.MouseUp:
                    engine!.inputManager.mouse.MouseUp(e.button);
                    break;
                case InputEvent.Type.MouseDown:
                    engine!.inputManager.mouse.MouseDown(e.button);
                    break;
                case InputEvent.Type.MouseWheel:
                    engine!.inputManager.mouse.MouseWheel(new Vector2(e.mouseMoveOrWheel.X, e.mouseMoveOrWheel.Y));
                    break;
            }
        }
        inputs.Clear();
    }

    private struct InputEvent
    {
        public enum Type { Letter, Up, Down, MouseDown, MouseUp, MouseWheel, MouseMove }
        public Type type;
        public char enterLetter;
        public Input.Key key;
        public MouseButton button;
        public Vector4 mouseMoveOrWheel;
    }

    private readonly List<InputEvent>[] keyEvents = { new(), new() };
    private int keyEventId;
    private List<InputEvent> SwapKeyEvent()
    {
        var newValue = Interlocked.Increment(ref keyEventId);
        return keyEvents[(newValue - 1) % 2];
    }
    private List<InputEvent> pendingBuffer => keyEvents[keyEventId % 2];
    private void PushEvent(InputEvent e) => pendingBuffer.Add(e);

    private int lastGotFocusFrame;
    private IDisposable? globalKeyDownDisposable;
    private IDisposable? globalKeyUpDisposable;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        sw.Restart();
        globalKeyDownDisposable = ((Control)e.Root).AddDisposableHandler(KeyDownEvent, GlobalKeyDown, RoutingStrategies.Tunnel);
        globalKeyUpDisposable = ((Control)e.Root).AddDisposableHandler(KeyUpEvent, GlobalKeyUp, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        globalKeyUpDisposable?.Dispose();
        globalKeyDownDisposable?.Dispose();
        globalKeyUpDisposable = null;
        globalKeyDownDisposable = null;
        sw.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void GlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (AvaloniaKeyMap.IsModifierKey(e.Key))
            PushEvent(new InputEvent { type = InputEvent.Type.Down, key = AvaloniaKeyMap.Convert(e.Key) });
    }

    // ALL key-ups are captured window-wide (not just modifiers): a key released while some other
    // control has focus must still clear the engine's key state, or it stays stuck down
    private void GlobalKeyUp(object? sender, KeyEventArgs e)
    {
        PushEvent(new InputEvent { type = InputEvent.Type.Up, key = AvaloniaKeyMap.Convert(e.Key) });
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        // only force-release when the pointer left the view too: focus blinks away on world clicks
        // (hosted documents activating) and releasing held WASD there kills the camera mid-flight;
        // while the pointer stays over the view, the window-wide key-up handler covers real releases
        if (!IsPointerOver)
            engine?.inputManager.keyboard.ReleaseAllKeys();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        lastGotFocusFrame = frame;
        base.OnGotFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        PushEvent(new InputEvent { type = InputEvent.Type.Down, key = AvaloniaKeyMap.Convert(e.Key) });
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        PushEvent(new InputEvent { type = InputEvent.Type.Up, key = AvaloniaKeyMap.Convert(e.Key) });
        base.OnKeyUp(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // ignore a press that arrives in the same frame as focus (click-to-focus)
        if (lastGotFocusFrame == frame)
            return;
        var props = e.GetCurrentPoint(this).Properties;
        PushEvent(new InputEvent
        {
            type = InputEvent.Type.MouseDown,
            button = (props.IsLeftButtonPressed ? MouseButton.Left : MouseButton.None) |
                     (props.IsRightButtonPressed ? MouseButton.Right : MouseButton.None) |
                     (props.IsMiddleButtonPressed ? MouseButton.Middle : MouseButton.None),
        });
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        PushEvent(new InputEvent
        {
            type = InputEvent.Type.MouseMove,
            mouseMoveOrWheel = new Vector4((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y, (float)Bounds.Width, (float)Bounds.Height),
        });
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        PushEvent(new InputEvent
        {
            type = InputEvent.Type.MouseUp,
            button = (props.IsLeftButtonPressed ? MouseButton.Left : MouseButton.None) |
                     (props.IsRightButtonPressed ? MouseButton.Right : MouseButton.None) |
                     (props.IsMiddleButtonPressed ? MouseButton.Middle : MouseButton.None),
        });
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        PushEvent(new InputEvent
        {
            type = InputEvent.Type.MouseWheel,
            mouseMoveOrWheel = new Vector4((float)e.Delta.X, (float)e.Delta.Y, 0, 0),
        });
        base.OnPointerWheelChanged(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Text != null)
        {
            foreach (var letter in e.Text)
                PushEvent(new InputEvent { type = InputEvent.Type.Letter, enterLetter = letter });
            e.Handled = true;
        }
    }
}
