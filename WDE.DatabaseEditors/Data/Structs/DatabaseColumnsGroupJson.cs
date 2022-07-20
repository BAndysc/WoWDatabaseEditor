using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [ExcludeFromCodeCoverage]
    public class DatabaseColumnsGroupJson
    {
        [JsonProperty(PropertyName = "group_name")]
        public string Name { get; set; } = "";
        
        [JsonProperty(PropertyName = "show_if")]
        public ShowIfCondition? ShowIf { get; set; }
        
        [JsonProperty(PropertyName = "fields")]
        public IList<DatabaseColumnJson> Fields { get; set; } = null!;

        public struct ShowIfCondition
        {
            [JsonProperty(PropertyName = "db_column_name")]
            public string ColumnName { get; set; }
            
            [JsonProperty(PropertyName = "value")]
            public int Value { get; set; }
        }
    }
}