using Avalonia;

namespace AvaloniaStyles.Desktop;

public class Program
{
    static void Main(string[] args) 
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    
    // App configuration, used by the entry point and previewer
    static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}