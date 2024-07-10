using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Generator.Equals;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Utils;

namespace WDE.SmartScriptEditor.Data
{
    public enum ParameterSpecialDefaultValue
    {
        None,
        EntryOrGuid
    }
    
    [Equatable]
    public partial struct SmartParameterJsonData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "required")]
        public bool Required { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "defaultVal")]
        public long DefaultVal { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "special_default")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ParameterSpecialDefaultValue SpecialDefaultVal { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "enter_to_accept")]
        public bool EnterToAccept { get; set; }
        
        [UnorderedEquality]
        [JsonProperty(PropertyName = "values")]
        [JsonConverter(typeof(ParameterValuesJsonConverter))]
        public Dictionary<long, SelectOption>? Values { get; set; }
    }

    public static class SmartParameterJsonDataExtensions
    {
        public static long GetEffectiveDefaultValue(this SmartParameterJsonData data, SmartScriptBase? scriptBase)
        {
            if (data.SpecialDefaultVal == ParameterSpecialDefaultValue.EntryOrGuid)
            {
                if (scriptBase is SmartScript script)
                    return script.EntryOrGuid;
                return 0;
            }

            return data.DefaultVal;
        }
    }
    
    [ExcludeFromCodeCoverage]
    [Equatable]
    public partial struct SmartFloatParameterJsonData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "defaultVal")]
        public float DefaultVal { get; set; }
    }
    
    [Equatable]
    [ExcludeFromCodeCoverage]
    public partial struct SmartStringParameterJsonData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "defaultVal")]
        public string DefaultVal { get; set; }
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
        MustBeLast = 256,
        AwaitAction = 512,
        SynchronizeParam12 = 1024,
        SynchronizeParam34 = 2048,
    }
    
    [Flags]
    [JsonConverter(typeof(EnumMaskConverter))]
    public enum SmartSourceTargetType
    {
        None = 0,
        Creature = 1,
        GameObject = 2,
        Player = 4,
        AreaTrigger = 8,
        Dummy = 16,
        Conversation = 32,
        BattlePet = 64,
        DynObj = 128,
        Position = 256,
        Aura = 512,
        Spell = 1024,
        Null = 2048,
        
        Self = 0x800000,
        
        Unit = Creature | Player,
        WorldObject = Unit | GameObject | AreaTrigger | Dummy | Conversation | DynObj
    }

    [Equatable]
    [ExcludeFromCodeCoverage]
    public partial struct SmartDescriptionRulesJsonData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "condition")]
        public string? Condition { get; set; }
    }
    
    public enum SmartContextMenuCommand
    {
        None,
        AddEvent,
        /// <summary>
        /// note: this is a hack, a better way would be to have generic command 'ShouldShow', but for now
        /// it is enough
        /// </summary>
        AddEventIfAura,
        OpenScript,
        AddAction,
        InvertBoolParameter
    }

    [Equatable]
    public partial class SmartContextMenuCopyParameter
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "const")]
        public long? Const { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "from")]
        public int? From { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "to", DefaultValueHandling = DefaultValueHandling.Include)]
        public int To { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "negate_value")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SmartContextMenuCopyParameterFlags Flags { get; set; }
    }

    [Flags]
    public enum SmartContextMenuCopyParameterFlags
    {
        None = 0,
        InvertBool = 1,
        NegateValue = 2,
    }

    [Equatable]
    public partial class SmartContextMenuData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "header")]
        public string Header { get; set; } = "";
        
        [DefaultEquality]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "command")]
        public SmartContextMenuCommand Command { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "icon")]
        public string? Icon { get; set; }

        // AddEvent
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "event_id")]
        public string? EventActionId { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "fill_parameters")]
        public IList<SmartContextMenuCopyParameter>? FillParameters { get; set; }
        
        // OpenScript
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "script_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SmartScriptType ScriptType { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "entry_from_parameter", DefaultValueHandling = DefaultValueHandling.Include)]
        public int EntryFromParameter { get; set; }
    }
    
    [Equatable]
    [ExcludeFromCodeCoverage]
    public partial class SmartGenericJsonData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "id", DefaultValueHandling = DefaultValueHandling.Include)]
        public int Id { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = "";

        [DefaultEquality]
        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; } = "";

        [DefaultEquality]
        [DefaultValue("")]
        [JsonProperty(PropertyName = "help")]
        public string Help { get; set; } = "";

        [DefaultEquality]
        [JsonProperty(PropertyName = "deprecated")]
        public bool Deprecated { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "parameters")]
        public IList<SmartParameterJsonData>? Parameters { get; set; }
        
        [OrderedEquality]
        [JsonProperty(PropertyName = "fparameters")]
        public IList<SmartFloatParameterJsonData>? FloatParameters { get; set; }
        
        [OrderedEquality]
        [JsonProperty(PropertyName = "sparameters")]
        public IList<SmartStringParameterJsonData>? StringParameters { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; } = "";

        [DefaultEquality]
        [JsonProperty(PropertyName = "types")]
        public SmartSourceTargetType RawTypes { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "is_only_target")]
        public bool IsOnlyTarget { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "sources")]
        public SmartSourceTargetType Sources { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "tags")]
        public IList<string>? Tags { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "do_not_propose_target")]
        public bool DoNotProposeTarget { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "target_types")]
        public SmartSourceTargetType TargetTypes { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "implicit_source")]
        public string? ImplicitSource { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "source_in_action")]
        public bool SourceStoreInAction { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "uses_target_position")]
        public bool UsesTargetPosition { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "target_is_source")]
        public bool TargetIsSource { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "is_timed")]
        public bool IsTimed { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "is_invoker")]
        public bool IsInvoker { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "flags")]
        public ActionFlags Flags { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "comment_field")]
        public string? CommentField { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "replace_with_id")]
        public int? ReplaceWithId { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "invoker")]
        public DataJsonInvoker? Invoker { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "usable_with_script_types")]
        [JsonConverter(typeof(EnumMaskConverter))]
        public SmartScriptTypeMask? UsableWithScriptTypes { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "usable_with_event_types")]
        public IList<int>? UsableWithEventTypes { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "description_rules")]
        public IList<SmartDescriptionRulesJsonData>? DescriptionRules { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "rules")]
        public IList<SmartRule>? Rules { get; set; }
        
        [OrderedEquality]
        [JsonProperty(PropertyName = "builtin_rules")]
        [JsonConverter(typeof(SmartBuiltinRuleConverter))]
        public IList<SmartBuiltInRule>? BuiltinRules { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "search_tags")]
        public string? SearchTags { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "default_for")]
        [JsonConverter(typeof(EnumMaskConverter))]
        public SmartScriptTypeMask? DefaultFor { get; set; }
        
        [OrderedEquality]
        [JsonProperty(PropertyName = "context_menu")]
        public IList<SmartContextMenuData>? ContextMenu { get; set; }
        
        public bool HasParameters()
        {
            return Parameters != null && Parameters.Count > 0;
        }
        
        public SmartSourceTargetType Types(SmartScriptType scriptType)
        {
            var hasSelf = RawTypes.HasFlag(SmartSourceTargetType.Self);
            if (!hasSelf)
                return RawTypes;
            var types = RawTypes;
            switch (scriptType)
            {
                case SmartScriptType.Creature:
                    types |= SmartSourceTargetType.Creature;
                    break;
                case SmartScriptType.GameObject:
                    types |= SmartSourceTargetType.GameObject;
                    break;
                case SmartScriptType.Quest:
                    types |= SmartSourceTargetType.Player;
                    break;
                case SmartScriptType.Spell:
                    types |= SmartSourceTargetType.Unit;
                    break;
                case SmartScriptType.TimedActionList:
                    types |= SmartSourceTargetType.Creature | SmartSourceTargetType.GameObject;
                    break;
                case SmartScriptType.AreaTriggerEntityServerSide:
                    types |= SmartSourceTargetType.AreaTrigger;
                    break;
                case SmartScriptType.StaticSpell:
                    types |= SmartSourceTargetType.Unit;
                    break;
                case SmartScriptType.BattlePet:
                    types |= SmartSourceTargetType.BattlePet;
                    break;
                case SmartScriptType.Conversation:
                    types |= SmartSourceTargetType.Player;
                    break;
            }

            return types;
        }
    }

    [Equatable]
    public partial struct SmartRule
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "rule")]
        public string Rule { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "level", DefaultValueHandling = DefaultValueHandling.Include)]
        [JsonConverter(typeof(StringEnumConverter))]
        public DiagnosticSeverity Level { get; set; }
    }

    [Equatable]
    public partial class DataJsonInvoker
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "types")]
        public SmartSourceTargetType Types { get; set; }
    }

    [Equatable]
    [ExcludeFromCodeCoverage]
    public partial struct SmartGroupsJsonData
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "group_members")]
        public IList<string> Members { get; set; }
    }
}
