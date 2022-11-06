using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using WDE.Common.Parameters;

namespace WDE.Parameters.Models
{
    [ExcludeFromCodeCoverage]
    public class ParameterSpecModel
    {
        [JsonProperty(PropertyName = "isflag")]
        public bool IsFlag { get; set; }

        public string Key { get; set; } = "";

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = "";

        [JsonProperty(PropertyName = "inmenu")]
        public bool InMenu { get; set; }

        [JsonProperty(PropertyName = "quickaccess")]
        public QuickAccessMode QuickAccess { get; set; }

        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; } = "";

        [JsonProperty(PropertyName = "tags")]
        public IList<string>? Tags { get; set; }
        
        [JsonProperty(PropertyName = "values")]
        [JsonConverter(typeof(ParameterValuesJsonConverter))]
        public Dictionary<long, SelectOption>? Values { get; set; }

        [JsonProperty(PropertyName = "mask_from")]
        public ParameterMaskFrom? MaskFrom { get; set; }
        
        [JsonProperty(PropertyName = "stringValues")]
        public Dictionary<string, SelectOption>? StringValues { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct ParameterMaskFrom
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "offset")]
        public int Offset { get; set; }
    }
}