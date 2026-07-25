using Avalonia.Threading;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using TheEngineAvalonia.Interfaces;

namespace TheEngineAvalonia.X11;

public class EmbeddedX11VulkanWindow : IRenderingWindow
{
    private readonly IntPtr display;
    private readonly IntPtr window;
    private readonly Thread eventThread;
    private volatile bool running;

    public IntPtr Pointer => window;
    public event Action<long, long, bool, bool>? OnPointerPressed;
    public event Action<long, long, bool, bool>? OnPointerReleased;
    public event Action<long, long, bool, bool>? OnPointerMoved;

    public string[] RequiredInstanceExtensions => new[] { KhrSurface.ExtensionName, KhrXlibSurface.ExtensionName };

    public EmbeddedX11VulkanWindow(IntPtr parentWindow)
    {
        display = Xlib.XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new Exception("XOpenDisplay failed (no X11 display?)");

        window = Xlib.XCreateSimpleWindow(display, parentWindow, 0, 0, 640, 480, 0, 0, 0);
        Xlib.XSelectInput(display, window, Xlib.ButtonPressMask | Xlib.ButtonReleaseMask | Xlib.PointerMotionMask);
        Xlib.XMapWindow(display, window);
        Xlib.XFlush(display);

        // Own connection to the X server (separate from Avalonia's), so events for our
        // window land in our own queue and don't need to be threaded through Avalonia's
        // X11 backend at all.
        running = true;
        eventThread = new Thread(EventLoop) { IsBackground = true, Name = "X11EmbeddedWindowEvents" };
        eventThread.Start();
    }

    private void EventLoop()
    {
        while (running)
        {
            while (running && Xlib.XPending(display) > 0)
            {
                Xlib.XEvent ev = default;
                Xlib.XNextEvent(display, ref ev);
                HandleEvent(ev);
            }
            Thread.Sleep(4);
        }
    }

    private void HandleEvent(Xlib.XEvent ev)
    {
        long x = ev.x, y = ev.y;
        switch (ev.type)
        {
            case Xlib.ButtonPress:
            {
                bool isLeft = ev.button == Xlib.Button1;
                bool isRight = ev.button == Xlib.Button3;
                Dispatcher.UIThread.Post(() => OnPointerPressed?.Invoke(x, y, isLeft, isRight));
                break;
            }
            case Xlib.ButtonRelease:
            {
                bool isLeft = ev.button == Xlib.Button1;
                bool isRight = ev.button == Xlib.Button3;
                Dispatcher.UIThread.Post(() => OnPointerReleased?.Invoke(x, y, isLeft, isRight));
                break;
            }
            case Xlib.MotionNotify:
                Dispatcher.UIThread.Post(() => OnPointerMoved?.Invoke(x, y, false, false));
                break;
        }
    }

    public void Show() => Xlib.XMapWindow(display, window);

    public void Hide() => Xlib.XUnmapWindow(display, window);

    public unsafe SurfaceKHR CreateSurface(Vk vk, Instance instance)
    {
        if (!vk.TryGetInstanceExtension(instance, out KhrXlibSurface xlibSurfaceExt))
            throw new Exception("VK_KHR_xlib_surface not available");

        var dpyHandle = display;
        IntPtr* dpy = &dpyHandle;
        var createInfo = new XlibSurfaceCreateInfoKHR
        {
            SType = StructureType.XlibSurfaceCreateInfoKhr,
            Dpy = dpy,
            Window = window,
        };
        var result = xlibSurfaceExt.CreateXlibSurface(instance, in createInfo, null, out var surface);
        if (result != Result.Success)
            throw new Exception($"vkCreateXlibSurfaceKHR failed: {result}");
        return surface;
    }

    public void Dispose()
    {
        running = false;
        eventThread.Join();
        Xlib.XDestroyWindow(display, window);
        Xlib.XCloseDisplay(display);
    }
}
