using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Demo;

namespace AvaloniaStyles
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow();
            base.OnFrameworkInitializationCompleted();
        }

        static void Main(string[] args) 
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        // App configuration, used by the entry point and previewer
        static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .With(new Win32PlatformOptions
                {
                    OverlayPopups = true,
                })
                .UsePlatformDetect()
                .LogToTrace();
    }
}