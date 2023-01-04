using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Extensions
{
    internal static class WindowExtensions
    {
        private static readonly bool IsWin32Nt = Environment.OSVersion.Platform == PlatformID.Win32NT;
        
        public enum DWMWINDOWATTRIBUTE : uint
        {
            NCRenderingEnabled = 1,
            NCRenderingPolicy,
            TransitionsForceDisabled,
            AllowNCPaint,
            CaptionButtonBounds,
            NonClientRtlLayout,
            ForceIconicRepresentation,
            Flip3DPolicy,
            ExtendedFrameBounds,
            HasIconicBitmap,
            DisallowPeek,
            ExcludedFromPeek,
            Cloak,
            Cloaked,
            FreezeRepresentation
        }
        
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);
        
        public static void ActivateWorkaround(this Window window)
        {
            if (ReferenceEquals(window, null)) throw new ArgumentNullException(nameof(window));

            if (IsWin32Nt)
            {
                
                if (!window.IsActive)
                {
                    int val = 1;
                    DwmSetWindowAttribute(window.TryGetPlatformHandle()!.Handle,
                        DWMWINDOWATTRIBUTE.TransitionsForceDisabled,
                        ref val,
                        sizeof(int));
                    
                    window.WindowState = WindowState.Minimized;
                    window.WindowState = WindowState.Normal;
                    
                    val = 0;
                    DwmSetWindowAttribute(window.TryGetPlatformHandle()!.Handle,
                        DWMWINDOWATTRIBUTE.TransitionsForceDisabled,
                        ref val,
                        sizeof(int));
                }
                window.Activate();
            }
            else
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                window.Activate();
            }
        }
    }
}