using AvaloniaStyles.Controls.FastTableView;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public partial class DatabaseColumnHeaderViewModel : BindableBase, ITableColumnHeader
    {
        [Notify] private bool isVisible = true;
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
            width = PreferredWidth ?? 100;
        }

        public DatabaseColumnHeaderViewModel(string name, string databaseName)
        {
            Name = name;
            DatabaseName = databaseName;
        }

        public string Header => Name;
        [Notify] private double width;
    }
}