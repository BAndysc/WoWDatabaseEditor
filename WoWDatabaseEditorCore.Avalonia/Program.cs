using System;
using System.IO;
using System.Reflection;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.ReactiveUI;
using WoWDatabaseEditorCore.Managers;

namespace WoWDatabaseEditorCore.Avalonia
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            FixCurrentDirectory();
            if (ProgramBootstrap.TryLaunchUpdaterIfNeeded())
                return;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            SafeFireAndForgetExtensions.SetDefaultExceptionHandling(Console.WriteLine);
            var app = BuildAvaloniaApp();
            try
            {
                app.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                FatalErrorHandler.ExceptionOccured(e);
            }
        }

        private static void FixCurrentDirectory()
        {
            var exePath = new FileInfo(Assembly.GetExecutingAssembly().Location);
            if (exePath.Directory != null)
                Directory.SetCurrentDirectory(exePath.Directory.FullName);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
               // .With(new AvaloniaNativePlatformOptions { UseGpu = false })
                //.With(new Win32PlatformOptions(){AllowEglInitialization = false})
                .UseReactiveUI()
                .LogToTrace();
    }
}