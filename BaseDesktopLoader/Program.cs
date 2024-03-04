﻿using System.Threading.Tasks;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using WDE.Common.Tasks;
using WoWDatabaseEditorCore;
using WoWDatabaseEditorCore.Avalonia;
using WoWDatabaseEditorCore.Avalonia.Utils;
using WoWDatabaseEditorCore.Managers;
using WoWDatabaseEditorCore.Services.FileSystemService;

namespace BaseDesktopLoader
{
    public abstract class BaseProgramLoader
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(System.Type[] modules, string[] args)
        {
            MigrateMacOsSettings();
            
            WoWDatabaseEditorCore.Avalonia.Program.PreloadedModules = modules;

            if (!BeforeBuildApp(args))
                return;

            var app = BuildAvaloniaApp();
            try
            {
                app.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                FatalErrorHandler.ExceptionOccured(e, args);
            }
            TheEngine.TheEngine.Deinit();
        }

        private static void MigrateMacOsSettings()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var oldLocalDataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

                var newSettingsPath = Path.Join(localDataPath, FileSystem.APPLICATION_FOLDER);
                var oldSettingsPath = Path.Join(oldLocalDataPath, FileSystem.APPLICATION_FOLDER);
                
                if (Directory.Exists(oldSettingsPath) && !Directory.Exists(newSettingsPath))
                {
                    Directory.Move(oldSettingsPath, newSettingsPath);
                }
            }
        }

        public static bool BeforeBuildApp(string[] args)
        {
            FixCurrentDirectory();
            if (ProgramBootstrap.TryLaunchUpdaterIfNeeded(args))
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
        
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            IconProvider.Current
                .Register<MaterialDesignIconProvider>();
            
            var configuration = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToTrace();

            return configuration;
        }
    }
}
