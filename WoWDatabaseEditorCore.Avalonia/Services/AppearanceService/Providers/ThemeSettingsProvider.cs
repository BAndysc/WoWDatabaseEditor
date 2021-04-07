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
        }

        private ThemeSettings Settings { get; set; }

        public ThemeSettings GetSettings()
        {
            return Settings;
        }

        public void UpdateSettings(Theme theme)
        {
            userSettings.Update(new ThemeSettings(theme.Name));
        }
    }
}