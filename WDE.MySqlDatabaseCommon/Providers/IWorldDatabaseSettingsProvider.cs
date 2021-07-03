namespace WDE.MySqlDatabaseCommon.Providers
{
    public interface IWorldDatabaseSettingsProvider
    {
        IDbAccess Settings { get; set; }
    }
}