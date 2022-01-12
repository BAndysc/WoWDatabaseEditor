using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using WDE.Common.Parameters;

namespace WDE.Parameters.Models
{
    [ExcludeFromCodeCoverage]
    public struct ParameterSpecModel
    {
        [JsonProperty(PropertyName = "isflag")]
        public bool IsFlag { get; set; }

        public string Key { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "inmenu")]
        public bool InMenu { get; set; }

        [JsonProperty(PropertyName = "quickaccess")]
        public QuickAccessMode QuickAccess { get; set; }

        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; }

        [JsonProperty(PropertyName = "values")]
        public Dictionary<long, SelectOption>? Values { get; set; }

        [JsonProperty(PropertyName = "stringValues")]
        public Dictionary<string, SelectOption>? StringValues { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}