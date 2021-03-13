using System.Collections.Generic;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data
{
    public struct TableFieldsNameSwapDefinition
    {
        [JsonProperty(PropertyName = "condition_value_source")]
        public string ConditionValueSource { get; set; }

        [JsonProperty(PropertyName = "options")]
        public Dictionary<long, IList<TableFieldSwapDataDefinition>> Options { get; set; }
    }

    public struct TableFieldSwapDataDefinition
    {
        [JsonProperty(PropertyName = "db_column_name")]
        public string DbColumnName { get; set; }
        
        [JsonProperty(PropertyName = "new_name")]
        public string NewName { get; set; }
    }
}