using Prism.Mvvm;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseColumnHeaderViewModel : BindableBase
    {
        public string Name { get; }
        public string DatabaseName { get; }
        public float? PreferredWidth { get; }
        
        public DatabaseColumnHeaderViewModel(DatabaseColumnJson column)
        {
            Name = column.Name;
            DatabaseName = column.DbColumnName;
            PreferredWidth = column.PreferredWidth;
        }
    }
}