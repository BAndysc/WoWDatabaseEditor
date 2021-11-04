using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.ReactiveUI;
using WDE.Common.Tasks;
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
            GlobalApplication.Arguments.Init(args);
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
        {
            var configuration = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                // .With(new AvaloniaNativePlatformOptions { UseGpu = false })
                //.With(new Win32PlatformOptions(){AllowEglInitialization = false})
                .UseReactiveUI()
                .LogToTrace();
            
#if USE_OPENTK
            configuration = configuration.UseOpenTK(new List<GlVersion> { new GlVersion(GlProfileType.OpenGL, 4, 1, true) });
#endif
            if (GlobalApplication.Arguments.IsArgumentSet("wgl"))
            {
                configuration = configuration.
                    With(new Win32PlatformOptions() { UseWgl = true, 
                        AllowEglInitialization = false,
                        WglProfiles = new List<GlVersion>()
                    {
                        new GlVersion(GlProfileType.OpenGL, 4, 6),
                        new GlVersion(GlProfileType.OpenGL, 4, 5),
                        new GlVersion(GlProfileType.OpenGL, 4, 4),
                        new GlVersion(GlProfileType.OpenGL, 4, 3),
                        new GlVersion(GlProfileType.OpenGL, 4, 2),
                        new GlVersion(GlProfileType.OpenGL, 4, 1),
                    }});
            }

            return configuration;
        }
    }
}