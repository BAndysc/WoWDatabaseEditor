using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Utils;

namespace WDE.SmartScriptEditor.Data
{
    [ExcludeFromCodeCoverage]
    public struct SmartParameterJsonData
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
        public Dictionary<long, SelectOption> Values { get; set; }
    }

    [Flags]
    [JsonConverter(typeof(FlagJsonConverter))]
    public enum ActionFlags
    {
        None = 0,
        Await = 1,
        Async = 2,
        BeginAwait = 4,
        IncreaseIndent = 8,
        DecreaseIndent = 16,
        ConditionInParameter1 = 32,
        WaitAction = 64,
        NeedsAwait = 128,
        MustBeLast = 256
    }

    [ExcludeFromCodeCoverage]
    public struct SmartDescriptionRulesJsonData
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "condition")]
        public string? Condition { get; set; }
    }
    
    // Kind of hackfix due to lack of string enums in C# :<
    public struct SmartTargetTypes
    {
        private const string Self = "Self";
        private const string Creature = "Creature";
        private const string GameObject = "GameObject";
        private const string Player = "Player";
        private const string Position = "Position";

        public static List<string> GetAllTypes() => new() {Self, Creature, GameObject, Player, Position};
    }

    [ExcludeFromCodeCoverage]
    public struct SmartGenericJsonData
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; }

        [JsonProperty(PropertyName = "help")]
        public string Help { get; set; }

        [JsonProperty(PropertyName = "deprecated")]
        public bool Deprecated { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public IList<SmartParameterJsonData>? Parameters { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "types")]
        public IList<string>? Types { get; set; }

        [JsonProperty(PropertyName = "is_only_target")]
        public bool IsOnlyTarget { get; set; }

        [JsonProperty(PropertyName = "sources")]
        public IList<string>? Sources { get; set; }

        [JsonProperty(PropertyName = "incompatible")]
        public IList<int>? Incompatible { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public IList<string>? Tags { get; set; }
        
        [JsonProperty(PropertyName = "do_not_propose_target")]
        public bool DoNotProposeTarget { get; set; }

        [JsonProperty(PropertyName = "target_types")]
        public IList<string>? TargetTypes { get; set; }

        [JsonProperty(PropertyName = "implicit_source")]
        public string? ImplicitSource { get; set; }
        
        [JsonProperty(PropertyName = "source_in_action")]
        public bool SourceStoreInAction { get; set; }

        [JsonProperty(PropertyName = "uses_target_position")]
        public bool UsesTargetPosition { get; set; }

        [JsonProperty(PropertyName = "target_is_source")]
        public bool TargetIsSource { get; set; }

        [JsonProperty(PropertyName = "is_timed")]
        public bool IsTimed { get; set; }
        
        [JsonProperty(PropertyName = "is_invoker")]
        public bool IsInvoker { get; set; }
        
        [JsonProperty(PropertyName = "flags")]
        public ActionFlags Flags { get; set; }

        [JsonProperty(PropertyName = "comment_field")]
        public string? CommentField { get; set; }
        
        [JsonProperty(PropertyName = "replace_with_id")]
        public int? ReplaceWithId { get; set; }
        
        [JsonProperty(PropertyName = "invoker")]
        public DataJsonInvoker? Invoker { get; set; }

        [JsonProperty(PropertyName = "usable_with_script_types", ItemConverterType = typeof(StringEnumConverter))]
        public IList<SmartScriptType>? UsableWithScriptTypes { get; set; }

        [JsonProperty(PropertyName = "usable_with_event_types")]
        public IList<int>? UsableWithEventTypes { get; set; }

        [JsonProperty(PropertyName = "description_rules")]
        public IList<SmartDescriptionRulesJsonData>? DescriptionRules { get; set; }

        [JsonProperty(PropertyName = "rules")]
        public IList<SmartRule>? Rules { get; set; }
        
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

    public struct SmartRule
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
    public struct SmartGroupsJsonData
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "group_members")]
        public IList<string> Members { get; set; }
    }
}
