using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    public interface IDatabaseSettingsProvider
    {
        DbAccess Settings { get; set; }
    }
}