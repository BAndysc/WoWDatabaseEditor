using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.DatabaseEditors.Solution
{
    public class DatabaseTableSolutionItem : ISolutionItem
    {
        public DatabaseTableSolutionItem(uint entry, string definitionId)
        {
            Entries.Add(entry);
            DefinitionId = definitionId;
        }

        public List<uint> Entries { get; set; } = new();
        
        public string DefinitionId { get; }

        public Dictionary<uint, List<EntityOrigianlField>> OriginalValues { get; set; } = new();

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
            return Entries == other.Entries && DefinitionId == other.DefinitionId;
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
            return HashCode.Combine(Entries, DefinitionId);
        }
    }

    public class EntityOrigianlField
    {
        public string ColumnName { get; set; } = "";
        public object? OriginalValue { get; set; }
    }
}