using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    [AutoRegister, SingleInstance]
    public class ConnectionSettingsProvider : IConnectionSettingsProvider
    {
        private DbAccess DbAccess { get; }

        public ConnectionSettingsProvider()
        {
            if (System.IO.File.Exists("database.json"))
            {
                JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
                using (StreamReader re = new StreamReader("database.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    DbAccess = ser.Deserialize<DbAccess>(reader);
                }
            }

            if (DbAccess == null)
                DbAccess = new DbAccess();
        }

        public DbAccess GetSettings() => DbAccess;

        public void UpdateSettings(string user, string password, string host, string database)
        {
            DbAccess.DB = database;
            DbAccess.User = user;
            DbAccess.Password = password;
            DbAccess.Host = host;
            JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
            using (StreamWriter file = File.CreateText(@"database.json"))
            {
                ser.Serialize(file, DbAccess);
            }
            System.Windows.MessageBox.Show("Restart the application.");
        }
    }
}
