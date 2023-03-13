using System.Linq;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public partial class DatabaseColumnHeaderViewModel : ObservableBase, ITableColumnHeader
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
            if (!string.IsNullOrWhiteSpace(column.ColumnIdForUi))
                ColumnIdForUi = column.ColumnIdForUi;
            else if (DatabaseName.Length > 0)
                ColumnIdForUi = DatabaseName;
            else
                ColumnIdForUi = string.Concat(Name.Where(char.IsLetter));
        }

        public string ColumnIdForUi { get; }
        public string Header => Name;
        [Notify] private double width;
    }
}