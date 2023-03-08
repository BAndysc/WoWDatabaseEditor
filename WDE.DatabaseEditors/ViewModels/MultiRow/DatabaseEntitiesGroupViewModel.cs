using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public partial class DatabaseEntitiesGroupViewModel : CustomObservableCollection<DatabaseEntityViewModel>, ITableRowGroup
    {
        private bool isExpanded = true;
        public DatabaseKey Key { get; }
        public string Name { get; }
        
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                isExpanded = value;
                RowsChanged?.Invoke(this);
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public DatabaseEntitiesGroupViewModel(DatabaseKey key, string name)
        {
            Key = key;
            Name = name;
        }

        public override void OrderByIndices(List<int> indices)
        {
            base.OrderByIndices(indices);
            RowsChanged?.Invoke(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (pauseNotifications)
                return;
            
            base.OnCollectionChanged(e);
            
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
    }
}