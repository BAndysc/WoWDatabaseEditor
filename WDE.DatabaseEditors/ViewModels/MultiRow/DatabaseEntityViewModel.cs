using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Mvvm;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    // single database entity
    public class DatabaseEntityViewModel : BindableBase, ITableRow
    {
        public string Name { get; }
        public DatabaseKey Key => Entity.Key;
        public DatabaseEntity Entity { get; }
        public ObservableCollection<DatabaseCellViewModel> Cells { get; } = new();

        public DatabaseEntityViewModel(DatabaseEntity entity, string name)
        {
            Name = name;
            Entity = entity;
        }

        public IReadOnlyList<ITableCell> CellsList => Cells;

        private bool duplicate;
        /// <summary>
        /// A visual marker whether this entity is a duplicate of another one
        /// </summary>
        public bool Duplicate
        {
            get => duplicate;
            set
            {
                duplicate = value;
                Changed?.Invoke(this);
            }
        }

        public event Action<ITableRow>? Changed;

        public void RaiseChanged(DatabaseCellViewModel cell, ColumnFullName? fieldName)
        {
            Changed?.Invoke(this);
        }
    }
}