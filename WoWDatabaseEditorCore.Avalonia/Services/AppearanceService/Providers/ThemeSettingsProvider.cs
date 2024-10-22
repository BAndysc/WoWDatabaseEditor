using System;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeSettingsProvider : IThemeSettingsProvider
    {
        private readonly IUserSettings userSettings;

        public ThemeSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            Settings = userSettings.Get<ThemeSettings>();
            Settings = new ThemeSettings(
                Settings.Name,
                Settings.UseCustomScaling,
                Settings.CustomScaling,
                Settings.Hue,
                Remap(Settings.Saturation, -1, 1, 0, 1),
                Remap(Settings.Lightness, -1, 1, 0, 1),
                Settings.AprilsFoolOverride,
                Settings.ThemeVariant ?? "Classic");
        }

        private ThemeSettings Settings { get; set; }

        public ThemeSettings GetSettings()
        {
            return Settings;
        }

        public void UpdateSettings(Theme theme, double? customScaling, double hue, double saturation, double lightness, int? aprilsFoolOverride, string? themeVariant)
        {
            Settings = new ThemeSettings(theme.Name, customScaling.HasValue, customScaling ?? 1, hue, saturation, lightness, aprilsFoolOverride, themeVariant);
            userSettings.Update(new ThemeSettings(theme.Name, customScaling.HasValue, customScaling ?? 1, hue, Remap(saturation, 0, 1, -1, 1), Remap(lightness, 0, 1, -1, 1), aprilsFoolOverride, themeVariant));
        }

        private double Remap(double val, double min, double max, double newMin, double newMax)
        {
            val = Math.Clamp(val, min, max);
            return ((val - min) / (max - min)) * (newMax - newMin) + newMin;
        }
    }
}