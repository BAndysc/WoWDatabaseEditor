namespace WDE.MySqlDatabaseCommon.Providers;

public interface IMySqlHotfixConnectionStringProvider
{
    public string ConnectionString { get; }
    public string DatabaseName { get; }
    public bool IsEmpty { get; }
}