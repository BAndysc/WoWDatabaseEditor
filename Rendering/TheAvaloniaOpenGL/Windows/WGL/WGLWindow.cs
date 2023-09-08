using SPB.Windowing;
using System.Runtime.Versioning;
using static SPB.Platform.Win32.Win32;

namespace SPB.Platform.WGL
{
    [SupportedOSPlatform("windows")]
    public class WGLWindow : SwappableNativeWindowBase
    {
        public override NativeHandle DisplayHandle { get; }
        public override NativeHandle WindowHandle { get; }

        public bool IsDisposed { get; private set; }

        public WGLWindow(NativeHandle windowHandle)
        {
            DisplayHandle = new NativeHandle(GetDC(windowHandle.RawHandle));
            WindowHandle = windowHandle;
        }

        public override uint SwapInterval
        {
            get
            {
                return (uint)WGLHelper.GetSwapInterval();
            }
            set
            {
                bool success = WGLHelper.SwapInterval((int)value);

                if (!success)
                {
                    // TODO: exception
                }
            }
        }

        public override void SwapBuffers()
        {
            bool success = Win32.Win32.SwapBuffers(DisplayHandle.RawHandle);

            if (!success)
            {
                // TODO: exception
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    ReleaseDC(WindowHandle.RawHandle, DisplayHandle.RawHandle);
                    DestroyWindow(WindowHandle.RawHandle);
                }

                IsDisposed = true;
            }
        }

        public override void Show()
        {
            ShowWindow(WindowHandle.RawHandle, ShowWindowFlag.SW_SHOWNOACTIVATE);
        }

        public override void Hide()
        {
            ShowWindow(WindowHandle.RawHandle, ShowWindowFlag.SW_HIDE);
        }
    }
}
