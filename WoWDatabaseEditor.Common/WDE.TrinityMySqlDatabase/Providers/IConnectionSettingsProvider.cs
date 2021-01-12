using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    public interface IConnectionSettingsProvider
    {
        DbAccess GetSettings();
        void UpdateSettings(string? user, string? password, string? host, int? port, string? database);
    }
}