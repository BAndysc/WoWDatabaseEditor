namespace WDE.MySqlDatabaseCommon.Providers
{
    public interface IMySqlAuthConnectionStringProvider
    {
        public string ConnectionString { get; }
        public string DatabaseName { get; }
    }
}