using System;
using Avalonia;
using AvaloniaStyles;
using AvaloniaStyles.Utils;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService
{
    public class AvaloniaThemeStyle : SystemTheme
    {
        public static double BaseHue = 0.7f;
        internal static SystemThemeOptions Theme = SystemThemeOptions.LightWindows11;

        internal static HslDiff AccentHue
        {
            set
            {
                Application.Current.Resources["AccentHue"] = value;
            }
        }

        internal static bool UseDock
        {
            get
            {
                var effectiveTheme = ResolveTheme(Theme);
                return effectiveTheme is SystemThemeOptions.DarkWindows10 or SystemThemeOptions.DarkWindows11 or SystemThemeOptions.LightWindows10 or SystemThemeOptions.LightWindows11;
            }
        }
        
        public AvaloniaThemeStyle(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Mode = Theme;
        }
    }
}