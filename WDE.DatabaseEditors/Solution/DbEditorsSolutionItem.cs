using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Solution
{
    public class DbEditorsSolutionItem : ISolutionItem
    {
        public DbEditorsSolutionItem() {} // for serialization to avoid crash
        
        public DbEditorsSolutionItem(uint entry, string tableDataLoaderMethodName, IDbTableData tableData)
        {
            TableData = tableData;
            Items = null;
            ItemDescription = tableData.TableDescription;
            Entry = entry;
            TableDataLoaderMethodName = tableDataLoaderMethodName;
            TableName = tableData.TableName;
            DbTableName = tableData.DbTableName;
            KeyColumnName = tableData.TableIndexFieldName;
            IsMultiRecord = tableData is DbMultiRecordTableData;
        }
        
        public uint Entry { get; set; }
        public string ItemDescription { get; set; }
        public string TableName { get; set; }
        public string DbTableName { get; set; }
        public string KeyColumnName { get; set; }
        public string TableDataLoaderMethodName { get; set; }
        public bool IsMultiRecord { get; set; }

        // just cache to avoid loading data from db each time
        [JsonIgnore] public IDbTableData? TableData { get; set; } = null;
        
        public Dictionary<string, DbTableSolutionItemModifiedField> ModifiedFields { get; set; }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items { get; }
        public string ExtraId => Entry.ToString();
        public bool IsExportable { get; } = true;
    }
}