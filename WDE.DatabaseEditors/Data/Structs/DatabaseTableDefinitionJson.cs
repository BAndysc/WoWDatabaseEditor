using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Generator.Equals;
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

    [Equatable]
    [ExcludeFromCodeCoverage]
    public partial class DatabaseTableDefinitionJson
    {
        [JsonIgnore] 
        public DatabaseTable Id => new DatabaseTable(DataDatabaseType, TableName);
        
        [SetEquality]
        [JsonProperty(PropertyName = "compatibility")] 
        public List<string> Compatibility { get; set; } = new List<string>();
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "single_solution_name")] 
        public string SingleSolutionName { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "multi_solution_name")] 
        public string MultiSolutionName { get; set; } = "";
        
        [DefaultEquality]
        [DefaultValue("")]
        [JsonProperty(PropertyName = "description")] 
        public string Description { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "table_name")]
        public string TableName { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "table_index_name")]
        public ColumnFullName? TablePrimaryKeyColumnName { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "record_mode", DefaultValueHandling = DefaultValueHandling.Include)]
        [JsonConverter(typeof(StringEnumConverter))]
        public RecordMode RecordMode { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "database")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DataDatabaseType DataDatabaseType { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "only_conditions")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OnlyConditionMode IsOnlyConditionsTable { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "skip_quick_load")]
        public bool SkipQuickLoad { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "group_name")] 
        public string? GroupName { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "icon_path")] 
        public string? IconPath { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "reload_command")]
        public string? ReloadCommand { get; set; }
        
        [OrderedEquality]
        [JsonProperty(PropertyName = "sort_by")]
        public ColumnFullName[]? SortBy { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "picker")]
        [DefaultValue("")]
        public string Picker { get; set; } = "";

        [DefaultEquality]
        [JsonProperty(PropertyName = "table_name_source_field")]
        public string? TableNameSource { get; set; }

        [OrderedEquality]
        [JsonProperty(PropertyName = "primary_key")]
        public IList<ColumnFullName> PrimaryKey { get; set; } = null!;

        [OrderedEquality]
        [JsonProperty(PropertyName = "commands")]
        public IList<DatabaseCommandDefinitionJson>? Commands { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "group_by_key")]
        public string? GroupByKey { get; set; } = null;
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "conditions")]
        public DatabaseConditionReferenceJson? Condition { get; set; }

        [SetEquality]
        [JsonProperty(PropertyName = "foreign_tables")]
        public IList<DatabaseForeignTableJson>? ForeignTable { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "autofill_build_column")]
        public string? AutofillBuildColumn { get; set; } = null;

        [OrderedEquality]
        [JsonProperty(PropertyName = "groups")]
        public IList<DatabaseColumnsGroupJson> Groups { get; set; } = new List<DatabaseColumnsGroupJson>();

        [DefaultEquality]
        [JsonProperty(PropertyName = "auto_key")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GuidType? AutoKeyValue { get; set; }
        
        [IgnoreEquality]
        [JsonIgnore]
        public string FileName { get; set; } = "";
        
        [IgnoreEquality]
        [JsonIgnore]
        public string AbsoluteFileName { get; set; } = "";
        
        [IgnoreEquality]
        [JsonIgnore] 
        public IDictionary<ColumnFullName, DatabaseColumnJson> TableColumns { get; set; } = null!;
        
        [IgnoreEquality]
        [JsonIgnore] 
        public IDictionary<string, DatabaseForeignTableJson>? ForeignTableByName { get; set; }

        [IgnoreEquality]
        [JsonIgnore]
        public IList<ColumnFullName> GroupByKeys
        {
            get
            {
                if (RecordMode == RecordMode.SingleRow)
                    return PrimaryKey!;
                return new List<ColumnFullName>(){TablePrimaryKeyColumnName.HasValue && !string.IsNullOrWhiteSpace(TablePrimaryKeyColumnName.Value.ColumnName) ? TablePrimaryKeyColumnName.Value : PrimaryKey[0]};
            }
        }
        
        // single row table type ignores solution item entries quality, because there is no keys
        [IgnoreEquality] [JsonIgnore] public bool IgnoreEquality => RecordMode == RecordMode.SingleRow;

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (PrimaryKey == null || PrimaryKey.Count == 0)
            {
                if (TablePrimaryKeyColumnName.HasValue && !string.IsNullOrEmpty(TablePrimaryKeyColumnName.Value.ColumnName))
                    PrimaryKey = new List<ColumnFullName> {TablePrimaryKeyColumnName.Value};
            }
        }
    }

    public enum OnlyConditionMode
    {
        None,
        IgnoreTableCompletely,
        /// <summary>
        /// table data is loaded, but not generated in queries (neither remove nor update nor insert), only conditions
        /// </summary>
        TableReadOnly
    }

    [Flags]
    [JsonConverter(typeof(FlagJsonConverter))]
    public enum DatabaseCommandUsage
    {
        Toolbar = 1,
        ContextMenu = 2,
        All = Toolbar | ContextMenu
    }
    
    [Equatable]
    public partial class DatabaseCommandDefinitionJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "command_id")]
        public string CommandId { get; set; } = "";
        
        [CustomEquality(typeof(NullableOrderedEquality<string>))]
        [JsonProperty(PropertyName = "parameters")]
        public string[]? Parameters { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "keybinding")]
        public string? KeyBinding { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "usage", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(DatabaseCommandUsage.All)]
        public DatabaseCommandUsage Usage { get; set; } = DatabaseCommandUsage.All;
    }
    
    [Equatable]
    public partial class DatabaseConditionReferenceJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "source_type")]
        public int SourceType { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "source_group")]
        [JsonConverter(typeof(DatabaseConditionColumnConverter))]
        public DatabaseConditionColumn? SourceGroupColumn { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "source_entry")]
        public ColumnFullName? SourceEntryColumn { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "source_id")]
        public ColumnFullName? SourceIdColumn { get; set; }
        
        [DefaultEquality]
        [JsonConverter(typeof(ColumnFullNameConverter))]
        [JsonProperty(PropertyName = "set_column")]
        public ColumnFullName? SetColumn { get; set; }
        
        [OrderedEquality]
        [JsonProperty(PropertyName = "targets")]
        public IList<DatabaseConditionTargetJson>? Targets { get; set; }
    }

    [Equatable]
    public sealed partial class DatabaseConditionColumn
    {
        [DefaultEquality]
        public ColumnFullName Name { get; set; } = ColumnFullName.Parse("");
        
        [DefaultEquality]
        public bool IsAbs { get; set; }

        public int Calculate(int val)
        {
            if (IsAbs)
                return Math.Abs(val);
            return val;
        }
    }
    
    [Equatable]
    public partial class DatabaseConditionTargetJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "id", DefaultValueHandling = DefaultValueHandling.Include)]
        public uint Id { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
    }
}