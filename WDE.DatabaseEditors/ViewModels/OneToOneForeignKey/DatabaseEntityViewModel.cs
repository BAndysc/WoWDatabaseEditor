using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.OneToOneForeignKey
{
    public partial class DatabaseFieldsGroup
    {
        [Notify] private bool isExpanded;
        [Notify] private bool showHeader = true;
        public string GroupName { get; }

        public DatabaseFieldsGroup(string groupName)
        {
            GroupName = groupName;
            IsExpanded = groupName != "Advanced";
        }

        public ObservableCollection<SingleRecordDatabaseCellViewModel> Cells { get; } = new();
    }
    
    public class DatabaseEntityViewModel : BindableBase, ITableRow
    {
        public DatabaseEntity Entity { get; }
        
        public bool IsPhantomEntity => Entity.Phantom;
        public ObservableCollection<DatabaseFieldsGroup> Groups { get; } = new();
        
        public DatabaseEntityViewModel(DatabaseEntity entity)
        {
            Entity = entity;
        }

        public IReadOnlyList<ITableCell> CellsList => Groups.SelectMany(x => x.Cells).ToList();
        public event Action<ITableRow>? Changed;
        public event Action<DatabaseEntityViewModel, SingleRecordDatabaseCellViewModel, ColumnFullName>? ChangedCell;

        public void RaiseChanged(SingleRecordDatabaseCellViewModel cell, ColumnFullName column)
        {
            ChangedCell?.Invoke(this, cell, column);
            Changed?.Invoke(this);
        }

        internal void DisposeAllCells()
        {
            foreach (var cell in CellsList)
            {
                ((SingleRecordDatabaseCellViewModel)cell).Dispose();
            }
        }
    }
}