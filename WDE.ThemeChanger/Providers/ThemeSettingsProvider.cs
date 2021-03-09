using System.IO;
using Newtonsoft.Json;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.ThemeChanger.Data;

namespace WDE.ThemeChanger.Providers
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