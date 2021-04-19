using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.Common;

namespace WDE.DatabaseEditors.Solution
{
    public class DatabaseTableSolutionItem : ISolutionItem
    {
        public DatabaseTableSolutionItem(uint entry, string tableId)
        {
            Entry = entry;
            TableId = tableId;
            ModifiedFields = new();
        }
        
        public uint Entry { get; }
        public string TableId { get; }

        public Dictionary<string, DatabaseSolutionItemModifiedField> ModifiedFields { get; set; }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem>? Items { get; } = null;
        public string ExtraId => Entry.ToString();
        public bool IsExportable { get; } = true;

        private bool Equals(DatabaseTableSolutionItem other)
        {
            return Entry == other.Entry && TableId == other.TableId;
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
            return HashCode.Combine(Entry, TableId);
        }
    }
}