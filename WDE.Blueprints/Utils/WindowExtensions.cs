using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace ExtensionMethods
{
    public static class WindowExtensions
    {
        public static void ShowDialogCenteredToMouse(this Window window)
        {
            ComputeTopLeft(ref window);
            window.Show();
        }

        public static Task<bool?> ShowDialogAsync(this Window self)
        {
            if (self == null)
                throw new ArgumentNullException("self");

            var completion = new TaskCompletionSource<bool?>();
            self.Dispatcher.BeginInvoke(new Action(() => completion.SetResult(self.ShowDialog())));

            return completion.Task;
        }

        private static void ComputeTopLeft(ref Window window)
        {
            W32Point pt = new();
            if (!GetCursorPos(ref pt))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            var dpi = 96; // System.Windows.Media.VisualTreeHelper.GetDpi(window);

            window.Top = pt.Y / 96; // dpi.DpiScaleY;
            window.Left = pt.X / 96; // dpi.DpiScaleX;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref W32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        public struct W32Point
        {
            public int X;
            public int Y;
        }
    }
}