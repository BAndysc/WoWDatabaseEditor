using System.IO;
using System.Windows;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class ConnectionSettingsProvider : IConnectionSettingsProvider
    {
        private readonly IUserSettings userSettings;

        public ConnectionSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            DbAccess? access = userSettings.Get<DbAccess?>();
            DbAccess = access ?? new DbAccess();
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
            userSettings.Update(DbAccess);
        }
    }
}