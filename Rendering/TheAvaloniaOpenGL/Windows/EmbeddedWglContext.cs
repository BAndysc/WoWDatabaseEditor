using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using OpenTK.Graphics.OpenGL4;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Platform.WGL;
using SPB.Windowing;
using TheAvaloniaOpenGL.Apple;
using TheAvaloniaOpenGL.Interfaces;
using static TheAvaloniaOpenGL.Windows.Win32NativeInterop;

namespace TheAvaloniaOpenGL.Windows;

public class EmbeddedWglContext : IRenderingOpenGlContext, System.IDisposable
{
    private static bool glBindingsLoaded;
    public OpenGLContextBase Context { get; set; }
    
    public EmbeddedWglContext()
    {
        var flags = OpenGLContextFlags.Compat;
#if DEBUG
        flags |= OpenGLContextFlags.Debug;
#endif

        var graphicsMode = FramebufferFormat.Default;
        
        Context = PlatformHelper.CreateOpenGLContext(graphicsMode, 4, 1, flags);
    }
    
    public void Dispose()
    {
        Context.Dispose();
        Context = null!;
    }

    public void MakeCurrentContext(IRenderingWindow? window)
    {
        Context.MakeCurrent((window as EmbeddedWglWindow)?.BaseWindow);
        if (!glBindingsLoaded)
        {
            GL.LoadBindings(new OpenTKBindingsContext(Context.GetProcAddress));
            glBindingsLoaded = true;
        }
    }

    public void Initialize(SwappableNativeWindowBase swappableNativeWindowBase)
    {
        Context.Initialize(swappableNativeWindowBase);
    }
}

public class EmbeddedWglWindow : IRenderingWindow
{
    private SwappableNativeWindowBase _window;
    private WindowProc _wndProcDelegate;
    private string _className;
    protected IntPtr WindowHandle { get; set; }
    public NativeWindowBase BaseWindow => _window;
    public IntPtr Pointer => WindowHandle;

    public event Action<long, long, bool, bool>? OnPointerPressed;
    public event Action<long, long, bool, bool>? OnPointerReleased;
    public event Action<long, long, bool, bool>? OnPointerMoved;
    
    public EmbeddedWglWindow(IntPtr parentHandle, EmbeddedWglContext context)
    {
        _className = "NativeWindow-" + Guid.NewGuid();

        _wndProcDelegate = delegate (IntPtr hWnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WindowsMessages.Lbuttondown ||
                msg == WindowsMessages.Rbuttondown ||
                msg == WindowsMessages.Lbuttonup ||
                msg == WindowsMessages.Rbuttonup ||
                msg == WindowsMessages.Mousemove)
            {
                var x = (long)lParam & 0xFFFF;
                var y = (long)lParam >> 16 & 0xFFFF;

                if (msg == WindowsMessages.Lbuttondown || msg == WindowsMessages.Rbuttondown)
                {
                    var isLeft = msg == WindowsMessages.Lbuttondown;
                    OnPointerPressed?.Invoke(x, y, isLeft, !isLeft);
                }
                else if (msg == WindowsMessages.Lbuttonup || msg == WindowsMessages.Rbuttonup)
                {
                    var isLeft = msg == WindowsMessages.Lbuttonup;
                    OnPointerReleased?.Invoke(x, y, isLeft, !isLeft);
                }
                else if (msg == WindowsMessages.Mousemove)
                {
                    OnPointerMoved?.Invoke(x, y, false, false);
                }
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        };

        WndClassEx wndClassEx = new()
        {
            cbSize = Marshal.SizeOf<WndClassEx>(),
            hInstance = GetModuleHandle(null),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            style = ClassStyles.CsOwndc,
            lpszClassName = Marshal.StringToHGlobalUni(_className),
            hCursor = CreateArrowCursor(),
        };

        RegisterClassEx(ref wndClassEx);

        WindowHandle = CreateWindowEx(0, _className, "NativeWindow", WindowStyles.WsChild, 0, 0, 640, 480, parentHandle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Marshal.FreeHGlobal(wndClassEx.lpszClassName);
        
        _window = new WGLWindow(new NativeHandle(WindowHandle));
        
        context.Initialize(_window);

    }
    
    public void Dispose()
    {
        DestroyWindow(WindowHandle);
        UnregisterClass(_className, GetModuleHandle(null));
    }

    public void SwapBuffers()
    {
        _window.SwapBuffers();
    }
}