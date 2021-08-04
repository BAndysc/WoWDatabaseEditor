using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Data
{
    public class NodeDefinition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "node_type")]
        public NodeType NodeType { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "header")]
        public string Header { get; set; }

        [JsonProperty(PropertyName = "inputs")]
        public IList<InputOutputDefinition> Inputs { get; set; }

        [JsonProperty(PropertyName = "outputs")]
        public IList<InputOutputDefinition> Outputs { get; set; }
    }
}