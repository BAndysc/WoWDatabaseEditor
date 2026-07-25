using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace WDE.CMangosConditions.Data
{
    /// <summary>One of the 87 UnitCondition variable types (mangos-wotlk UnitCondition.h).</summary>
    [ExcludeFromCodeCoverage]
    public class UnitConditionVariableJson
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>Enum name from UnitCondition.h.</summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("name_readable")]
        public string NameReadable { get; set; } = "";

        [JsonProperty("help")]
        public string? Help { get; set; }

        /// <summary>The compared Value column; null = plain number.</summary>
        [JsonProperty("value")]
        public MangosConditionParameterJson? Value { get; set; }

        /// <summary>SmartFormat template with {op}, {value}, {rawvalue} args;
        /// default "{name} {op} {value}".</summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>Not implemented in the cmangos core (evaluates to false).</summary>
        [JsonProperty("nyi")]
        public bool Nyi { get; set; }

        public override string ToString() => NameReadable + (Nyi ? " (NYI)" : "") + $" ({Id})";
    }
}
