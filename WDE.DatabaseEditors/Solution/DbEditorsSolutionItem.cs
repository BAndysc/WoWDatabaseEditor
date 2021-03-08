using System.Collections.ObjectModel;
using WDE.Common;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Solution
{
    public class DbEditorsSolutionItem : ISolutionItem
    {
        public DbEditorsSolutionItem(DbTableData tableData)
        {
            TableData = tableData;
            Items = null;
            ExtraId = TableData.TableName;
        }

        public DbTableData TableData { get; }
        
        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items { get; }
        public string ExtraId { get; }
        public bool IsExportable { get; } = true;
    }
}