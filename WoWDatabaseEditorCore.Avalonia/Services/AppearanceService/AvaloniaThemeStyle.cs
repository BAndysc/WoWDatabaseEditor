using System;
using AvaloniaStyles;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService
{
    public class AvaloniaThemeStyle : SystemTheme
    {
        internal static SystemThemeOptions Theme = SystemThemeOptions.Auto;

        internal static bool UseDock =>
            Theme == SystemThemeOptions.Windows10Dark || Theme == SystemThemeOptions.Windows10Light;
        
        public AvaloniaThemeStyle(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Mode = Theme;
        }
    }
}