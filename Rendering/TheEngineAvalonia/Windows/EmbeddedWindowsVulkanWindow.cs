using System.Runtime.InteropServices;
using SPB.Platform.Win32;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using TheEngineAvalonia.Interfaces;
using static TheEngineAvalonia.Windows.Win32NativeInterop;

public class EmbeddedWindowsVulkanWindow : IRenderingWindow
{
    private WindowProc _wndProcDelegate;
    private string _className;
    protected IntPtr WindowHandle { get; set; }
    public IntPtr Pointer => WindowHandle;

    public string[] RequiredInstanceExtensions => new[] { KhrSurface.ExtensionName, KhrWin32Surface.ExtensionName };

    public unsafe SurfaceKHR CreateSurface(Vk vk, Instance instance)
    {
        if (!vk.TryGetInstanceExtension(instance, out KhrWin32Surface win32SurfaceExt))
            throw new Exception("VK_KHR_win32_surface not available");

        var createInfo = new Win32SurfaceCreateInfoKHR
        {
            SType = StructureType.Win32SurfaceCreateInfoKhr,
            Hinstance = GetModuleHandle(null),
            Hwnd = WindowHandle,
        };
        var result = win32SurfaceExt.CreateWin32Surface(instance, in createInfo, null, out var surface);
        if (result != Result.Success)
            throw new Exception($"vkCreateWin32SurfaceKHR failed: {result}");
        return surface;
    }

    public event Action<long, long, bool, bool>? OnPointerPressed;
    public event Action<long, long, bool, bool>? OnPointerReleased;
    public event Action<long, long, bool, bool>? OnPointerMoved;
    
    public EmbeddedWindowsVulkanWindow(IntPtr parentHandle)
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
    }
    
    public void Show()
    {
        ShowWindow(WindowHandle, ShowWindowFlag.SW_SHOWNOACTIVATE);
    }

    public void Hide()
    {
        ShowWindow(WindowHandle, ShowWindowFlag.SW_HIDE);
    }
    
    public void Dispose()
    {
        DestroyWindow(WindowHandle);
        UnregisterClass(_className, GetModuleHandle(null));
    }
}