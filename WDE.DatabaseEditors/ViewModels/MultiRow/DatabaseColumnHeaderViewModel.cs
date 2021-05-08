using Prism.Mvvm;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseColumnHeaderViewModel : BindableBase
    {
        public string Name { get; }
        public string? Help { get; }
        public string DatabaseName { get; }
        public float? PreferredWidth { get; }
        
        public DatabaseColumnHeaderViewModel(DatabaseColumnJson column)
        {
            Name = column.Name;
            DatabaseName = column.DbColumnName;
            Help = column.Help;
            PreferredWidth = column.PreferredWidth;
        }

        public DatabaseColumnHeaderViewModel(string name, string databaseName)
        {
            Name = name;
            DatabaseName = databaseName;
        }
    }
}