using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Services
{
    // Finds dbscript steps that reference a searched entity (creature/GO/spell/quest/…) across all
    // 10 dbscripts_on_* tables, matching the mapped destination columns of parameters whose type
    // corresponds to the searched entity. Mirrors the EventAI find-anywhere source.
    [AutoRegister]
    [SingleInstance]
    public class DbScriptFindAnywhereSource : IFindAnywhereSource
    {
        private readonly IDbScriptDataManager dataManager;
        private readonly IMySqlExecutor executor;
        private readonly ISolutionItemNameRegistry nameRegistry;
        private readonly ISolutionItemIconRegistry iconRegistry;

        public DbScriptFindAnywhereSource(
            IDbScriptDataManager dataManager,
            IMySqlExecutor executor,
            ISolutionItemNameRegistry nameRegistry,
            ISolutionItemIconRegistry iconRegistry)
        {
            this.dataManager = dataManager;
            this.executor = executor;
            this.nameRegistry = nameRegistry;
            this.iconRegistry = iconRegistry;
        }

        public FindAnywhereSourceType SourceType => FindAnywhereSourceType.Other;

        public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType,
            IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken)
        {
            if (!executor.IsConnected)
                return;

            var columnToCommands = DbScriptReferenceQuery.MapColumns(dataManager.AllCommands, parameterNames);
            if (columnToCommands.Count == 0)
                return;

            foreach (var info in DbScriptTypes.All)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var sql = DbScriptReferenceQuery.BuildTableQuery(info.TableName, columnToCommands, parameterValue);
                if (sql == null)
                    continue;

                IDatabaseSelectResult result;
                try
                {
                    result = await executor.ExecuteSelectSql(sql);
                }
                catch
                {
                    // a table absent on this core (e.g. classic lacking some columns) must not abort
                    continue;
                }

                var idColumn = result.ColumnIndex("id");
                for (var row = 0; row < result.Rows; row++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    var id = result.Value<long>(row, idColumn);
                    var item = new DbScriptSolutionItem(info.Type, (uint)id);
                    resultContext.AddResult(new FindAnywhereResult(
                        iconRegistry.GetIcon(item),
                        id,
                        nameRegistry.GetName(item),
                        info.ReadableName,
                        item));
                }
            }
        }
    }
}
