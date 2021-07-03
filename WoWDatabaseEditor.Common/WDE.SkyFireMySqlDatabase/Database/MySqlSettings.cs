using System.Collections.Generic;
using System.Linq;
using LinqToDB.Configuration;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SkyFireMySqlDatabase.Data;

namespace WDE.SkyFireMySqlDatabase.Database
{
    public class MySqlSettings : ILinqToDBSettings, IMySqlConnectionStringProvider
    {
        private readonly IDbAccess worldAccess;
        private readonly IDbAccess authAccess;

        public MySqlSettings(IDbAccess worldAccess, IDbAccess authAccess)
        {
            this.worldAccess = worldAccess;
            this.authAccess = authAccess;
            DatabaseName = worldAccess.Database ?? "";
            ConnectionStrings = new[]
            {
                new ConnectionStringSettings
                {
                    Name = "SkyFire",
                    ProviderName = "MySqlConnector",
                    ConnectionString = ConnectionString
                },
                new ConnectionStringSettings
                {
                    Name = "SkyFireAuth",
                    ProviderName = "MySqlConnector",
                    ConnectionString = AuthConnectionString
                }
            };
        }

        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "MySqlConnector";
        public string DefaultDataProvider => "MySqlConnector";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }

        public string ConnectionString =>
            $"Server={worldAccess.Host};Port={worldAccess.Port ?? 3306};Database={worldAccess.Database};Uid={worldAccess.User};Pwd={worldAccess.Password};AllowUserVariables=True";

        public string AuthConnectionString =>
            $"Server={authAccess.Host};Port={authAccess.Port ?? 3306};Database={authAccess.Database};Uid={authAccess.User};Pwd={authAccess.Password};AllowUserVariables=True";

        public string DatabaseName { get; }
    }
}