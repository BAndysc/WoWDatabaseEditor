using System.Collections.Generic;
using System.Linq;
using LinqToDB.Configuration;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.CMMySqlDatabase.Database
{
    public class MySqlWorldSettings : ILinqToDBSettings, IMySqlWorldConnectionStringProvider
    {
        private readonly IDbAccess worldAccess;
        private readonly IDbAccess authAccess;

        public MySqlWorldSettings(IDbAccess worldAccess, IDbAccess authAccess)
        {
            this.worldAccess = worldAccess;
            this.authAccess = authAccess;
            DatabaseName = worldAccess.Database ?? "";
            ConnectionStrings = new[]
            {
                new ConnectionStringSettings
                {
                    Name = "CMaNGOS-WoTLK-World",
                    ProviderName = "MySqlConnector",
                    ConnectionString = ConnectionString
                },
                new ConnectionStringSettings
                {
                    Name = "CMaNGOS-WoTLK-Auth",
                    ProviderName = "MySqlConnector",
                    ConnectionString = AuthConnectionString
                }
            };
        }

        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "MySqlConnector";
        public string DefaultDataProvider => "MySqlConnector";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }

        public bool IsEmpty => string.IsNullOrEmpty(authAccess.Host) || string.IsNullOrEmpty(authAccess.Database) || string.IsNullOrEmpty(authAccess.User);
        
        public string ConnectionString =>
            $"Server={worldAccess.Host};Port={worldAccess.Port ?? 3306};Database={worldAccess.Database};Uid={worldAccess.User};Pwd={worldAccess.Password};AllowUserVariables=True;TreatTinyAsBoolean=False";

        public string AuthConnectionString =>
            $"Server={authAccess.Host};Port={authAccess.Port ?? 3306};Database={authAccess.Database};Uid={authAccess.User};Pwd={authAccess.Password};AllowUserVariables=True;TreatTinyAsBoolean=False";

        public string DatabaseName { get; }
    }
}