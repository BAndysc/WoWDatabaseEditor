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
        public ObservableCollection<DatabaseCellViewModel> Cells { get; } = new();

        public DatabaseRowViewModel(DbEditorTableGroupFieldJson columnData, string categoryName, int categoryIndex, int index)
        {
            IsReadOnly = columnData.IsReadOnly;
            CategoryName = categoryName;
            CategoryIndex = categoryIndex;
            Order = index;
            Name = columnData.Name;
        }
    }
}