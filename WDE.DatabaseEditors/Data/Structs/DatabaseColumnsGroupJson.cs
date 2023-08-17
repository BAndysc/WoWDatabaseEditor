using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Generator.Equals;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [ExcludeFromCodeCoverage]
    [Equatable]
    public partial class DatabaseColumnsGroupJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "group_name")]
        public string Name { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "show_if")]
        public ShowIfCondition? ShowIf { get; set; }
        
        [JsonProperty(PropertyName = "fields")]
        [OrderedEquality]
        public IList<DatabaseColumnJson> Fields { get; set; } = null!;
        
        [Equatable]
        public partial struct ShowIfCondition
        {
            [DefaultEquality]
            [JsonProperty(PropertyName = "db_column_name")]
            public string ColumnName { get; set; }
            
            [DefaultEquality]
            [JsonProperty(PropertyName = "value", DefaultValueHandling = DefaultValueHandling.Include)]
            public int Value { get; set; }
        }
    }
}