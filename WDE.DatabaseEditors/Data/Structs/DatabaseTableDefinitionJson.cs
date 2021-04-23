using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        public string TableNameSource { get; set; } = "";
        
        [JsonProperty(PropertyName = "foreign_tables")]
        public IList<DatabaseForeignTableJson>? ForeignTable { get; set; }
        
        [JsonProperty(PropertyName = "groups")]
        public IList<DatabaseColumnsGroupJson> Groups { get; set; } = new List<DatabaseColumnsGroupJson>();

        [JsonIgnore] 
        public IDictionary<string, DbEditorTableGroupFieldJson> TableColumns { get; set; } = null!;
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseForeignTableJson> ForeignTableByName { get; set; } = null!;
    }

    public struct DatabaseForeignTableJson
    {
        [JsonProperty(PropertyName = "table_name")]
        public string TableName { get; set; }
        
        [JsonProperty(PropertyName = "foreign_key")]
        public string ForeignKey { get; set; }
    }

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
        public IList<DbEditorTableGroupFieldJson> Fields { get; set; }

        public struct ShowIfCondition
        {
            [JsonProperty(PropertyName = "db_column_name")]
            public string ColumnName { get; set; }
            
            [JsonProperty(PropertyName = "value")]
            public int Value { get; set; }
        }
    }

    public struct DbEditorTableGroupFieldJson
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "db_column_name")]
        public string DbColumnName { get; set; }
        
        [JsonProperty(PropertyName = "foreign_table")]
        public string? ForeignTable { get; set; }
        
        [JsonProperty(PropertyName = "value_type")]
        public string ValueType { get; set; }
        
        [JsonProperty(PropertyName = "read_only")]
        public bool IsReadOnly { get; set; }
        
        [JsonProperty(PropertyName = "can_be_null")]
        public bool CanBeNull { get; set; }
    }
}