using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Mvvm;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.SingleRow
{
    // single row database entity
    public class DatabaseEntityViewModel : BindableBase, ITableRow
    {
        public DatabaseKey Key => Entity.Key;
        public DatabaseEntity Entity { get; }
        public ObservableCollection<SingleRecordDatabaseCellViewModel> Cells { get; } = new();

        public DatabaseEntityViewModel(DatabaseEntity entity)
        {
            Entity = entity;
        }

        public IReadOnlyList<ITableCell> CellsList => Cells;
        public event Action<ITableRow>? Changed;
        public event Action<DatabaseEntityViewModel, SingleRecordDatabaseCellViewModel, string>? ChangedCell;

        public void RaiseChanged(SingleRecordDatabaseCellViewModel cell, string column)
        {
            ChangedCell?.Invoke(this, cell, column);
            Changed?.Invoke(this);
        }

        internal void DisposeAllCells()
        {
            foreach (var cell in Cells)
            {
                cell.Dispose();
            }
        }
    }
}