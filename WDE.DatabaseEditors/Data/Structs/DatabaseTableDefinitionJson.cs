using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [ExcludeFromCodeCoverage]
    public class DatabaseTableDefinitionJson
    {
        [JsonProperty(PropertyName = "id")] 
        public string Id { get; set; } = "";

        [JsonProperty(PropertyName = "compatibility")] 
        public IList<string> Compatibility { get; set; } = System.Array.Empty<string>();
        
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
        
        [JsonProperty(PropertyName = "group_name")] 
        public string? GroupName { get; set; }
        
        [JsonProperty(PropertyName = "single_solution_name")] 
        public string SingleSolutionName { get; set; } = "";
        
        [JsonProperty(PropertyName = "multi_solution_name")] 
        public string MultiSolutionName { get; set; } = "";
        
        [JsonProperty(PropertyName = "description")] 
        public string Description { get; set; } = "";
        
        [JsonProperty(PropertyName = "table_name")]
        public string TableName { get; set; } = "";
        
        [JsonProperty(PropertyName = "table_index_name")]
        public string TablePrimaryKeyColumnName { get; set; } = "";
        
        [JsonProperty(PropertyName = "multi_record")]
        public bool IsMultiRecord { get; set; }
        
        [JsonProperty(PropertyName = "reload_command")]
        public string? ReloadCommand { get; set; }
        
        [JsonProperty(PropertyName = "picker")]
        public string Picker { get; set; } = "";

        [JsonProperty(PropertyName = "table_name_source_field")]
        public string? TableNameSource { get; set; }
        
        [JsonProperty(PropertyName = "primary_key")]
        public IList<string>? PrimaryKey { get; set; }
        
        [JsonProperty(PropertyName = "foreign_tables")]
        public IList<DatabaseForeignTableJson>? ForeignTable { get; set; }
        
        [JsonProperty(PropertyName = "groups")]
        public IList<DatabaseColumnsGroupJson> Groups { get; set; } = new List<DatabaseColumnsGroupJson>();

        [JsonIgnore]
        public string FileName { get; set; } = "";
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseColumnJson> TableColumns { get; set; } = null!;
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseForeignTableJson> ForeignTableByName { get; set; } = null!;
    }
}