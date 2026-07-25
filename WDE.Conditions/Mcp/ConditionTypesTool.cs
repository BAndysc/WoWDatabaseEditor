using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.Mcp;
using WDE.Conditions.Data;
using WDE.Module.Attributes;

namespace WDE.Conditions.Mcp
{
    public class ConditionTypesInput
    {
        [Description("Optional case-insensitive substring filter matched against the CONDITION_* name, readable name, description and tags.")]
        public string? Filter { get; set; }

        [Description("Optional exact condition type id to return a single type.")]
        public int? Id { get; set; }
    }

    public class ConditionTypeParameterValueJson
    {
        [Description("Numeric value to store in the ConditionValueN column.")]
        public required long Value { get; set; }

        [Description("Name of the option.")]
        public required string Name { get; set; }

        public string? Description { get; set; }
    }

    public class ConditionTypeParameterJson
    {
        [Description("1-based slot: this parameter is stored in ConditionValue<slot>.")]
        public required int Slot { get; set; }

        [Description("Parameter name shown by the editor; also accepted as the 'name' of a parameter value in trinity_condition_update input.")]
        public required string Name { get; set; }

        public string? Description { get; set; }

        [Description("Editor parameter type hinting what the value is (SpellParameter = spell id, ItemParameter = item id, BoolParameter = 0/1...).")]
        public string? ValueKind { get; set; }

        [Description("When set, the value should be one of these options (or an OR-ed combination for flag parameters).")]
        public List<ConditionTypeParameterValueJson>? Values { get; set; }
    }

    public class ConditionTypeStringParameterJson
    {
        [Description("String parameter name (stored in ConditionStringValue1; only cores like TrinityCore master support it).")]
        public required string Name { get; set; }

        public string? Description { get; set; }
    }

    public class ConditionTypeJson
    {
        [Description("ConditionTypeOrReference value.")]
        public required int Id { get; set; }

        [Description("CONDITION_* enum name.")]
        public required string Name { get; set; }

        [Description("Short readable name the editor shows.")]
        public required string ReadableName { get; set; }

        [Description("What the condition checks.")]
        public string? Help { get; set; }

        [Description("The editor's sentence template ({target}, {pram1}..{pram3}, {negate:...} placeholders) - shows how parameters combine into a readable sentence.")]
        public required string DescriptionTemplate { get; set; }

        [Description("Sentence template used when the condition is negated (when null, {negate:...} inside descriptionTemplate handles negation).")]
        public string? NegatedDescriptionTemplate { get; set; }

        public List<string>? Tags { get; set; }

        [Description("Named parameters (ConditionValue1..3). Missing slots are unused and must stay 0.")]
        public List<ConditionTypeParameterJson>? Parameters { get; set; }

        public List<ConditionTypeStringParameterJson>? StringParameters { get; set; }
    }

    public class ConditionTypesOutput
    {
        [Description("Number of returned condition types.")]
        public required int Total { get; set; }

        public required List<ConditionTypeJson> Types { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    internal class ConditionTypesTool : McpTool<ConditionTypesInput, ConditionTypesOutput>
    {
        private readonly Lazy<IConditionDataManager> dataManager;

        public ConditionTypesTool(Lazy<IConditionDataManager> dataManager)
        {
            this.dataManager = dataManager;
        }

        public override string Name => "trinity_condition_types";

        public override string Description => "Lists the TrinityCore condition types (CONDITION_*) the way the conditions editor understands them: id, name, what the condition checks, named parameters (meaning of ConditionValue1..3 per type, with enum/flag options where defined) and the readable sentence template. Use this vocabulary with trinity_condition_get/validate/update. Note: OR between conditions is not a type - it is expressed by separate groups in the trinity_condition_get/update JSON. Source types (CONDITION_SOURCE_TYPE_*) are listed by trinity_condition_sources.";

        protected override Task<ConditionTypesOutput> Execute(ConditionTypesInput input, CancellationToken token)
        {
            var all = dataManager.Value.AllConditionData
                .Where(c => c.Id >= 0) // skip the CONDITION_LOGICAL_OR UI marker
                .OrderBy(c => c.Id);

            IEnumerable<ConditionJsonData> filtered = all;
            if (input.Id.HasValue)
                filtered = filtered.Where(c => c.Id == input.Id.Value);
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.Trim();
                filtered = filtered.Where(c =>
                    c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    c.NameReadable.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (c.Help?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (c.Tags?.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)) ?? false));
            }

            var types = filtered.Select(ToJson).ToList();
            if (types.Count == 0)
                throw new McpToolException(input.Id.HasValue
                    ? $"No condition type with id {input.Id.Value}."
                    : $"No condition type matches '{input.Filter}'. Call the tool without a filter to list all types.");

            return Task.FromResult(new ConditionTypesOutput
            {
                Total = types.Count,
                Types = types
            });
        }

        private static ConditionTypeJson ToJson(ConditionJsonData data)
        {
            List<ConditionTypeParameterJson>? parameters = null;
            if (data.Parameters is { Count: > 0 })
            {
                parameters = new List<ConditionTypeParameterJson>();
                for (int i = 0; i < data.Parameters.Count; ++i)
                {
                    var param = data.Parameters[i];
                    List<ConditionTypeParameterValueJson>? values = null;
                    if (param.Values is { Count: > 0 })
                        values = param.Values
                            .OrderBy(v => v.Key)
                            .Select(v => new ConditionTypeParameterValueJson
                            {
                                Value = v.Key,
                                Name = v.Value.Name,
                                Description = v.Value.Description
                            })
                            .ToList();

                    parameters.Add(new ConditionTypeParameterJson
                    {
                        Slot = i + 1,
                        Name = param.Name,
                        Description = string.IsNullOrEmpty(param.Description) ? null : param.Description,
                        // parameters with inline values get a generated "condition_*" key at load time - not useful for clients
                        ValueKind = param.Type != null! && !param.Type.StartsWith("condition_", StringComparison.Ordinal) ? param.Type : null,
                        Values = values
                    });
                }
            }

            List<ConditionTypeStringParameterJson>? stringParameters = null;
            if (data.StringParameters is { Count: > 0 })
                stringParameters = data.StringParameters
                    .Select(p => new ConditionTypeStringParameterJson
                    {
                        Name = p.Name,
                        Description = string.IsNullOrEmpty(p.Description) ? null : p.Description
                    })
                    .ToList();

            return new ConditionTypeJson
            {
                Id = data.Id,
                Name = data.Name,
                ReadableName = data.NameReadable,
                Help = data.Help,
                DescriptionTemplate = data.Description,
                NegatedDescriptionTemplate = data.NegativeDescription,
                Tags = data.Tags?.ToList(),
                Parameters = parameters,
                StringParameters = stringParameters
            };
        }
    }
}
