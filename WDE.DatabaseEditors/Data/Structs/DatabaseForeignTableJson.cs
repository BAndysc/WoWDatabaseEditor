using Generator.Equals;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [Equatable]
    public partial struct DatabaseForeignTableJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "table_name")]
        public string TableName { get; set; }
        
        [JsonProperty(PropertyName = "foreign_key")]
        [OrderedEquality]
        public ColumnFullName[] ForeignKeys { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "autofill_build_column")]
        public string? AutofillBuildColumn { get; set; }
    }
}