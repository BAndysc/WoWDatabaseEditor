using System.Diagnostics;

namespace WDE.Common.Tasks
{
    public static class GlobalApplication
    {
        public static IMainThread MainThread { get; private set; }

        public static void InitializeApplication(IMainThread mainThread)
        {
            Debug.Assert(MainThread == null);
            MainThread = mainThread;
        }
    }
}