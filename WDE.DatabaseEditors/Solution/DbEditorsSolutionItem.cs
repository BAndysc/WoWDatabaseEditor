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

        // just cache to avoid loading data from db each time
        [JsonIgnore] public IDbTableData? TableData { get; private set; } = null;
        public void CacheTableData(IDbTableData tableData) => TableData = tableData;
        
        public Dictionary<string, DbTableSolutionItemModifiedField> ModifiedFields { get; set; }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items { get; } = null;
        public string ExtraId => Entry.ToString();
        public bool IsExportable { get; } = true;
    }
}