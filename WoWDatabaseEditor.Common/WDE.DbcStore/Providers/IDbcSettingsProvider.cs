using WDE.DbcStore.Data;

namespace WDE.DbcStore.Providers
{
    public interface IDbcSettingsProvider
    {
        DBCSettings GetSettings();
        void UpdateSettings(DBCSettings newSettings);
    }
}