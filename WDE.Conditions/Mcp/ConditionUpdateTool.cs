using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;
using WDE.Conditions.Data;
using WDE.Conditions.ViewModels;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Conditions.Mcp
{
    public class ConditionUpdateInput
    {
        [Description("conditions.SourceTypeOrReferenceId - the CONDITION_SOURCE_TYPE_* id (list them with trinity_condition_sources).")]
        public required int SourceType { get; set; }

        [Description("conditions.SourceGroup; its meaning depends on the source type (see trinity_condition_sources). Pass 0 when the source type does not use it.")]
        public required long SourceGroup { get; set; }

        [Description("conditions.SourceEntry; its meaning depends on the source type (see trinity_condition_sources). Pass 0 when the source type does not use it.")]
        public required int SourceEntry { get; set; }

        [Description("conditions.SourceId, used only by a few source types (e.g. smart_scripts). Default 0.")]
        public int? SourceId { get; set; }

        [Description("The full new condition set in the same shape trinity_condition_get returns: conditions inside one group are ANDed, groups are ORed. It REPLACES all conditions of this source; an empty list deletes them (making the source unrestricted). Tip: call trinity_condition_get first and modify its output.")]
        public required List<List<McpConditionJson>> Groups { get; set; }

        [Description("When true, the generated SQL is executed against the world database. When false (default), the SQL is only returned for review.")]
        public bool Execute { get; set; }
    }

    public class ConditionUpdateOutput
    {
        [Description("The generated DELETE+INSERT SQL replacing the conditions of this source - the same query the editor's conditions dialog would produce on save.")]
        public required string Sql { get; set; }

        [Description("True when the SQL was executed against the world database.")]
        public required bool Executed { get; set; }

        [Description("Number of condition rows the SQL inserts.")]
        public required int InsertedRows { get; set; }

        [Description("Validation warnings found in the input (non-blocking; errors would have aborted the tool).")]
        public required List<McpConditionProblemJson> Warnings { get; set; }

        [Description("Extra information about what happened and what to do next.")]
        public string? Note { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    internal class ConditionUpdateTool : McpTool<ConditionUpdateInput, ConditionUpdateOutput>
    {
        private readonly Lazy<IConditionDataManager> dataManager;
        private readonly Lazy<IConditionsFactory> conditionsFactory;
        private readonly Lazy<IConditionQueryGenerator> queryGenerator;
        private readonly Lazy<IMySqlExecutor> mySqlExecutor;
        private readonly Lazy<ICurrentCoreVersion> currentCoreVersion;

        public ConditionUpdateTool(Lazy<IConditionDataManager> dataManager,
            Lazy<IConditionsFactory> conditionsFactory,
            Lazy<IConditionQueryGenerator> queryGenerator,
            Lazy<IMySqlExecutor> mySqlExecutor,
            Lazy<ICurrentCoreVersion> currentCoreVersion)
        {
            this.dataManager = dataManager;
            this.conditionsFactory = conditionsFactory;
            this.queryGenerator = queryGenerator;
            this.mySqlExecutor = mySqlExecutor;
            this.currentCoreVersion = currentCoreVersion;
        }

        public override string Name => "trinity_condition_update";

        public override string Description => "Replaces the whole TrinityCore condition set of one source (SourceTypeOrReferenceId + SourceGroup/SourceEntry/SourceId) with the given groups (same JSON shape trinity_condition_get returns) and generates the DELETE+INSERT SQL the conditions editor would produce, including editor-style readable comments. The input is validated first (like trinity_condition_validate); errors abort without SQL. With execute=true the SQL is run against the world database, otherwise (default) it is only returned for review.";

        public override bool Mutating => true;

        protected override async Task<ConditionUpdateOutput> Execute(ConditionUpdateInput input, CancellationToken token)
        {
            if (input.Groups == null!)
                throw new McpToolException("Missing required 'groups' array (use [] to delete all conditions of the source).");

            var coreHasStringValue = currentCoreVersion.Value.Current.ConditionFeatures.HasConditionStringValue;
            var problems = ConditionsMcpModelBuilder.Validate(input.SourceType, input.Groups, dataManager.Value, coreHasStringValue);
            var errors = problems.Where(p => p.Severity == McpConditionProblemSeverity.Error).ToList();
            if (errors.Count > 0)
                throw new McpToolException("The conditions are invalid, no SQL was generated:\n" +
                    string.Join("\n", errors.Select(e => e.Group.HasValue ? $"[group {e.Group}, condition {e.Index}] {e.Message}" : e.Message)));

            var lines = ConditionsMcpModelBuilder.BuildConditionLines(input.SourceType, input.SourceGroup,
                input.SourceEntry, input.SourceId ?? 0, input.Groups, conditionsFactory.Value, dataManager.Value);

            var mask = ConditionsMcpModelBuilder.GetMask(dataManager.Value, input.SourceType, input.SourceId.HasValue);
            var key = new IDatabaseProvider.ConditionKey(input.SourceType, input.SourceGroup, input.SourceEntry, input.SourceId ?? 0);

            // same query shape the editor's conditions dialog produces on save (ConditionEditService)
            var transaction = Queries.BeginTransaction(DataDatabaseType.World);
            transaction.Add(queryGenerator.Value.BuildDeleteQuery(key.WithMask(mask)));
            if (lines.Count > 0)
                transaction.Add(queryGenerator.Value.BuildInsertQuery(lines));
            var query = transaction.Close();

            bool executed = false;
            if (input.Execute)
            {
                try
                {
                    await mySqlExecutor.Value.ExecuteSql(query);
                    executed = true;
                }
                catch (IMySqlExecutor.DatabaseExecutorException e)
                {
                    throw new McpToolException("Failed to execute the query: " + (e.InnerException?.Message ?? e.Message));
                }
            }

            return new ConditionUpdateOutput
            {
                Sql = query.QueryString,
                Executed = executed,
                InsertedRows = lines.Count,
                Warnings = problems,
                Note = executed
                    ? (lines.Count == 0 ? "All conditions of the source were deleted." : null)
                    : "The SQL was only generated, not executed. Pass execute=true to run it against the world database."
            };
        }
    }
}
