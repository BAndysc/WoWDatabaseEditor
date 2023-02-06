using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Utils;

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
        
        [JsonProperty(PropertyName = "database")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DataDatabaseType DataDatabaseType { get; set; }

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
        public IList<string> PrimaryKey { get; set; } = null!;

        [JsonProperty(PropertyName = "commands")]
        public IList<DatabaseCommandDefinitionJson>? Commands { get; set; }

        [JsonProperty(PropertyName = "group_by_key")]
        public string? GroupByKey { get; set; } = null;
        
        [JsonProperty(PropertyName = "conditions")]
        public DatabaseConditionReferenceJson? Condition { get; set; }

        [JsonProperty(PropertyName = "foreign_tables")]
        public IList<DatabaseForeignTableJson>? ForeignTable { get; set; }
        
        [JsonProperty(PropertyName = "groups")]
        public IList<DatabaseColumnsGroupJson> Groups { get; set; } = new List<DatabaseColumnsGroupJson>();

        [JsonProperty(PropertyName = "auto_key")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GuidType? AutoKeyValue { get; set; }
        
        [JsonIgnore]
        public string FileName { get; set; } = "";
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseColumnJson> TableColumns { get; set; } = null!;
        
        [JsonIgnore] 
        public IDictionary<string, DatabaseForeignTableJson>? ForeignTableByName { get; set; }

        [JsonIgnore]
        public IList<string> GroupByKeys
        {
            get
            {
                if (RecordMode == RecordMode.SingleRow)
                    return PrimaryKey!;
                return new List<string>(){!string.IsNullOrWhiteSpace(TablePrimaryKeyColumnName) ? TablePrimaryKeyColumnName : PrimaryKey[0]};
            }
        }
        
        // single row table type ignores solution item entries quality, because there is no keys
        [JsonIgnore] public bool IgnoreEquality => RecordMode == RecordMode.SingleRow;
    }

    [Flags]
    [JsonConverter(typeof(FlagJsonConverter))]
    public enum DatabaseCommandUsage
    {
        Toolbar = 1,
        ContextMenu = 2,
        All = Toolbar | ContextMenu
    }
    
    public class DatabaseCommandDefinitionJson
    {
        [JsonProperty(PropertyName = "command_id")]
        public string CommandId { get; set; } = "";
        
        [JsonProperty(PropertyName = "parameters")]
        public string[]? Parameters { get; set; }

        [JsonProperty(PropertyName = "keybinding")]
        public string? KeyBinding { get; set; }
        
        [JsonProperty(PropertyName = "usage", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(DatabaseCommandUsage.All)]
        public DatabaseCommandUsage Usage { get; set; } = DatabaseCommandUsage.All;
    }
    
    public class DatabaseConditionReferenceJson
    {
        [JsonProperty(PropertyName = "source_type")]
        public int SourceType { get; set; }
        
        [JsonProperty(PropertyName = "source_group")]
        [JsonConverter(typeof(DatabaseConditionColumnConverter))]
        public DatabaseConditionColumn? SourceGroupColumn { get; set; }
        
        [JsonProperty(PropertyName = "source_entry")]
        public string? SourceEntryColumn { get; set; }
        
        [JsonProperty(PropertyName = "source_id")]
        public string? SourceIdColumn { get; set; }
        
        [JsonProperty(PropertyName = "set_column")]
        public string? SetColumn { get; set; }
        
        [JsonProperty(PropertyName = "targets")]
        public IList<DatabaseConditionTargetJson>? Targets { get; set; }
    }

    public sealed class DatabaseConditionColumn
    {
        public string Name { get; set; } = "";
        
        public bool IsAbs { get; set; }

        public int Calculate(int val)
        {
            if (IsAbs)
                return Math.Abs(val);
            return val;
        }
    }

    public class DatabaseConditionTargetJson
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }

        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
    }
}