using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    public class DatabaseColumnJson
    {
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
        
        [JsonProperty(PropertyName = "help")] 
        public string? Help { get; set; } = null;
        
        [JsonProperty(PropertyName = "db_column_name")]
        public string DbColumnName { get; set; } = "";
        
        [JsonProperty(PropertyName = "foreign_table")]
        public string? ForeignTable { get; set; }
        
        [JsonProperty(PropertyName = "value_type")]
        public string ValueType { get; set; } = "";

        [JsonProperty(PropertyName = "default")]
        public object? Default { get; set; }
        
        [JsonProperty(PropertyName = "autoincrement")]
        public bool AutoIncrement { get; set; }
        
        [JsonProperty(PropertyName = "read_only")]
        public bool IsReadOnly { get; set; }
        
        [JsonProperty(PropertyName = "can_be_null")]
        public bool CanBeNull { get; set; }
        
        [JsonProperty(PropertyName = "is_condition")]
        public bool IsConditionColumn { get; set; }
        
        [JsonProperty(PropertyName = "zero_is_blank")]
        public bool IsZeroBlank { get; set; }
        
        [JsonProperty(PropertyName = "autogenerate_comment")]
        public string? AutogenerateComment { get; set; }
        
        [JsonProperty(PropertyName = "is_meta")]
        public bool IsMetaColumn { get; set; }
        
        [JsonProperty(PropertyName = "expression")]
        public string? Expression { get; set; }

        [JsonProperty(PropertyName = "preferred_width")]
        public float? PreferredWidth { get; set; }
    }
}