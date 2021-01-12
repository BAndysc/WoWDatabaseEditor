using System.IO;
using System.Windows;
using Newtonsoft.Json;
using WDE.DbcStore.Data;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class DbcSettingsProvider : IDbcSettingsProvider
    {
        public DbcSettingsProvider()
        {
            if (File.Exists("dbc.json"))
            {
                JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
                using (StreamReader re = new("dbc.json"))
                {
                    JsonTextReader reader = new(re);
                    DBCSettings = ser.Deserialize<DBCSettings>(reader);
                }
            }

            if (DBCSettings == null)
                DBCSettings = new DBCSettings();
        }

        private DBCSettings DBCSettings { get; set; }

        public DBCSettings GetSettings()
        {
            return DBCSettings;
        }

        public void UpdateSettings(DBCSettings newSettings)
        {
            DBCSettings = newSettings;
            JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
            using (StreamWriter file = File.CreateText(@"dbc.json"))
            {
                ser.Serialize(file, DBCSettings);
            }

            MessageBox.Show("Restart the application.");
        }
    }
}