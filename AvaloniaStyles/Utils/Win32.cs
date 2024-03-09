using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using WDE.Common;

namespace AvaloniaStyles.Utils;

public static class Win32
{
    [Flags]
    public enum DwmWindowAttribute : uint
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE_OLD_WINDOWS = 19,
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR,
        DWMWA_CAPTION_COLOR,
        DWMWA_TEXT_COLOR,
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
        DWMWA_SYSTEMBACKDROP_TYPE
    }
    
    public static readonly int S_OK = 0;
    
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    public static extern int UpdateWindow(IntPtr hwnd);

    public static bool SetDarkMode(IntPtr hwnd, bool on)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT ||
            Environment.OSVersion.Version.Build < 10240) // pre windows 10
        {
            return false;
        }
        
        int value = on ? 1 : 0;
        int result = DwmSetWindowAttribute(hwnd, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));

        if (result == S_OK)
        {
            UpdateWindow(hwnd);
            return true;
        }

        int result2 = DwmSetWindowAttribute(hwnd, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD_WINDOWS, ref value,
            sizeof(int));

        if (result2 == S_OK)
        {
            UpdateWindow(hwnd);
            return true;
        }
        
        var msg = Marshal.GetExceptionForHR(result)?.Message;
        var msg2 = Marshal.GetExceptionForHR(result2)?.Message;
        
        LOG.LogWarning($"Can't set window dark mode:\n DWMWA_USE_IMMERSIVE_DARK_MODE method: {msg}\n DWMWA_USE_IMMERSIVE_DARK_MODE_OLD_WINDOWS method: {msg2}");
        return false;
    }

    private static int ToWinApiRGB(Color color)
    {
        // https://learn.microsoft.com/en-us/windows/win32/gdi/colorref
        // When specifying an explicit RGB color, the COLORREF value has the following hexadecimal form:
        // 0x00bbggrr
        
        return (color.B << 16) | (color.G << 8) | color.R;
    }
    
    public static bool SetTitleBarColor(IntPtr hwnd, Color color)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT ||
            Environment.OSVersion.Version.Build < 22000) // pre windows 11
        {
            return false;
        }

        int rgb = ToWinApiRGB(color);
        int result = DwmSetWindowAttribute(hwnd, DwmWindowAttribute.DWMWA_CAPTION_COLOR, ref rgb,
            sizeof(int));

        if (result == S_OK)
        {
            UpdateWindow(hwnd);
            return true;
        }
        
        var msg = Marshal.GetExceptionForHR(result)?.Message;
        LOG.LogWarning($"Can't set window caption color: {msg}");
        return false;
    }
}