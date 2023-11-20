using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlInterpreter;

namespace WDE.MySqlDatabaseCommon.Providers;

[AutoRegister]
[SingleInstance]
public class QueryEvaluator : BaseQueryEvaluator
{
    public QueryEvaluator(IHotfixDatabaseSettingsProvider hotfixSettings,
        IWorldDatabaseSettingsProvider worldSettings) : base(worldSettings.Settings.Database, hotfixSettings.Settings.Database, DataDatabaseType.World)
    {
    }
}