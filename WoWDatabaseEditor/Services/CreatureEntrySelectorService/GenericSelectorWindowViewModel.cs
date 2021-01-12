using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Mvvm;
using WoWDatabaseEditor.Extensions;

namespace WoWDatabaseEditor.Services.CreatureEntrySelectorService
{
    public class GenericSelectorWindowViewModel<T> : BindableBase
    {
        private readonly Func<T, uint> entryGetter;
        private readonly Func<T, string> index;

        private readonly CollectionViewSource items;

        private string search = "";

        public GenericSelectorWindowViewModel(IEnumerable<ColumnDescriptor> columns,
            IEnumerable<T> collection,
            Func<T, uint> entryGetter,
            Func<T, string> index)
        {
            this.entryGetter = entryGetter;
            this.index = index;
            RawItems = new ObservableCollection<T>();

            Columns = new ObservableCollection<ColumnDescriptor>();

            foreach (var column in columns)
                Columns.Add(column);

            foreach (T item in collection)
                RawItems.Add(item);

            items = new CollectionViewSource();
            items.Source = RawItems;
            items.Filter += ItemsOnFilter;
        }

        public ObservableCollection<T> RawItems { get; set; }
        public ObservableCollection<ColumnDescriptor> Columns { get; set; }

        public ICollectionView AllItems => items.View;

        public T? SelectedItem { get; set; }

        public string SearchText
        {
            get => search;
            set
            {
                SetProperty(ref search, value);
                items.View.Refresh();
            }
        }

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            T model = (T) filterEventArgs.Item;
            filterEventArgs.Accepted = string.IsNullOrEmpty(SearchText) || index(model).ToLower().Contains(SearchText.ToLower());
        }

        public uint GetEntry()
        {
            if (SelectedItem != null)
                return entryGetter(SelectedItem);

            uint res;
            uint.TryParse(SearchText, out res);
            return res;
        }
    }
}