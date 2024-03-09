using Avalonia;
using System;
using System.IO;
using System.Reflection;
using WDE.Common.Tasks;

namespace CrashReport;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        FixCurrentDirectory();
        GlobalApplication.Arguments.Init(args);
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void FixCurrentDirectory()
    {
        #pragma warning disable IL3000
        var path = Assembly.GetExecutingAssembly().Location;
        #pragma warning restore IL3000
        if (string.IsNullOrEmpty(path))
            path = System.AppContext.BaseDirectory;
        var exePath = new FileInfo(path);
        if (exePath.Directory != null)
            Directory.SetCurrentDirectory(exePath.Directory.FullName);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont() 
            .LogToTrace();
}