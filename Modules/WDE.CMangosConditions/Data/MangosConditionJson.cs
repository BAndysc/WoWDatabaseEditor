using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using WDE.Common.Parameters;

namespace WDE.CMangosConditions.Data
{
    [ExcludeFromCodeCoverage]
    public struct MangosConditionParameterJson
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("values")]
        public Dictionary<long, SelectOption>? Values { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class MangosConditionJson
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("name_readable")]
        public string NameReadable { get; set; } = "";

        [JsonProperty("help")]
        public string? Help { get; set; }

        [JsonProperty("parameters")]
        public IList<MangosConditionParameterJson>? Parameters { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("negdescription")]
        public string? NegativeDescription { get; set; }

        /// <summary>Logical nodes only (AND/OR/NOT): allowed children range. 0/0 = leaf.</summary>
        [JsonProperty("min_children")]
        public int MinChildren { get; set; }

        [JsonProperty("max_children")]
        public int MaxChildren { get; set; }

        /// <summary>What kind of object this condition operates on; drives {target}/{source}
        /// fallback wording. Loaded from the "tags" string array.</summary>
        [JsonProperty("tags")]
        public MangosConditionSubject Subject { get; set; }

        [JsonIgnore]
        public bool IsLogical => MaxChildren > 0;

        public override string ToString() => $"{NameReadable} ({Id})";
    }
}
