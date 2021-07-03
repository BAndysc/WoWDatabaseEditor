namespace WDE.MySqlDatabaseCommon.Providers
{
    public interface IAuthDatabaseSettingsProvider
    {
        IDbAccess Settings { get; set; }
    }
}