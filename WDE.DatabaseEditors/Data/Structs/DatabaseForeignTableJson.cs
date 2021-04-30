using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    public struct DatabaseForeignTableJson
    {
        [JsonProperty(PropertyName = "table_name")]
        public string TableName { get; set; }
        
        [JsonProperty(PropertyName = "foreign_key")]
        public string ForeignKey { get; set; }
    }
}