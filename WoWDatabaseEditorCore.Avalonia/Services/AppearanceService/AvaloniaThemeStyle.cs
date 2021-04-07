using System;
using AvaloniaStyles;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService
{
    public class AvaloniaThemeStyle : SystemTheme
    {
        internal static SystemThemeOptions Theme = SystemThemeOptions.Auto;
        
        public AvaloniaThemeStyle(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Mode = Theme;
        }
    }
}