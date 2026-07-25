using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;
using WDE.Conditions.Data;
using WDE.Module.Attributes;

namespace WDE.Conditions.Mcp
{
    public class ConditionSourcesInput
    {
        [Description("Optional case-insensitive substring filter matched against the CONDITION_SOURCE_TYPE_* name and description.")]
        public string? Filter { get; set; }

        [Description("Optional exact source type id to return a single source.")]
        public int? Id { get; set; }
    }

    public class ConditionSourceTargetJson
    {
        [Description("ConditionTarget value.")]
        public required int Id { get; set; }

        [Description("What object the condition is checked on.")]
        public required string Label { get; set; }

        public string? Comment { get; set; }
    }

    public class ConditionSourceKeyColumnJson
    {
        [Description("What the editor calls this key column.")]
        public required string Name { get; set; }

        public string? Description { get; set; }

        [Description("Editor parameter type hinting what the value is (ItemParameter = item id, QuestParameter = quest id...).")]
        public string? ValueKind { get; set; }
    }

    public class ConditionSourceJson
    {
        [Description("conditions.SourceTypeOrReferenceId value.")]
        public required int Id { get; set; }

        [Description("CONDITION_SOURCE_TYPE_* enum name.")]
        public required string Name { get; set; }

        [Description("What the conditions of this source gate ({group}/{entry}/{sourceId} refer to the key columns).")]
        public required string Description { get; set; }

        [Description("Valid ConditionTarget values for this source type.")]
        public required List<ConditionSourceTargetJson> Targets { get; set; }

        [Description("Meaning of conditions.SourceGroup for this source; null when the column is unused (pass 0 then).")]
        public ConditionSourceKeyColumnJson? SourceGroup { get; set; }

        [Description("Meaning of conditions.SourceEntry for this source; null when the column is unused (pass 0 then).")]
        public ConditionSourceKeyColumnJson? SourceEntry { get; set; }

        [Description("Meaning of conditions.SourceId for this source; null when the column is unused (pass 0 then).")]
        public ConditionSourceKeyColumnJson? SourceId { get; set; }

        [Description("Which key columns identify a condition set of this source type (the editor's delete/replace scope).")]
        public required List<string> KeyColumns { get; set; }
    }

    public class ConditionSourcesOutput
    {
        [Description("Number of returned source types.")]
        public required int Total { get; set; }

        public required List<ConditionSourceJson> Sources { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    internal class ConditionSourcesTool : McpTool<ConditionSourcesInput, ConditionSourcesOutput>
    {
        private readonly Lazy<IConditionDataManager> dataManager;

        public ConditionSourcesTool(Lazy<IConditionDataManager> dataManager)
        {
            this.dataManager = dataManager;
        }

        public override string Name => "trinity_condition_sources";

        public override string Description => "Lists the TrinityCore condition source types (CONDITION_SOURCE_TYPE_* - what a `conditions` row can be attached to) with the editor's meaning of the SourceGroup/SourceEntry/SourceId key columns and the valid ConditionTarget values per source. Use these ids and key column meanings for trinity_condition_get/validate/update; condition types themselves are listed by trinity_condition_types.";

        protected override Task<ConditionSourcesOutput> Execute(ConditionSourcesInput input, CancellationToken token)
        {
            IEnumerable<ConditionSourcesJsonData> filtered = dataManager.Value.AllConditionSourceData.OrderBy(s => s.Id);
            if (input.Id.HasValue)
                filtered = filtered.Where(s => s.Id == input.Id.Value);
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.Trim();
                filtered = filtered.Where(s =>
                    s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (s.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var sources = filtered.Select(ToJson).ToList();
            if (sources.Count == 0)
                throw new McpToolException(input.Id.HasValue
                    ? $"No condition source type with id {input.Id.Value}."
                    : $"No condition source type matches '{input.Filter}'. Call the tool without a filter to list all source types.");

            return Task.FromResult(new ConditionSourcesOutput
            {
                Total = sources.Count,
                Sources = sources
            });
        }

        private static ConditionSourceJson ToJson(ConditionSourcesJsonData data)
        {
            var targets = new List<ConditionSourceTargetJson>();
            if (data.Targets is { } definedTargets)
            {
                foreach (var target in definedTargets.OrderBy(t => t.Key))
                    targets.Add(new ConditionSourceTargetJson
                    {
                        Id = target.Key,
                        Label = target.Value.Description,
                        Comment = target.Value.Comment != target.Value.Description ? target.Value.Comment : null
                    });
            }

            var mask = data.GetMask();
            var keyColumns = new List<string>();
            if (mask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceGroup))
                keyColumns.Add("SourceGroup");
            if (mask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceEntry))
                keyColumns.Add("SourceEntry");
            if (mask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceId))
                keyColumns.Add("SourceId");

            return new ConditionSourceJson
            {
                Id = data.Id,
                Name = data.Name,
                Description = data.Description,
                Targets = targets,
                SourceGroup = ToKeyColumn(data.Group),
                SourceEntry = ToKeyColumn(data.Entry),
                SourceId = ToKeyColumn(data.SourceId),
                KeyColumns = keyColumns
            };
        }

        private static ConditionSourceKeyColumnJson? ToKeyColumn(ConditionSourceParamsJsonData param)
        {
            if (string.IsNullOrEmpty(param.Name))
                return null;
            return new ConditionSourceKeyColumnJson
            {
                Name = param.Name,
                Description = string.IsNullOrEmpty(param.Description) ? null : param.Description,
                ValueKind = string.IsNullOrEmpty(param.Type) ? null : param.Type
            };
        }
    }
}
