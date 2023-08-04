using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.SqlInterpreter;

namespace WDE.MySqlDatabaseCommon.Database;

public class HotfixMySqlExecutor : BaseMySqlExecutor
{
    private readonly IMySqlHotfixConnectionStringProvider hotfixConnectionString;

    public HotfixMySqlExecutor(IMySqlHotfixConnectionStringProvider hotfixConnectionString,
        IDatabaseProvider databaseProvider,
        IQueryEvaluator queryEvaluator,
        IEventAggregator eventAggregator,
        IMainThread mainThread,
        DatabaseLogger databaseLogger) : base(hotfixConnectionString.ConnectionString,
        hotfixConnectionString.DatabaseName,
        databaseProvider,
        queryEvaluator,
        eventAggregator,
        mainThread,
        databaseLogger)
    {
        this.hotfixConnectionString = hotfixConnectionString;
    }

    public override bool IsConnected => !hotfixConnectionString.IsEmpty;
}