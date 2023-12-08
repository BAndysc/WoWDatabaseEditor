using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using WDE.Common.Tasks;
using WoWDatabaseEditorCore.Avalonia.Utils;
using WoWDatabaseEditorCore.Managers;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class Program
    {
        public static Type[] PreloadedModules = new Type[]{};
        public static string ApplicationName = "WoW Database Editor 2024.1";
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            if (!BeforeBuildApp(args))
                return;

            var app = BuildAvaloniaApp();
            try
            {
                app.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                FatalErrorHandler.ExceptionOccured(e);
            }
            TheEngine.TheEngine.Deinit();
        }

        public static bool BeforeBuildApp(string[] args)
        {
            FixCurrentDirectory();
            if (ProgramBootstrap.TryLaunchUpdaterIfNeeded())
                return false;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            SafeFireAndForgetExtensions.SetDefaultExceptionHandling(Console.WriteLine);
            TaskScheduler.UnobservedTaskException += (sender, eventArgs) => Console.WriteLine(eventArgs.Exception);
            GlobalApplication.Arguments.Init(args);

            if (GlobalApplication.Arguments.IsArgumentSet("console"))
            {
                if (OperatingSystem.IsWindows())
                {
                    if (!Win32.AttachConsole(Win32.ATTACH_PARENT_PROCESS))
                        Win32.AllocConsole();
                }
            }
            
            return true;
        }

        private static void FixCurrentDirectory()
        {
            var path = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(path))
                path = System.AppContext.BaseDirectory;
            var exePath = new FileInfo(path);
            if (exePath.Directory != null)
                Directory.SetCurrentDirectory(exePath.Directory.FullName);
        }

        // private static T SafeUseOpenTK<T>(T builder, IList<GlVersion> probeVersions) where T : AppBuilderBase<T>, new()
        // {
        //     return builder.AfterPlatformServicesSetup(_ =>
        //     {
        //         var method = typeof(AvaloniaOpenTKIntegration).GetMethod("Initialize",
        //                 BindingFlags.NonPublic | BindingFlags.Static);
        //
        //         try
        //         {
        //             method!.Invoke(null, new object?[]{probeVersions});
        //         }
        //         catch (Exception e)
        //         {
        //             GlobalApplication.Supports3D = false;
        //             Console.WriteLine(e);
        //         }
        //     });
        // }
        
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var configuration = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                // .With(new AvaloniaNativePlatformOptions { UseGpu = false })
                //.With(new Win32PlatformOptions(){AllowEglInitialization = false})
                .UseReactiveUI()
                .LogToTrace()
                .WithIcons(x => x.Register<MaterialDesignIconProvider>());
#if USE_OPENTK
            Console.WriteLine("Initializing OpenGL");

            configuration = SafeUseOpenTK(configuration, new List<GlVersion> { new GlVersion(GlProfileType.OpenGL, 4, 1, true) });
#endif

            if (OperatingSystem.IsLinux())
                GlobalApplication.Supports3D = false;
            else if (OperatingSystem.IsWindows())
            {
                GlobalApplication.Supports3D = true;
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
                                new GlVersion(GlProfileType.OpenGL, 3, 3),
                            }});
                }
            }
            else
            {
                GlobalApplication.Supports3D = true;
            }

            return configuration;
        }
    }
}