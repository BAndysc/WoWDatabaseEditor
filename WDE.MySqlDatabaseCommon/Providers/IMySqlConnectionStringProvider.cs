namespace WDE.MySqlDatabaseCommon.Providers
{
    public interface IMySqlConnectionStringProvider
    {
        public string ConnectionString { get; }
        public string DatabaseName { get; }
    }
}