using System.IO;
using System.Windows;
using Newtonsoft.Json;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class ConnectionSettingsProvider : IConnectionSettingsProvider
    {
        public ConnectionSettingsProvider()
        {
            DbAccess? access = null;
            if (File.Exists("database.json"))
            {
                JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
                using (StreamReader re = new("database.json"))
                {
                    JsonTextReader reader = new(re);
                    access = ser.Deserialize<DbAccess>(reader);
                }
            }

            if (access == null)
                access = new DbAccess();

            DbAccess = access;
        }

        private DbAccess DbAccess { get; }

        public DbAccess GetSettings()
        {
            return DbAccess;
        }

        public void UpdateSettings(string? user, string? password, string? host, int? port, string? database)
        {
            DbAccess.Database = database;
            DbAccess.User = user;
            DbAccess.Password = password;
            DbAccess.Host = host;
            DbAccess.Port = port;
            JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
            using (StreamWriter file = File.CreateText(@"database.json"))
            {
                ser.Serialize(file, DbAccess);
            }
        }
    }
}