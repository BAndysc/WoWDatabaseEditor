using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SPB.Windowing;
using TheAvaloniaOpenGL.Apple;
using TheAvaloniaOpenGL.Interfaces;
using TheAvaloniaOpenGL.Windows;
using Pointer = Avalonia.Input.Pointer;

namespace TheAvaloniaOpenGL;

public class NativeOpenGlControlBase : NativeControlHost
{
    private IRenderingWindow? window;
    private IRenderingOpenGlContext? context;
    private IDisposable? timer;

    private IRenderingOpenGlContext CreateContext()
    {
        if (OperatingSystem.IsWindows())
            return new EmbeddedWglContext();
        else if (OperatingSystem.IsMacOS())
            return new EmbeddedAppleContext();
        throw new PlatformNotSupportedException();
    }

    private IRenderingWindow CreateWindow(IntPtr parentHwnd, IRenderingOpenGlContext context)
    {
        if (OperatingSystem.IsWindows())
            return new EmbeddedWglWindow(parentHwnd, (context as EmbeddedWglContext)!);
        else if (OperatingSystem.IsMacOS())
            return new EmbeddedAppleWindow((context as EmbeddedAppleContext)!);
        throw new PlatformNotSupportedException();
    }

    Stopwatch sw = new Stopwatch();
    Compositor compositor;

    IPlatformHandle CreateWindows(IPlatformHandle control)
    {
        bool justCreatedContext = false;
        if (context == null)
        {
            context = CreateContext();
            justCreatedContext = true;
        }

        window = CreateWindow(control.Handle, context);
        window.OnPointerPressed += OnEmbeddedPointerPressed;
        window.OnPointerReleased += OnEmbeddedPointerReleased;
        window.OnPointerMoved += OnEmbeddedPointerMoved;

        if (justCreatedContext)
        {
            context.MakeCurrentContext(window);
            OnOpenGlInit(null, 0);
            context.MakeCurrentContext(null);
        }

        var rendererType = this.GetVisualRoot()!.Renderer.GetType();
        compositor = rendererType.GetProperty("Compositor", BindingFlags.Instance | BindingFlags.Public)
            .GetValue(this.GetVisualRoot()?.Renderer) as Compositor;

        //compositor.RequestCompositionUpdate(Render);

        // Task.Run(() =>
        // {
        //     while (true)
        //         Render();
        // });

        timer = DispatcherTimer.Run(() =>
        {
            //Dispatcher.UIThread.RunJobs(DispatcherPriority.Input);
            Render();
            return true;
        }, TimeSpan.FromMilliseconds(10));
        
        return new PlatformHandle(window.Pointer, "HWND");        
        
        /*bool dontRender = false;
        Stopwatch sw = new Stopwatch();
        timer = DispatcherTimer.Run(() =>
        {
            if (dontRender)
                return true;
            dontRender = true;
            sw.Restart();
            context.MakeCurrentContext();
            OnOpenGlRender(null, 0);
        
            Task.Run(() =>
            {
                if (window == null)
                    return;
                
                context?.FlushBuffer();
                dontRender = false;
            });
            sw.Stop();
            PresentTime = (uint)sw.Elapsed.TotalMilliseconds;
            return true;
        }, TimeSpan.FromMilliseconds(1));*/
    }

    private void Render()
    {
        sw.Restart();
        context.MakeCurrentContext(window);
        OnOpenGlRender(null, 0);
        window?.SwapBuffers();
        context?.MakeCurrentContext(null);
        sw.Stop();
        PresentTime = (uint)sw.Elapsed.TotalMilliseconds;
        //compositor.RequestCompositionUpdate(Render);
    }

    private void OnEmbeddedPointerPressed(long x, long y, bool isLeft, bool isRight)
    {
        var scaling = VisualRoot?.RenderScaling ?? 1;
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
        var scaling = VisualRoot?.RenderScaling ?? 1;
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
        var scaling = VisualRoot?.RenderScaling ?? 1;
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
        return CreateWindows(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (window != null)
        {
            window.OnPointerPressed -= OnEmbeddedPointerPressed;
            window.OnPointerReleased -= OnEmbeddedPointerReleased;
            window.OnPointerMoved -= OnEmbeddedPointerMoved;
            window.Dispose();
            window = null;
        }
        timer?.Dispose();
        timer = null;
    }

    protected virtual void OnOpenGlInit(GlInterface gl, int fb)
    {
        
    }

    protected virtual void OnOpenGlDeinit(GlInterface gl, int fb)
    {
        
    }

    protected virtual void OnOpenGlRender(GlInterface gl, int fb)
    {
        
    }
    
    protected void DoCleanup()
    {
        if (context != null && window != null)
        {
            context.MakeCurrentContext(window);
            OnOpenGlDeinit(null, 0);
            context?.Dispose();
            context = null;
            timer?.Dispose();
            timer = null;
        }
    }

    protected uint PresentTime { get; private set; }
    
    public (int, int) PixelSize => (GetPixelSize().Width, GetPixelSize().Height);
    
    private PixelSize GetPixelSize()
    {
        var scaling = VisualRoot?.RenderScaling ?? 1;
        return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
            Math.Max(1, (int)(Bounds.Height * scaling)));
    }

    public bool HitTest(Point point)
    {
        return true;
    }
}