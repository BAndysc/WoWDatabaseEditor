using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Solution
{
    public class DatabaseTableSolutionItem : ISolutionItem
    {
        public DatabaseTableSolutionItem(DatabaseKey entry, 
            bool existsInDatabase, 
            bool conditionModified, 
            DatabaseTable tableName, 
            bool ignoreEquality, 
            Guid guid, 
            DateTime created, 
            DateTime modified,
            int build) : this(tableName, ignoreEquality, guid, created, modified, build)
        {
            Entries.Add(new SolutionItemDatabaseEntity(entry, existsInDatabase, conditionModified));
        }
        
        public DatabaseTableSolutionItem(DatabaseKey entry, 
            bool existsInDatabase, 
            bool conditionModified, 
            DatabaseTable tableName, 
            bool ignoreEquality) : this(tableName, ignoreEquality)
        {
            Entries.Add(new SolutionItemDatabaseEntity(entry, existsInDatabase, conditionModified));
        }

        public DatabaseTableSolutionItem(DatabaseTable tableName, bool ignoreEquality)
            :this(tableName, ignoreEquality, System.Guid.NewGuid(), DateTime.Now, DateTime.Now, 0) {}
        
        public DatabaseTableSolutionItem(DatabaseTable tableName, bool ignoreEquality, 
            Guid guid, 
            DateTime created, 
            DateTime modified,
            int build)
        {
            TableName = tableName;
            IgnoreEquality = ignoreEquality;
            Guid = guid;
            Created = created;
            Modified = modified;
            Build = build;
        }

        public DatabaseTableSolutionItem()
        {
        }

        public ISolutionItem Clone() => new DatabaseTableSolutionItem(TableName, IgnoreEquality, Guid, Created, Modified, Build)
        {
            Entries = Entries.Select(e => new SolutionItemDatabaseEntity(e)).ToList(),
            DeletedEntries = DeletedEntries.ToList()
        };

        public List<DatabaseKey> DeletedEntries { get; set; } = new();
        public List<SolutionItemDatabaseEntity> Entries { get; set; } = new();

        [JsonProperty] 
        public readonly Guid Guid;
        
        [JsonProperty] 
        public readonly DateTime Created;
        
        [JsonProperty] 
        public readonly DateTime Modified;
        
        [JsonProperty] 
        public readonly int Build;
        
        [JsonProperty("DefinitionId")]
        public readonly DatabaseTable TableName;

        [JsonProperty]
        public readonly bool IgnoreEquality;
        
        [JsonIgnore]
        public bool IsContainer => false;

        [JsonIgnore]
        public ObservableCollection<ISolutionItem>? Items { get; } = null;
        
        [JsonIgnore]
        public string? ExtraId => IgnoreEquality ? null : string.Join(", ", Entries);

        [JsonIgnore]
        public bool IsExportable => true;

        private bool Equals(DatabaseTableSolutionItem other)
        {
            if (TableName != other.TableName)
                return false;

            if (IgnoreEquality)
                return true;
         
            if (Entries.Count != other.Entries.Count)
                return false;

            if (other.Entries.Any(e => !Entries.Contains(e)))
                return false;
            
            if (Entries.Any(e => !other.Entries.Contains(e)))
                return false;

            if (other.DeletedEntries.Any(e => !DeletedEntries.Contains(e)))
                return false;
            
            if (DeletedEntries.Any(e => !other.DeletedEntries.Contains(e)))
                return false;
            
            return true;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DatabaseTableSolutionItem) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableName);
        }
    }

    public class SolutionItemDatabaseEntity
    {
        [JsonConstructor]
        public SolutionItemDatabaseEntity(DatabaseKey key, bool existsInDatabase, bool conditionsModified, List<EntityOrigianlField>? originalValues = null)
        {
            Key = key;
            ExistsInDatabase = existsInDatabase;
            OriginalValues = originalValues;
            ConditionsModified = conditionsModified;
        }

        [JsonProperty("k")] public readonly DatabaseKey Key;
        [JsonProperty("e")] public readonly bool ExistsInDatabase;
        [JsonProperty("c")] public readonly bool ConditionsModified;

        [JsonProperty("v")] public List<EntityOrigianlField>? OriginalValues { get; set; }
        // legacy names, now using one byte property names for space saving
        //[JsonProperty("OriginalValues")] private string _OriginalValues { set => OriginalValues = value; }

        public SolutionItemDatabaseEntity(SolutionItemDatabaseEntity copy)
        {
            Key = copy.Key;
            ExistsInDatabase = copy.ExistsInDatabase;
            ConditionsModified = copy.ConditionsModified;
            OriginalValues = copy.OriginalValues?.Select(c => new EntityOrigianlField(c)).ToList();
        }

        public override string ToString()
        {
            return Key.ToString();
        }

        protected bool Equals(SolutionItemDatabaseEntity other)
        {
            return Key == other.Key;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SolutionItemDatabaseEntity)obj);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    public class EntityOrigianlField
    {
        public EntityOrigianlField()
        {
        }

        public EntityOrigianlField(EntityOrigianlField copy)
        {
            ColumnName = copy.ColumnName;
            OriginalValue = copy.OriginalValue;
        }

        [JsonProperty("c")] [JsonConverter(typeof(ColumnFullNameConverter))] public ColumnFullName ColumnName { get; set; }
        [JsonProperty("v")] public object? OriginalValue { get; set; }
        
        // legacy names, now using one byte property names for space saving
        [JsonProperty("ColumnName")] [JsonConverter(typeof(ColumnFullNameConverter))] private ColumnFullName _ColumnName { set => ColumnName = value; }
        [JsonProperty("OriginalValue")] private object? _OriginalValue { set => OriginalValue = value; }
    }
}