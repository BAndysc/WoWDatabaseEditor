using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.SqlInterpreter;

namespace WDE.MySqlDatabaseCommon.Database;

public class WorldMySqlExecutor : BaseMySqlExecutor
{
    private readonly IDatabaseProvider databaseProvider;

    public WorldMySqlExecutor(IMySqlWorldConnectionStringProvider worldConnectionString,
        IDatabaseProvider databaseProvider,
        IQueryEvaluator queryEvaluator,
        IEventAggregator eventAggregator,
        IMainThread mainThread,
        DatabaseLogger databaseLogger) : base(worldConnectionString.ConnectionString,
        worldConnectionString.DatabaseName,
        databaseProvider,
        queryEvaluator,
        eventAggregator,
        mainThread,
        databaseLogger)
    {
        this.databaseProvider = databaseProvider;
    }

    public override bool IsConnected => databaseProvider.IsConnected;
}