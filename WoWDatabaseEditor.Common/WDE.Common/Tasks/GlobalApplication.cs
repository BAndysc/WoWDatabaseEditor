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
        
        public static IMainThread MainThread { get; private set; }
        public static AppBackend Backend { get; private set; }

        public static void InitializeApplication(IMainThread mainThread, AppBackend backend)
        {
            Debug.Assert(MainThread == null);
            MainThread = mainThread;
            Backend = backend;
        }
    }
}