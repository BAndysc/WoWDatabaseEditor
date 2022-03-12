﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WDE.DatabaseEditors.Data.Structs
{
    public enum RecordMode
    {
        Template,
        MultiRecord,
        SingleRow
    }
    
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
        
        [JsonProperty(PropertyName = "record_mode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RecordMode RecordMode { get; set; }
        
        [JsonProperty(PropertyName = "only_conditions")]
        public bool IsOnlyConditionsTable { get; set; }
        
        [JsonProperty(PropertyName = "skip_quick_load")]
        public bool SkipQuickLoad { get; set; }
        
        [JsonProperty(PropertyName = "group_name")] 
        public string? GroupName { get; set; }
        
        [JsonProperty(PropertyName = "icon_path")] 
        public string? IconPath { get; set; }

        [JsonProperty(PropertyName = "reload_command")]
        public string? ReloadCommand { get; set; }
        
        [JsonProperty(PropertyName = "sort_by")]
        public string[]? SortBy { get; set; }
        
        [JsonProperty(PropertyName = "picker")]
        public string Picker { get; set; } = "";

        [JsonProperty(PropertyName = "table_name_source_field")]
        public string? TableNameSource { get; set; }
        
        [JsonProperty(PropertyName = "primary_key")]
        public IList<string>? PrimaryKey { get; set; }
        
        [JsonProperty(PropertyName = "conditions")]
        public DatabaseConditionReferenceJson? Condition { get; set; }

        [JsonProperty(PropertyName = "foreign_tables")]
        public IList<DatabaseForeignTableJson>? ForeignTable { get; set; }
        
        [JsonProperty(PropertyName = "groups")]
        public IList<DatabaseColumnsGroupJson> Groups { get; set; } = new List<DatabaseColumnsGroupJson>();

        [JsonProperty(PropertyName = "commands")]
        public IList<DatabaseCommandDefinitionJson>? Commands { get; set; }
        
        [JsonIgnore]
        public string FileName { get; set; } = "";
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseColumnJson> TableColumns { get; set; } = null!;
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseForeignTableJson> ForeignTableByName { get; set; } = null!;

        // single row table type ignores solution item entries quality, because there is no keys
        [JsonIgnore] public bool IgnoreEquality => RecordMode == RecordMode.SingleRow;
    }

    public class DatabaseCommandDefinitionJson
    {
        [JsonProperty(PropertyName = "command_id")]
        public string CommandId { get; set; } = "";
        
        [JsonProperty(PropertyName = "parameters")]
        public string[]? Parameters { get; set; }
    }
    
    public class DatabaseConditionReferenceJson
    {
        [JsonProperty(PropertyName = "source_type")]
        public int SourceType { get; set; }
        
        [JsonProperty(PropertyName = "source_group")]
        public string? SourceGroupColumn { get; set; }
        
        [JsonProperty(PropertyName = "source_entry")]
        public string? SourceEntryColumn { get; set; }
        
        [JsonProperty(PropertyName = "source_id")]
        public string? SourceIdColumn { get; set; }
        
        [JsonProperty(PropertyName = "set_column")]
        public string? SetColumn { get; set; }
        
        [JsonProperty(PropertyName = "targets")]
        public IList<DatabaseConditionTargetJson>? Targets { get; set; }
    }

    public class DatabaseConditionTargetJson
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }

        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
    }
}