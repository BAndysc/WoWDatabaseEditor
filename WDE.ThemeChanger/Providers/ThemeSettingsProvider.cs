using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.ThemeChanger.Data;
using System.IO;
using WDE.Module.Attributes;
using System.Windows;
using Newtonsoft.Json;
using WDE.Common.Managers;

namespace WDE.ThemeChanger.Providers
{
    [AutoRegister, SingleInstance]
    public class ThemeSettingsProvider : IThemeSettingsProvider
    {
        private ThemeSettings settings { get; set;  }

        public ThemeSettings GetSettings() => settings;

        public ThemeSettingsProvider()
        {
            settings = new ThemeSettings(null);
            if (File.Exists("theme.json"))
            {
                JsonSerializer ser = new JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
                using (StreamReader re = new StreamReader("theme.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    settings = ser.Deserialize<ThemeSettings>(reader);
                }
            }
        }

        public void UpdateSettings(Theme theme)
        {
            settings = new ThemeSettings(theme.Name);
            JsonSerializer ser = new JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
            using (StreamWriter file = File.CreateText(@"theme.json"))
            {
                ser.Serialize(file, settings);
            }
        }
    }
}
