using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using AvaloniaStyles.Controls.FastTableView;
using DynamicData.Binding;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseEntitiesGroupViewModel : ObservableCollectionExtended<DatabaseEntityViewModel>, ITableRowGroup
    {
        private bool pauseNotifications = false;
        public DatabaseKey Key { get; }
        public string Name { get; }

        public DatabaseEntitiesGroupViewModel(DatabaseKey key, string name)
        {
            Key = key;
            Name = name;
            base.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (pauseNotifications)
                return;
            
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ITableRow row in e.NewItems!)
                {
                    row.Changed += RowOnChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ITableRow row in e.OldItems!)
                {
                    row.Changed -= RowOnChanged;
                }
            }
            else
            {
                throw new Exception("Only add or remove supported to unbind the items");
            }
            RowsChanged?.Invoke(this);
        }

        private void RowOnChanged(ITableRow obj)
        {
            RowChanged?.Invoke(this, obj);
        }

        public DatabaseEntityViewModel? GetAndRemove(DatabaseEntity entity)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (this[i].Entity == entity)
                {
                    var vm = this[i];
                    RemoveAt(i);
                    return vm;
                }
            }

            return null;
        }

        public IReadOnlyList<ITableRow> Rows => this;
        public event Action<ITableRowGroup, ITableRow>? RowChanged;
        public event Action<ITableRowGroup>? RowsChanged;

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// fast ordering, without firing notifications
        /// </summary>
        /// <param name="columnIndex"></param>
        public void OrderByIndices(List<int> indices)
        {
            var sorted = indices.Select(i => this[i]).ToList();
            pauseNotifications = true;
            try
            {
                Clear();
                AddRange(sorted);
                RowsChanged?.Invoke(this);
            }
            finally
            {
                pauseNotifications = false;
            }
        }
    }
}