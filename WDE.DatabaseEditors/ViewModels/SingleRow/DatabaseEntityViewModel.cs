using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Mvvm;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.SingleRow
{
    // single row database entity
    public class DatabaseEntityViewModel : BindableBase, ITableRow
    {
        public DatabaseEntity Entity { get; }
        public ObservableCollection<SingleRecordDatabaseCellViewModel> Cells { get; } = new();
        public bool IsPhantomEntity => Entity.Phantom;

        public DatabaseEntityViewModel(DatabaseEntity entity)
        {
            Entity = entity;
        }

        public IReadOnlyList<ITableCell> CellsList => Cells;
        public event Action<ITableRow>? Changed;
        public event Action<DatabaseEntityViewModel, SingleRecordDatabaseCellViewModel, ColumnFullName>? ChangedCell;

        public void RaiseChanged(SingleRecordDatabaseCellViewModel cell, ColumnFullName column)
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