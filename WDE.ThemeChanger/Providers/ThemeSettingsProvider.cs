using System.IO;
using Newtonsoft.Json;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.ThemeChanger.Data;

namespace WDE.ThemeChanger.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeSettingsProvider : IThemeSettingsProvider
    {
        public ThemeSettingsProvider()
        {
            Settings = new ThemeSettings(null);
            if (File.Exists("theme.json"))
            {
                JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
                using (StreamReader re = new("theme.json"))
                {
                    JsonTextReader reader = new(re);
                    Settings = ser.Deserialize<ThemeSettings>(reader);
                }
            }
        }

        private ThemeSettings Settings { get; set; }

        public ThemeSettings GetSettings()
        {
            return Settings;
        }

        public void UpdateSettings(Theme theme)
        {
            Settings = new ThemeSettings(theme.Name);
            JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
            using (StreamWriter file = File.CreateText(@"theme.json"))
            {
                ser.Serialize(file, Settings);
            }
        }
    }
}