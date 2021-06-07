using System;
using System.Diagnostics;

namespace WDE.Common.Tasks
{
    public static class GlobalApplication
    {
        public enum AppBackend
        {
            Uninitialized,
            WPF,
            Avalonia
        }

        public static IMainThread? mainThread;

        public static IMainThread MainThread
        {
            get
            {
                if (mainThread == null)
                    throw new Exception("Invalid use of MainThread before initialization, fatal error");
                return mainThread;
            }
        }
        public static AppBackend Backend { get; private set; }
        
        public static bool HighDpi { get; set; }

        public static void InitializeApplication(IMainThread thread, AppBackend backend)
        {
            Debug.Assert(mainThread == null);
            mainThread = thread;
            Backend = backend;
        }
    }
}