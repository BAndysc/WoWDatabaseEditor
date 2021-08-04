using WDE.Common.Services;
using WDE.DbcStore.Data;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class DbcSettingsProvider : IDbcSettingsProvider
    {
        private readonly IUserSettings userSettings;

        public DbcSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            DBCSettings = userSettings.Get<DBCSettings>();
        }

        private DBCSettings DBCSettings { get; set; }

        public DBCSettings GetSettings()
        {
            return DBCSettings;
        }

        public void UpdateSettings(DBCSettings newSettings)
        {
            DBCSettings = newSettings;
            userSettings.Update(DBCSettings);
        }
    }
}