using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TheEngineAvalonia.X11;

// Minimal Xlib P/Invoke surface: just enough to embed a child window and pump its
// button/motion events. Struct layouts below match the public, ABI-frozen Xlib.h headers.
[SupportedOSPlatform("linux")]
internal static partial class Xlib
{
    private const string LibX11 = "libX11.so.6";

    public const long ButtonPressMask = 1L << 2;
    public const long ButtonReleaseMask = 1L << 3;
    public const long PointerMotionMask = 1L << 6;

    public const int ButtonPress = 4;
    public const int ButtonRelease = 5;
    public const int MotionNotify = 6;

    public const uint Button1 = 1;
    public const uint Button3 = 3;

    // XEvent is a C union sized to the largest event struct (long pad[24] == 192 bytes on
    // 64-bit). XButtonEvent/XMotionEvent share an identical layout up to x/y/state, which is
    // all we read here.
    [StructLayout(LayoutKind.Explicit, Size = 192)]
    public struct XEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(64)] public int x;
        [FieldOffset(68)] public int y;
        [FieldOffset(84)] public uint button;
    }

    [LibraryImport(LibX11)]
    public static partial IntPtr XOpenDisplay(IntPtr displayName);

    [LibraryImport(LibX11)]
    public static partial int XDefaultScreen(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint borderWidth, nuint border, nuint background);

    [LibraryImport(LibX11)]
    public static partial int XMapWindow(IntPtr display, IntPtr window);

    [LibraryImport(LibX11)]
    public static partial int XUnmapWindow(IntPtr display, IntPtr window);

    [LibraryImport(LibX11)]
    public static partial int XDestroyWindow(IntPtr display, IntPtr window);

    [LibraryImport(LibX11)]
    public static partial int XSelectInput(IntPtr display, IntPtr window, long eventMask);

    [LibraryImport(LibX11)]
    public static partial int XFlush(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial int XPending(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial int XNextEvent(IntPtr display, ref XEvent eventReturn);

    [LibraryImport(LibX11)]
    public static partial int XCloseDisplay(IntPtr display);
}
