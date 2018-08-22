using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

using WDE.Common.Database;
using WoWDatabaseEditor.Extensions;
using Prism.Ioc;

namespace WoWDatabaseEditor.Services.CreatureEntrySelectorService
{
    
    public class GenericSelectorWindowViewModel<T> : BindableBase
    {
        private readonly Func<T, uint> _entryGetter;
        private readonly Func<T, string> _index;
        public ObservableCollection<T> RawItems { get; set; }
        public ObservableCollection<ColumnDescriptor> Columns { get; set; }

        private CollectionViewSource _items;

        public ICollectionView AllItems => _items.View;

        public T SelectedItem { get; set; }

        private string _search;
        public string SearchText
        {
            get { return _search; }
            set { SetProperty(ref _search, value); _items.View.Refresh();  }
        }

        public GenericSelectorWindowViewModel(IEnumerable<ColumnDescriptor> columns, IEnumerable<T> collection, Func<T, uint> entryGetter, Func<T, string> index)
        {
            _entryGetter = entryGetter;
            _index = index;
            RawItems = new ObservableCollection<T>();

            Columns = new ObservableCollection<ColumnDescriptor>();

            foreach (var column in columns)
                Columns.Add(column);

            foreach (var item in collection)
            {
                RawItems.Add(item);
            }

            _items = new CollectionViewSource();
            _items.Source = RawItems;
            _items.Filter += ItemsOnFilter;
        }

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        { 
            T model = (T)filterEventArgs.Item;
            filterEventArgs.Accepted = string.IsNullOrEmpty(SearchText) || _index(model).ToLower().Contains(SearchText.ToLower());
        }

        public uint GetEntry()
        {
            if (SelectedItem != null)
                return _entryGetter(SelectedItem);

            uint res;
            uint.TryParse(SearchText, out res);
            return res;
        }
    }
}
