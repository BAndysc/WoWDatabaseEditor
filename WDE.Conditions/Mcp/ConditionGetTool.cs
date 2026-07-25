using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;
using WDE.Conditions.Data;
using WDE.Conditions.ViewModels;
using WDE.Module.Attributes;

namespace WDE.Conditions.Mcp
{
    public class ConditionGetInput
    {
        [Description("conditions.SourceTypeOrReferenceId - the CONDITION_SOURCE_TYPE_* id (list them with trinity_condition_sources).")]
        public required int SourceType { get; set; }

        [Description("conditions.SourceGroup; its meaning depends on the source type (see trinity_condition_sources). Pass 0 when the source type does not use it.")]
        public required long SourceGroup { get; set; }

        [Description("conditions.SourceEntry; its meaning depends on the source type (see trinity_condition_sources). Pass 0 when the source type does not use it.")]
        public required int SourceEntry { get; set; }

        [Description("conditions.SourceId, used only by a few source types (e.g. smart_scripts). Default 0.")]
        public int? SourceId { get; set; }
    }

    public class ConditionGetOutput
    {
        public required int SourceType { get; set; }

        [Description("CONDITION_SOURCE_TYPE_* name, when known to the editor.")]
        public string? SourceTypeName { get; set; }

        public required long SourceGroup { get; set; }

        public required int SourceEntry { get; set; }

        public required int SourceId { get; set; }

        [Description("What this condition set gates, rendered from the source type description.")]
        public string? SourceReadable { get; set; }

        [Description("Number of condition rows found in the database.")]
        public required int TotalConditions { get; set; }

        [Description("The conditions grouped by ElseGroup: conditions inside one group are ANDed, the groups themselves are ORed (the whole set passes when any group passes). The same shape is accepted by trinity_condition_validate and trinity_condition_update. An empty list means no conditions exist - the source is unrestricted.")]
        public required List<List<McpConditionJson>> Groups { get; set; }

        [Description("The whole set as one readable sentence (groups joined with OR).")]
        public string? Readable { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    internal class ConditionGetTool : McpTool<ConditionGetInput, ConditionGetOutput>
    {
        private readonly Lazy<IDatabaseProvider> databaseProvider;
        private readonly Lazy<IConditionDataManager> dataManager;
        private readonly Lazy<IConditionsFactory> conditionsFactory;

        public ConditionGetTool(Lazy<IDatabaseProvider> databaseProvider,
            Lazy<IConditionDataManager> dataManager,
            Lazy<IConditionsFactory> conditionsFactory)
        {
            this.databaseProvider = databaseProvider;
            this.dataManager = dataManager;
            this.conditionsFactory = conditionsFactory;
        }

        public override string Name => "trinity_condition_get";

        public override string Description => "Loads the TrinityCore `conditions` rows attached to one source (SourceTypeOrReferenceId + SourceGroup/SourceEntry/SourceId) and returns them the way the conditions editor presents them: condition type names, named parameter values with resolved meanings, target labels and readable sentences, grouped by ElseGroup (conditions in a group are ANDed, groups are ORed). The returned 'groups' round-trip with trinity_condition_update. Vocabulary: trinity_condition_sources for source types, trinity_condition_types for condition types.";

        protected override async Task<ConditionGetOutput> Execute(ConditionGetInput input, CancellationToken token)
        {
            var mask = ConditionsMcpModelBuilder.GetMask(dataManager.Value, input.SourceType, input.SourceId.HasValue);
            var key = new IDatabaseProvider.ConditionKey(input.SourceType, input.SourceGroup, input.SourceEntry, input.SourceId ?? 0);

            IReadOnlyList<IConditionLine> lines;
            try
            {
                lines = await databaseProvider.Value.GetConditionsForAsync(mask, key);
            }
            catch (Exception e)
            {
                throw new McpToolException("Failed to load conditions from the database: " + e.Message);
            }

            var groups = ConditionsMcpModelBuilder.GroupConditions(input.SourceType, lines, conditionsFactory.Value, dataManager.Value);

            return new ConditionGetOutput
            {
                SourceType = input.SourceType,
                SourceTypeName = ConditionsMcpModelBuilder.GetSourceTypeName(dataManager.Value, input.SourceType),
                SourceGroup = input.SourceGroup,
                SourceEntry = input.SourceEntry,
                SourceId = input.SourceId ?? 0,
                SourceReadable = BuildSourceReadable(input),
                TotalConditions = lines.Count,
                Groups = groups,
                Readable = ConditionsMcpModelBuilder.BuildOverallReadable(groups)
            };
        }

        private string? BuildSourceReadable(ConditionGetInput input)
        {
            if (!dataManager.Value.HasConditionSourceData(input.SourceType))
                return null;
            var source = dataManager.Value.GetConditionSourceData(input.SourceType);
            try
            {
                return SmartFormat.Smart.Format(source.Description, new
                {
                    group = input.SourceGroup,
                    entry = input.SourceEntry,
                    sourceId = input.SourceId ?? 0
                });
            }
            catch (Exception)
            {
                return source.Description;
            }
        }
    }
}
