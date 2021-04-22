using System.Collections.ObjectModel;
using Prism.Mvvm;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.ViewModels
{
    public class DatabaseRowViewModel : BindableBase
    {
        public string CategoryName { get; }
        public int CategoryIndex { get; }
        public string Name { get; } = "";
        public int Order { get; }
        public bool IsReadOnly { get; }
        public bool CanBeNull { get; }
        public ObservableCollection<DatabaseCellViewModel> Cells { get; } = new();
        public DbEditorTableGroupFieldJson ColumnData { get; }
        public DatabaseColumnsGroupJson GroupData { get; }

        public DatabaseRowViewModel(DbEditorTableGroupFieldJson columnData, DatabaseColumnsGroupJson group, int categoryIndex, int index)
        {
            GroupData = group;
            ColumnData = columnData;
            CanBeNull = columnData.CanBeNull;
            IsReadOnly = columnData.IsReadOnly;
            CategoryName = group.Name;
            CategoryIndex = categoryIndex;
            Order = index;
            Name = columnData.Name;
        }
    }
}