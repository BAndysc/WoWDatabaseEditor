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
                if (EffectiveThemeIsDark)
                {
                    // values around 0 looks bad, so let's change them to look better
                    if (value.L >= 0.45f && value.L <= 0.5f)
                    {
                        value.L = 0.45f;
                    }
                    else if (value.L >= 0.5f && value.L < 0.55f)
                    {
                        value.L = 0.55f;
                    }
                }
                Application.Current!.Resources["AccentHue"] = value;
            }
        }

        public AvaloniaThemeStyle(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Mode = Theme;
        }
    }
}
