using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.DatabaseEditors.Solution
{
    public class DatabaseTableSolutionItem : ISolutionItem
    {
        public DatabaseTableSolutionItem(uint entry, bool existsInDatabase, string definitionId)
        {
            Entries.Add(new SolutionItemDatabaseEntity(entry, existsInDatabase));
            DefinitionId = definitionId;
        }

        public DatabaseTableSolutionItem(string definitionId)
        {
            DefinitionId = definitionId;
        }

        public DatabaseTableSolutionItem()
        {
            DefinitionId = null!;
        }

        public ISolutionItem Clone() => new DatabaseTableSolutionItem(DefinitionId)
            { Entries = Entries.Select(e => new SolutionItemDatabaseEntity(e)).ToList() };

        public List<SolutionItemDatabaseEntity> Entries { get; set; } = new();

        [JsonProperty]
        public readonly string DefinitionId;

        [JsonIgnore]
        public bool IsContainer => false;

        [JsonIgnore]
        public ObservableCollection<ISolutionItem>? Items { get; } = null;
        
        [JsonIgnore]
        public string ExtraId => string.Join(", ", Entries);

        [JsonIgnore]
        public bool IsExportable => true;

        private bool Equals(DatabaseTableSolutionItem other)
        {
            if (Entries.Count != other.Entries.Count)
                return false;

            if (DefinitionId != other.DefinitionId)
                return false;

            if (other.Entries.Any(e => !Entries.Contains(e)))
                return false;
            
            if (Entries.Any(e => !other.Entries.Contains(e)))
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
            return HashCode.Combine(DefinitionId);
        }
    }

    public class SolutionItemDatabaseEntity
    {
        [JsonConstructor]
        public SolutionItemDatabaseEntity(uint key, bool existsInDatabase, List<EntityOrigianlField>? originalValues = null)
        {
            Key = key;
            ExistsInDatabase = existsInDatabase;
            OriginalValues = originalValues;
        }

        public readonly uint Key;
        public readonly bool ExistsInDatabase;

        public SolutionItemDatabaseEntity(SolutionItemDatabaseEntity copy)
        {
            Key = copy.Key;
            ExistsInDatabase = copy.ExistsInDatabase;
            OriginalValues = copy.OriginalValues?.Select(c => new EntityOrigianlField(c)).ToList();
        }

        public List<EntityOrigianlField>? OriginalValues { get; set; }

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

        [JsonProperty("c")] public string ColumnName { get; set; } = "";
        [JsonProperty("v")] public object? OriginalValue { get; set; }
        
        // legacy names, now using one byte property names for space saving
        [JsonProperty("ColumnName")] private string _ColumnName { set => ColumnName = value; }
        [JsonProperty("OriginalValue")] private object? _OriginalValue { set => OriginalValue = value; }
    }
}