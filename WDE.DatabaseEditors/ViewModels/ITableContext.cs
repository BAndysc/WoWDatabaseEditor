using System.Collections.Generic;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public interface ITableContext
    {
        DatabaseEntity AddRow(DatabaseKey key, int? index = null);
        bool SupportsMultiSelect { get; }
        IReadOnlyList<DatabaseEntity>? MultiSelectionEntities { get; }
        System.IDisposable BulkEdit(string text);
    }
}