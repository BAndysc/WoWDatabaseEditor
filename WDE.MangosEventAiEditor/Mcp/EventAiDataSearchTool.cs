using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.Mcp;
using WDE.EventAiEditor.Data;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Mcp
{
    public enum EventAiDataType
    {
        Event,
        Action
    }

    public class EventAiDataSearchInput
    {
        [Description("Whether to search EventAI event types or action types.")]
        public required EventAiDataType Type { get; set; }

        [Description("Case-insensitive regex (or plain substring) matched against the internal and readable names, e.g. 'timer' or 'EVENT_T_.*HP'.")]
        public string? Filter { get; set; }

        [Description("Exact event/action type id to look up. When given, filter is ignored.")]
        public uint? Id { get; set; }

        [Description("Maximum number of returned entries, default 100.")]
        public int Limit { get; set; } = 100;
    }

    public class EventAiDataSearchOutput
    {
        [Description("Total number of matching entries (may be larger than the number of returned items when limited).")]
        public required int TotalMatches { get; set; }

        public required List<EventAiDataEntryJson> Items { get; set; }
    }

    public class EventAiDataEntryJson
    {
        [Description("Event/action type id used in creature_ai_scripts.")]
        public required uint Id { get; set; }

        [Description("Internal name, e.g. EVENT_T_TIMER_IN_COMBAT or ACTION_T_CAST.")]
        public required string Name { get; set; }

        [Description("Human friendly name.")]
        public required string ReadableName { get; set; }

        [Description("Longer help text explaining the event/action.")]
        public string? Help { get; set; }

        [Description("Readable description template ({pramN} placeholders refer to the parameters).")]
        public string? Description { get; set; }

        [Description("True when the event/action is deprecated and should not be used in new scripts.")]
        public bool Deprecated { get; set; }

        public List<EventAiDataParameterJson>? Parameters { get; set; }
    }

    public class EventAiDataParameterJson
    {
        [Description("0-based parameter index (matches the 'index' field of script parameters).")]
        public required int Index { get; set; }

        [Description("Parameter name.")]
        public required string Name { get; set; }

        [Description("Parameter type, e.g. SpellParameter, CreatureParameter, Parameter (plain number).")]
        public required string Type { get; set; }

        [Description("What the parameter does.")]
        public string? Description { get; set; }

        [Description("True when a zero value is considered an error.")]
        public bool Required { get; set; }

        [Description("Known values map (value -> meaning) for enum/flag-like parameters.")]
        public Dictionary<string, string>? Values { get; set; }
    }

    [AutoRegisterToParentScope]
    [SingleInstance]
    public class EventAiDataSearchTool : McpTool<EventAiDataSearchInput, EventAiDataSearchOutput>
    {
        private readonly Lazy<IEventAiDataManager> dataManager;

        public EventAiDataSearchTool(Lazy<IEventAiDataManager> dataManager)
        {
            this.dataManager = dataManager;
        }

        public override string Name => "eventai_data_search";

        public override string Description => "Searches the known CMaNGOS EventAI event types and action types (creature_ai_scripts) by name or id. Returns each entry's id, names, help text and parameter definitions. Use it to discover valid ids and parameter meanings before calling eventai_get/eventai_update.";

        protected override Task<EventAiDataSearchOutput> Execute(EventAiDataSearchInput input, CancellationToken token)
        {
            var type = input.Type == EventAiDataType.Event ? EventOrAction.Event : EventOrAction.Action;

            List<EventActionGenericJsonData> matched;
            if (input.Id.HasValue)
            {
                if (!dataManager.Value.Contains(type, input.Id.Value))
                    throw new McpToolException($"There is no {input.Type} with id {input.Id.Value}.");
                matched = new List<EventActionGenericJsonData> { dataManager.Value.GetRawData(type, input.Id.Value) };
            }
            else
            {
                IEnumerable<EventActionGenericJsonData> all = dataManager.Value.GetAllData(type).OrderBy(d => d.Id);
                matched = Filter(all, input.Filter).ToList();
            }

            int limit = Math.Max(1, input.Limit);
            var output = new EventAiDataSearchOutput
            {
                TotalMatches = matched.Count,
                Items = matched.Take(limit).Select(ToJson).ToList()
            };
            return Task.FromResult(output);
        }

        private static IEnumerable<EventActionGenericJsonData> Filter(IEnumerable<EventActionGenericJsonData> source, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return source;

            Regex? regex = null;
            try
            {
                regex = new Regex(filter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            }
            catch (ArgumentException)
            {
                // not a valid regex - fall back to substring matching
            }

            return source.Where(d => Matches(d.Name, filter, regex) || Matches(d.NameReadable, filter, regex));
        }

        private static bool Matches(string? text, string filter, Regex? regex)
        {
            if (text == null)
                return false;
            if (regex != null)
            {
                try
                {
                    return regex.IsMatch(text);
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
            }

            return text.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        private static EventAiDataEntryJson ToJson(EventActionGenericJsonData data)
        {
            List<EventAiDataParameterJson>? parameters = null;
            if (data.Parameters != null && data.Parameters.Count > 0)
            {
                parameters = new List<EventAiDataParameterJson>();
                for (int i = 0; i < data.Parameters.Count; ++i)
                {
                    var param = data.Parameters[i];
                    Dictionary<string, string>? values = null;
                    if (param.Values != null && param.Values.Count > 0)
                    {
                        values = new Dictionary<string, string>();
                        foreach (var (value, option) in param.Values.OrderBy(p => p.Key))
                            values[value.ToString()] = string.IsNullOrEmpty(option.Description)
                                ? option.Name
                                : $"{option.Name} - {option.Description}";
                    }

                    parameters.Add(new EventAiDataParameterJson
                    {
                        Index = i,
                        Name = param.Name,
                        Type = param.Type,
                        Description = string.IsNullOrEmpty(param.Description) ? null : param.Description,
                        Required = param.Required,
                        Values = values
                    });
                }
            }

            return new EventAiDataEntryJson
            {
                Id = data.Id,
                Name = data.Name,
                ReadableName = data.NameReadable,
                Help = string.IsNullOrEmpty(data.Help) ? null : data.Help,
                Description = string.IsNullOrEmpty(data.Description) ? null : data.Description,
                Deprecated = data.Deprecated,
                Parameters = parameters
            };
        }
    }
}
