namespace WDE.MySqlDatabaseCommon.Providers;

public interface IHotfixDatabaseSettingsProvider
{
    IDbAccess Settings { get; set; }
}