namespace WDE.MySqlDatabaseCommon.Providers
{
    public interface IMySqlWorldConnectionStringProvider
    {
        public string ConnectionString { get; }
        public string DatabaseName { get; }
        public bool IsEmpty { get; }
    }
}