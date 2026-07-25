using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using TheEngineAvalonia.Apple;
using TheEngineAvalonia.Interfaces;
using TheEngineAvalonia.X11;
using Veldrid;
using MouseButton = Avalonia.Input.MouseButton;
using Point = Avalonia.Point;
using Pointer = Avalonia.Input.Pointer;

namespace TheEngineAvalonia;

public partial class NativeVulkanControlBase : NativeControlHost
{
    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleA")]
    public static partial IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPStr)] string lpModuleName);
    
    private IRenderingWindow? window;
    private IDisposable? timer;
    private bool isHidden = true;
    private Thread? renderThread;
    // dispose flow (mirrors ProperTheEnginePanel): RequestCleanup sets the flag from any thread,
    // the render loop consumes it (dedicated thread or UI timer - both keep running while the
    // control is detached), runs OnVulkanDeinit on the rendering thread and stops itself
    private volatile bool cleanupRequested;
    private volatile bool cleanedUp;

    public bool UseThreadRendering { get; set; } = true;
    public static readonly DirectProperty<NativeVulkanControlBase, bool> UseThreadRenderingProperty
        = AvaloniaProperty.RegisterDirect<NativeVulkanControlBase, bool>(nameof(UseThreadRendering), o => o.UseThreadRendering, (o, val) => o.UseThreadRendering = val);

    public NativeVulkanControlBase()
    {
        PixelSize = GetPixelSize();
        this.GetObservable(BoundsProperty)
            .Subscribe(_ =>
            {
                PixelSize = GetPixelSize();
            });
    }

    private IRenderingWindow CreateWindow(IntPtr parentHwnd)
    {
        if (OperatingSystem.IsWindows())
            return new EmbeddedWindowsVulkanWindow(parentHwnd);
        else if (OperatingSystem.IsMacOS())
            return new EmbeddedAppleVulkanWindow();
        else if (OperatingSystem.IsLinux())
            return new EmbeddedX11VulkanWindow(parentHwnd);
        throw new PlatformNotSupportedException();
    }

    private void OnEmbeddedPointerPressed(long x, long y, bool isLeft, bool isRight)
    {
        if (isHidden)
            return;
        
        var scaling = TopLevel.GetTopLevel(VisualRoot)?.RenderScaling ?? 1;
        Point rootVisualPosition = this.TranslatePoint(new Point(x / scaling, y / scaling), this) ?? default;
        Pointer pointer = new(0, PointerType.Mouse, true);
        
        RawInputModifiers pointerPointModifier = isLeft ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton;
        PointerPointProperties properties = new(pointerPointModifier, isLeft ? PointerUpdateKind.LeftButtonPressed : PointerUpdateKind.RightButtonPressed);

        var evnt = new PointerPressedEventArgs(
            this,
            pointer,
            this,
            rootVisualPosition,
            (ulong)Environment.TickCount64,
            properties,
            KeyModifiers.None);

        RaiseEvent(evnt);
    }
    
    private void OnEmbeddedPointerReleased(long x, long y, bool isLeft, bool isRight)
    {
        if (isHidden)
            return;
        
        var scaling = TopLevel.GetTopLevel(VisualRoot)?.RenderScaling ?? 1;
        Point rootVisualPosition = this.TranslatePoint(new Point(x / scaling, y / scaling), this) ?? default;
        Pointer pointer = new(0, PointerType.Mouse, true);
        
        RawInputModifiers pointerPointModifier = isLeft ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton;
        PointerPointProperties properties = new(pointerPointModifier, isLeft ? PointerUpdateKind.LeftButtonReleased : PointerUpdateKind.RightButtonReleased);

        var evnt = new PointerReleasedEventArgs(
            this,
            pointer,
            this,
            rootVisualPosition,
            (ulong)Environment.TickCount64,
            properties,
            KeyModifiers.None,
            isLeft ? MouseButton.Left : MouseButton.Right);

        RaiseEvent(evnt);
    }
    
    private void OnEmbeddedPointerMoved(long x, long y, bool isLeft, bool isRight)
    {
        if (isHidden)
            return;
        
        var scaling = TopLevel.GetTopLevel(VisualRoot)?.RenderScaling ?? 1;
        Point rootVisualPosition = this.TranslatePoint(new Point(x / scaling, y / scaling), this) ?? default;
        Pointer pointer = new(0, PointerType.Mouse, true);
        
        var evnt = new PointerEventArgs(
            PointerMovedEvent,
            this,
            pointer,
            this,
            rootVisualPosition,
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None);

        RaiseEvent(evnt);
    }
    
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        isHidden = false;
        if (window != null)
        {
            window.Show();
            return new PlatformHandle(window.Pointer, "window");
        }

        window = CreateWindow(parent.Handle);
        if (cleanupRequested || cleanedUp)
        {
            // reattached after a dispose request: the host still needs a valid native window
            // handle, but the engine is (being) torn down - don't start a new render loop
            return new PlatformHandle(window.Pointer, "HWND");
        }
        window.OnPointerPressed += OnEmbeddedPointerPressed;
        window.OnPointerReleased += OnEmbeddedPointerReleased;
        window.OnPointerMoved += OnEmbeddedPointerMoved;

        bool debug;
