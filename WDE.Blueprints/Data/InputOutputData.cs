using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Data
{
    public class InputOutputDefinition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "type")]
        public IoType Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }
}