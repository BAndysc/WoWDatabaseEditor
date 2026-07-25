using System.Collections.Generic;
using System.ComponentModel;

namespace WDE.MangosEventAiEditor.Mcp
{
    /// <summary>
    /// JSON representation of a whole EventAI script (creature_ai_scripts rows for one creature).
    /// The same shape is returned by eventai_get and accepted by eventai_update/eventai_validate,
    /// so it can round-trip. Fields marked "output only" are informational and ignored on input.
    /// </summary>
    public class EventAiScriptJson
    {
        [Description("Events of the script, in creature_ai_scripts row order.")]
        public required List<EventAiEventJson> Events { get; set; }
    }

    public class EventAiEventJson
    {
        [Description("Event type id (creature_ai_scripts.event_type). Use eventai_data_search with type=Event to discover ids.")]
        public required uint Id { get; set; }

        [Description("Event type internal name, e.g. EVENT_T_TIMER_IN_COMBAT. Output only, ignored on input.")]
        public string? Name { get; set; }

        [Description("Human readable description of the whole event. Output only, ignored on input.")]
        public string? Readable { get; set; }

        [Description("Percent chance the event is processed (event_chance, 0-100). Defaults to 100 when omitted.")]
        public long? Chance { get; set; }

        [Description("Inverse phase mask (event_inverse_phase_mask): bit N set means the event is DISABLED in phase N. 0 = active in all phases. Defaults to 0.")]
        public long? PhaseMask { get; set; }

        [Description("Event flags (event_flags), e.g. 1 = repeatable. Defaults to 0.")]
        public long? Flags { get; set; }

        [Description("Event parameter values (event_param1..event_param6). Omitted parameters keep their default value.")]
        public List<EventAiParamJson>? Params { get; set; }

        [Description("Up to 3 actions executed when the event triggers.")]
        public List<EventAiActionJson>? Actions { get; set; }
    }

    public class EventAiActionJson
    {
        [Description("Action type id (creature_ai_scripts.actionN_type). Use eventai_data_search with type=Action to discover ids.")]
        public required uint Id { get; set; }

        [Description("Action type internal name, e.g. ACTION_T_CAST. Output only, ignored on input.")]
        public string? Name { get; set; }

        [Description("Human readable description of the action. Output only, ignored on input.")]
        public string? Readable { get; set; }

        [Description("Optional free text comment attached to the action (used for special comment fields, e.g. text comments).")]
        public string? Comment { get; set; }

        [Description("Action parameter values (actionN_param1..actionN_param3). Omitted parameters keep their default value.")]
        public List<EventAiParamJson>? Params { get; set; }
    }

    public class EventAiParamJson
    {
        [Description("0-based parameter index. On input either index or name must be provided (index wins when both given).")]
        public int? Index { get; set; }

        [Description("Parameter name as reported by eventai_get / eventai_data_search. Alternative to index on input.")]
        public string? Name { get; set; }

        [Description("Raw numeric value stored in the database column.")]
        public required long Value { get; set; }

        [Description("Human readable form of the value (spell name, creature name, flags...). Output only, ignored on input.")]
        public string? ReadableValue { get; set; }
    }

    public class EventAiProblemJson
    {
        [Description("Severity of the problem: Critical, Error, Warning or Info.")]
        public required string Severity { get; set; }

        [Description("Description of the problem.")]
        public required string Message { get; set; }

        [Description("1-based script line the problem refers to.")]
        public required int Line { get; set; }
    }
}
