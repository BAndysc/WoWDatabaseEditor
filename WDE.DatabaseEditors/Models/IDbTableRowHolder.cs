using System.Collections.ObjectModel;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableRowHolder
    {
        ObservableCollection<IDbTableRow> Rows { get; }

        void AddRow(IDbTableFieldFactory creator);

        void DeleteRow(int at);
    }
}