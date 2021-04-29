using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class DatabaseSettingsProvider : IDatabaseSettingsProvider, IMySqlConnectionStringProvider
    {
        private readonly IUserSettings userSettings;

        public DatabaseSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
        }

        public string ConnectionString => $"Server={Settings.Host};Port={Settings.Port ?? 3306};Database={Settings.Database};Uid={Settings.User};Pwd={Settings.Password};AllowUserVariables=True";
        public string DatabaseName => Settings.Database ?? "";

        public DbAccess Settings
        {
            get => userSettings.Get<DbAccess>(DbAccess.Default);
            set => userSettings.Update(value);
        }
    }
}