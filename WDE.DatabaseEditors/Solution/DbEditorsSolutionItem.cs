using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Solution
{
    public class DbEditorsSolutionItem : ISolutionItem
    {
        public DbEditorsSolutionItem(uint entry, DbTableContentType tableContentType, bool isMultiRecord,
            Dictionary<string, DbTableSolutionItemModifiedField> modifiedFields)
        {
            Entry = entry;
            TableContentType = tableContentType;
            IsMultiRecord = isMultiRecord;
            ModifiedFields = modifiedFields;
        }
        
        public uint Entry { get;  }
        public DbTableContentType TableContentType { get; }
        public bool IsMultiRecord { get; }

        public Dictionary<string, DbTableSolutionItemModifiedField> ModifiedFields { get; set; }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem>? Items { get; } = null;
        public string ExtraId => Entry.ToString();
        public bool IsExportable { get; } = true;

        private bool Equals(DbEditorsSolutionItem other)
        {
            return Entry == other.Entry && TableContentType == other.TableContentType && IsMultiRecord == other.IsMultiRecord;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DbEditorsSolutionItem) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entry, (int) TableContentType, IsMultiRecord);
        }
    }
}