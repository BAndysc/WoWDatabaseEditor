using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    public struct DatabaseForeignTableJson
    {
        [JsonProperty(PropertyName = "table_name")]
        public string TableName { get; set; }
        
        [JsonProperty(PropertyName = "foreign_key")]
        public string[] ForeignKeys { get; set; }

        [JsonProperty(PropertyName = "autofill_build_column")]
        public string? AutofillBuildColumn { get; set; }
    }
}