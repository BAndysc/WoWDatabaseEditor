using System.Collections.Generic;
using System.ComponentModel;

namespace WDE.Conditions.Mcp
{
    /// <summary>
    /// A single condition row presented the way the conditions editor shows it:
    /// named type, named parameters with resolved meanings and a readable sentence.
    /// The same shape is returned by trinity_condition_get and accepted by
    /// trinity_condition_validate / trinity_condition_update.
    /// </summary>
    public class McpConditionJson
    {
        [Description("Condition type name, e.g. CONDITION_AURA (list them with trinity_condition_types). Either 'type' or 'typeId' must be set; 'type' wins when both are present.")]
        public string? Type { get; set; }

        [Description("Numeric condition type id (ConditionTypeOrReference), alternative to 'type'.")]
        public int? TypeId { get; set; }

        [Description("True to negate the condition (NegativeCondition = 1).")]
        public bool Negated { get; set; }

        [Description("ConditionTarget - which object the condition is checked on. Valid targets per source type are listed by trinity_condition_sources. Default 0.")]
        public int Target { get; set; }

        [Description("Human readable label of 'target' (informational, output only, ignored on input).")]
        public string? TargetLabel { get; set; }

        [Description("Named parameter values (ConditionValue1..3) in the order defined by the condition type; missing trailing parameters default to 0.")]
        public List<McpConditionParameterJson>? Parameters { get; set; }

        [Description("ConditionStringValue1 (only stored by cores that support it, e.g. TrinityCore master).")]
        public string? StringParameter { get; set; }

        [Description("The editor's readable sentence for this condition (informational, output only, ignored on input).")]
        public string? Readable { get; set; }

        [Description("SQL comment of the row. May be omitted on input - the editor-style readable sentence is used as the comment then, exactly like the conditions editor does.")]
        public string? Comment { get; set; }
    }

    public class McpConditionParameterJson
    {
        [Description("Parameter name as defined by the condition type (always set on output for defined parameters). On input it is optional - values are matched by position - but when a name is given it must be one of the type's defined parameter names and that parameter's position is used.")]
        public string? Name { get; set; }

        [Description("Raw numeric value stored in ConditionValueN.")]
        public required long Value { get; set; }

        [Description("Readable meaning of the value as the editor resolves it (spell/item/creature name, enum label...). Output only, ignored on input.")]
        public string? ValueName { get; set; }
    }

    public enum McpConditionProblemSeverity
    {
        Error,
        Warning
    }

    public class McpConditionProblemJson
    {
        [Description("Error = the editor could not represent/author this condition; Warning = suspicious but representable.")]
        public required McpConditionProblemSeverity Severity { get; set; }

        [Description("Zero-based index of the OR-group the problem refers to, or null for problems about the whole input.")]
        public int? Group { get; set; }

        [Description("Zero-based index of the condition inside the group, or null.")]
        public int? Index { get; set; }

        public required string Message { get; set; }
    }
}
