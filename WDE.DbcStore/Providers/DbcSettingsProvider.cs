using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.DbcStore.Data;

namespace WDE.DbcStore.Providers
{
    [AutoRegister, SingleInstance]
    public class DbcSettingsProvider : IDbcSettingsProvider
    {
        private DBCSettings DBCSettings { get; set; }

        public DbcSettingsProvider()
        {
            if (File.Exists("dbc.json"))
            {
                JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
                using (StreamReader re = new StreamReader("dbc.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    DBCSettings = ser.Deserialize<DBCSettings>(reader);
                }
            }
            
            if (DBCSettings == null)
                DBCSettings = new DBCSettings();
        }

        public DBCSettings GetSettings() => DBCSettings;

        public void UpdateSettings(DBCSettings newSettings)
        {
            DBCSettings = newSettings;
            JsonSerializer ser = new JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
            using (StreamWriter file = File.CreateText(@"dbc.json"))
            {
                ser.Serialize(file, DBCSettings);
            }
            System.Windows.MessageBox.Show("Restart the application.");
        }
    }
}
