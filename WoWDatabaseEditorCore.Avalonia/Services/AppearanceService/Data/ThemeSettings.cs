using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data
{
    public struct ThemeSettings : ISettings
    {
        public string Name { get; }
        
        public bool UseCustomScaling { get; }

        public double CustomScaling { get; }
        
        public double Hue { get; } /* 0 --- 1 */
        
        public double Saturation { get; } /* -1 --- 1 */
        
        public double Lightness { get; } /* -1 --- 1 */

        public int? AprilsFoolOverride { get; }

        public string? ThemeVariant { get; }

        public ThemeSettings(string name, bool useCustomScaling, double customScaling, double hue, double saturation, double lightness, int? aprilsFoolOverride, string? themeVariant)
        {
            Name = name;
            UseCustomScaling = useCustomScaling;
            CustomScaling = customScaling;
            Hue = hue;
            Saturation = saturation;
            Lightness = lightness;
            AprilsFoolOverride = aprilsFoolOverride;
            ThemeVariant = themeVariant;
        }
    }
}