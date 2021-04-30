using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [ExcludeFromCodeCoverage]
    public struct DatabaseColumnsGroupJson
    {
        [JsonProperty(PropertyName = "group_name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "show_if")]
        public ShowIfCondition? ShowIf { get; set; }
        
        [JsonProperty(PropertyName = "group_sort_field")]
        public string? GroupSortField { get; set; }

        [JsonProperty(PropertyName = "fields")]
        public IList<DatabaseColumnJson> Fields { get; set; }

        public struct ShowIfCondition
        {
            [JsonProperty(PropertyName = "db_column_name")]
            public string ColumnName { get; set; }
            
            [JsonProperty(PropertyName = "value")]
            public int Value { get; set; }
        }
    }
}