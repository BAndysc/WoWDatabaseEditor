using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Common.Database;
using WDE.Common.Parameters;

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
        public int DefaultVal { get; set; }

        [JsonProperty(PropertyName = "enter_to_accept")]
        public bool EnterToAccept { get; set; }

        [JsonProperty(PropertyName = "values")]
        public Dictionary<int, SelectOption> Values { get; set; }
    }

    public enum WarningType
    {
        NOT_SET,
        INVALID_TARGET,
        INVALID_PARAMETER,
        INVALID_VALUE
    }

    public enum CompareType
    {
        [Description("equal")]
        EQUALS,

        [Description("not equal")]
        NOT_EQUALS,

        [Description("lower than")]
        LOWER_THAN,

        [Description("greater than")]
        GREATER_THAN,

        [Description("lower or equal")]
        LOWER_OR_EQUALS,

        [Description("greater or equal")]
        GREATER_OR_EQUALS,

        [Description("between")]
        BETWEEN
    }

    [ExcludeFromCodeCoverage]
    public struct SmartConditionalJsonData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "warning_type")]
        public WarningType WarningType { get; set; }


        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "compare_type")]
        public CompareType CompareType { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "invert")]
        public bool Invert { get; set; }

        [JsonProperty(PropertyName = "compared_parameter_id")]
        public int ComparedParameterId { get; set; }

        [JsonProperty(PropertyName = "compare_to_parameter_id")]
        public int CompareToParameterId { get; set; }

        [JsonProperty(PropertyName = "compare_to_value")]
        public int CompareToValue { get; set; }

        [JsonProperty(PropertyName = "compared_any_param")]
        public string ComparedAnyParam { get; set; }

        [JsonProperty(PropertyName = "compared_to_any_param")]
        public string ComparedToAnyParam { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public struct SmartDescriptionRulesJsonData
    {
        [JsonProperty(PropertyName = "description")]
        public string Description;

        [JsonProperty(PropertyName = "conditions")]
        public IList<SmartConditionalJsonData> Conditions { get; set; }
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

        [JsonProperty(PropertyName = "invoker")]
        public string Invoker { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public IList<SmartParameterJsonData> Parameters { get; set; }

        [JsonProperty(PropertyName = "conditions")]
        public IList<SmartConditionalJsonData> Conditions { get; set; }

        [JsonProperty(PropertyName = "valid_types", ItemConverterType = typeof(StringEnumConverter))]
        public IList<SmartScriptType> ValidTypes { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "types")]
        public IList<string> Types { get; set; }

        [JsonProperty(PropertyName = "is_only_target")]
        public bool IsOnlyTarget { get; set; }

        [JsonProperty(PropertyName = "sources")]
        public IList<string> Sources { get; set; }

        [JsonProperty(PropertyName = "targets")]
        public IList<string> Targets { get; set; }

        [JsonProperty(PropertyName = "incompatible")]
        public IList<int> Incompatible { get; set; }

        [JsonProperty(PropertyName = "do_not_propose_target")]
        public bool DoNotProposeTarget { get; set; }

        [JsonProperty(PropertyName = "uses_target")]
        public bool UsesTarget { get; set; }

        [JsonProperty(PropertyName = "implicit_source")]
        public bool ImplicitSource { get; set; }

        [JsonProperty(PropertyName = "target_is_source")]
        public bool TargetIsSource { get; set; }

        [JsonProperty(PropertyName = "is_timed")]
        public bool IsTimed { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public string Tags { get; set; }

        [JsonProperty(PropertyName = "usable_with_events")]
        public IList<int> UsableWithEvents { get; set; }

        [JsonProperty(PropertyName = "usable_with_event_types")]
        public IList<SmartScriptType> UsableWithEventTypes { get; set; }

        [JsonProperty(PropertyName = "description_rules")]
        public IList<SmartDescriptionRulesJsonData> DescriptionRules { get; set; }

        [JsonProperty(PropertyName = "tooltip")]
        public string Tooltip { get; set; }

        public bool HasParameters() { return Parameters != null && Parameters.Count > 0; }
    }
}