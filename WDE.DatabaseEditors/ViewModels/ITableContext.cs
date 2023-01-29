using System.Collections.Generic;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public interface ITableContext
    {
        DatabaseEntity AddRow(DatabaseKey key);
        DatabaseKey? SelectedTableKey { get; }
        bool SupportsMultiSelect { get; }
        ICollection<DatabaseEntity>? MultiSelectionEntities { get; }
        System.IDisposable BulkEdit(string text);
    }
}