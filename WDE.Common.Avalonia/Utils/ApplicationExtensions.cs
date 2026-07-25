using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia.Utils;

public static class ApplicationExtensions
{
    public static TopLevel? GetTopLevel(this Application app)
    {
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }
        if (app.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
        {
            return TopLevel.GetTopLevel(viewApp.MainView);
        }
        return null;
    }
}