using Avalonia;
using System;
using System.IO;
using System.Reflection;

namespace CrashReport;

class Program
{
    public static string[] Arguments = Array.Empty<string>();
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        FixCurrentDirectory();
        Arguments = args;
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
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
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont() 
            .LogToTrace();
}