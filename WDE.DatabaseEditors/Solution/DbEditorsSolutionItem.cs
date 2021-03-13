using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Solution
{
    public class DbEditorsSolutionItem : ISolutionItem
    {
        public DbEditorsSolutionItem() {}
        
        public DbEditorsSolutionItem(uint entry, string tableDataLoaderMethodName, DbTableData tableData)
        {
            TableData = tableData;
            Items = null;
            ItemDescription = tableData.TableDescription;
            Entry = entry;
            TableDataLoaderMethodName = tableDataLoaderMethodName;
            TableName = tableData.TableName;
        }
        
        public uint Entry { get; set; }
        public string ItemDescription { get; set; }
        public string TableName { get; set; }
        public string TableDataLoaderMethodName { get; set; }

        [JsonIgnore] public DbTableData? TableData { get; } = null;
        
        public Dictionary<string, DbTableSolutionItemModifiedField> ModifiedFields { get; set; }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items { get; }
        public string ExtraId => Entry.ToString();
        public bool IsExportable { get; } = true;
    }
}