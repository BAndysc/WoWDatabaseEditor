using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;

namespace WDE.EventAiEditor.Data
{
    [ExcludeFromCodeCoverage]
    public struct EventAiParameterJsonData
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "required")]
        public bool Required { get; set; }

        [JsonProperty(PropertyName = "defaultVal")]
        public long DefaultVal { get; set; }

        [JsonProperty(PropertyName = "enter_to_accept")]
        public bool EnterToAccept { get; set; }
        
        [JsonProperty(PropertyName = "values")]
        [JsonConverter(typeof(ParameterValuesJsonConverter))]
        public Dictionary<long, SelectOption> Values { get; set; }
    }

    [Flags]
    [JsonConverter(typeof(FlagJsonConverter))]
    public enum ActionFlags
    {
        None = 0,
        IncreaseIndent = 8,
        DecreaseIndent = 16
    }

    [ExcludeFromCodeCoverage]
    public struct EventDescriptionRulesJsonData
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "condition")]
        public string? Condition { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public struct EventActionGenericJsonData
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; }

        [JsonProperty(PropertyName = "help")]
        public string Help { get; set; }

        [JsonProperty(PropertyName = "deprecated")]
        public bool Deprecated { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public IList<EventAiParameterJsonData>? Parameters { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public IList<string>? Tags { get; set; }
        
        [JsonProperty(PropertyName = "is_timed")]
        public bool IsTimed { get; set; }
        
        [JsonProperty(PropertyName = "flags")]
        public ActionFlags Flags { get; set; }

        [JsonProperty(PropertyName = "comment_field")]
        public string? CommentField { get; set; }
        
        [JsonProperty(PropertyName = "replace_with_id")]
        public int? ReplaceWithId { get; set; }
        
        [JsonProperty(PropertyName = "invoker")]
        public DataJsonInvoker? Invoker { get; set; }

        [JsonProperty(PropertyName = "description_rules")]
        public IList<EventDescriptionRulesJsonData>? DescriptionRules { get; set; }

        [JsonProperty(PropertyName = "rules")]
        public IList<EventAiRule>? Rules { get; set; }
        
        [JsonProperty(PropertyName = "builtin_rules")]
        public IList<string>? BuiltinRules { get; set; }
        
        [JsonProperty(PropertyName = "tooltip")]
        public string Tooltip { get; set; }

        [JsonProperty(PropertyName = "search_tags")]
        public string? SearchTags { get; set; }
        
        public bool HasParameters()
        {
            return Parameters != null && Parameters.Count > 0;
        }
    }

    public struct EventAiRule
    {
        [JsonProperty(PropertyName = "rule")]
        public string Rule { get; set; }
        
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        
        [JsonProperty(PropertyName = "level", ItemConverterType = typeof(StringEnumConverter))]
        public DiagnosticSeverity Level { get; set; }
    }

    public class DataJsonInvoker
    {
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
        
        [JsonProperty(PropertyName = "types")]
        public IList<string> Types { get; set; } = new List<string>();
    }

    [ExcludeFromCodeCoverage]
    public struct EventAiGroupsJsonData
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "group_members")]
        public IList<string> Members { get; set; }
    }
}