#if DEBUG
        debug = true;
#else
        debug = false;
#endif

        int lastWidth = -1;
        int lastHeight = -1;
        
        Stopwatch sw = new Stopwatch();
        sw.Start();

        if (UseThreadRendering)
        {
            var afterInit = new ManualResetEventSlim();
            Console.WriteLine("Will render on a dedicated thread");
            renderThread = new Thread(() =>
            {
                OnVulkanInit(window);
                afterInit.Set();
                while (!cleanupRequested)
                {
                    OnVulkanRender();
                }
                PerformCleanup();
            })
            {
                Name = "Vulkan render thread",
                // must not keep the process alive if the app exits without a dispose request
                IsBackground = true,
            };
            renderThread.Start();
            afterInit.Wait();
        }
        else
        {
            OnVulkanInit(window);
            timer = DispatcherTimer.Run(() =>
            {
                // consume a dispose request before the hidden-check: the timer keeps ticking while
                // the control is detached, and this is the only place timer-mode teardown may run
                if (cleanupRequested)
                {
                    PerformCleanup();
                    return false;
                }
                if (sw.ElapsedMilliseconds < 1)
                    return true;

                sw.Restart();
                // the native window can be detached (tab switch, panel close, docking move) without
                // disposing the Vulkan device - presenting against a detached CAMetalLayer/HWND after
                // that point GPU-faults on MoltenVK, so skip rendering entirely while hidden.
                if (isHidden)
                    return true;
                if (lastWidth != PixelSize.Width || lastHeight != PixelSize.Height)
                {
                    lastWidth = PixelSize.Width;
                    lastHeight = PixelSize.Height;
                    // gd.ResizeMainWindow((uint)lastWidth, (uint)lastHeight);
                }
                OnVulkanRender();
                //if (!isHidden)
                //    gd?.SwapBuffers();
                PresentTime = (uint)sw.Elapsed.TotalMilliseconds;
                return true;
            }, TimeSpan.FromMilliseconds(1));
        }
        
        return new PlatformHandle(window.Pointer, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        isHidden = true;
        if (cleanedUp)
        {
            // the engine was already torn down after a dispose request; nothing uses the native
            // window anymore, so free it now (PerformCleanup skipped it while we were attached)
            DisposeWindow();
        }
        else
        {
            // detaching (tab switch, docking move) must not destroy the engine or the window; the
            // game asks for real teardown itself via IGame.RequestDispose -> RequestCleanup
            window?.Hide();
        }
    }

    protected virtual void OnVulkanInit(IRenderingWindow window)
    {

    }

    protected virtual void OnVulkanDeinit()
    {
        
    }

    protected virtual void OnVulkanRender()
    {
        
    }
    
    /// <summary>Requests engine/window teardown. Safe to call from any thread; the render loop
    /// consumes the request (it keeps running while the control is detached), calls
    /// <see cref="OnVulkanDeinit"/> on the rendering thread, stops itself and frees the native
    /// window. If rendering was never initialized there is nothing to clean up.</summary>
    public void RequestCleanup()
    {
        cleanupRequested = true;
    }

    private void PerformCleanup()
    {
        if (cleanedUp)
            return;
        cleanedUp = true;
        OnVulkanDeinit();
        timer?.Dispose();
        timer = null;
        // the native window must be freed on the UI thread; while the control is still attached
        // the host keeps using the handle, so DestroyNativeControlCore frees it on detach instead
        Dispatcher.UIThread.Post(() =>
        {
            if (isHidden)
                DisposeWindow();
        });
    }

    private void DisposeWindow()
    {
        if (window == null)
            return;
        window.OnPointerPressed -= OnEmbeddedPointerPressed;
        window.OnPointerReleased -= OnEmbeddedPointerReleased;
        window.OnPointerMoved -= OnEmbeddedPointerMoved;
        window.Dispose();
        window = null;
    }

    protected uint PresentTime { get; private set; }
    
    public PixelSize PixelSize { get; set; }
    
    private PixelSize GetPixelSize()
    {
        double scaling = TopLevel.GetTopLevel(VisualRoot)?.RenderScaling ?? 1;
        if (OperatingSystem.IsMacOS())
            scaling = 1;
        return new PixelSize(Math.Max(1, (int)Math.Ceiling(Bounds.Width * scaling)),Math.Max(1, (int)Math.Ceiling(Bounds.Height * scaling)));
    }

    public bool HitTest(Avalonia.Point point)
    {
        return true;
    }
}
