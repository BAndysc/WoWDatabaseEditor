using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

using WDE.Common.Database;
using WDE.Common.Parameters;
using WoWDatabaseEditor.Extensions;
using Prism.Ioc;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    public class ItemFromListProviderViewModel : BindableBase
    {
        private readonly bool _asFlags;
        public ObservableCollection<KeyValuePair<int, CheckableSelectOption>> RawItems { get; set; }
        public ObservableCollection<ColumnDescriptor> Columns { get; set; }

        private CollectionViewSource _items;

        public ICollectionView AllItems => _items.View;

        public KeyValuePair<int, CheckableSelectOption>? SelectedItem { get; set; }

        private string _search;
        public string SearchText
        {
            get { return _search; }
            set { SetProperty(ref _search, value); _items.View.Refresh(); }
        }

        public ItemFromListProviderViewModel(Dictionary<int, SelectOption> items, bool asFlags)
        {
            _asFlags = asFlags;
            RawItems = new ObservableCollection<KeyValuePair<int, CheckableSelectOption>>();

            foreach (int key in items.Keys)
                RawItems.Add(new KeyValuePair<int, CheckableSelectOption>(key, new CheckableSelectOption(items[key])));

            Columns = new ObservableCollection<ColumnDescriptor>
            {
                new ColumnDescriptor { HeaderText = "Key", DisplayMember = "Key"},
                new ColumnDescriptor { HeaderText = "Name", DisplayMember = "Value.Name" },
                new ColumnDescriptor { HeaderText = "Description", DisplayMember = "Value.Description" },
            };

            if (asFlags)
                Columns.Insert(0, new ColumnDescriptor(){HeaderText = "", DisplayMember= "Value.IsChecked", CheckboxMember = true});

            _items = new CollectionViewSource();
            _items.Source = RawItems;
            _items.Filter += ItemsOnFilter;
        }

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            KeyValuePair<int, CheckableSelectOption>? model = filterEventArgs.Item as KeyValuePair<int, CheckableSelectOption>?;
            filterEventArgs.Accepted = string.IsNullOrEmpty(SearchText) || model.Value.Value.Name.ToLower().Contains(SearchText.ToLower());
        }

        public int GetEntry()
        {
            if (_asFlags)
            {
                int val = 0;
                foreach (var item in RawItems)
                {
                    if (item.Value.IsChecked)
                        val |= item.Key;
                }
                return val;
            }
            else
            {
                if (SelectedItem != null)
                    return SelectedItem.Value.Key;

                int res;
                int.TryParse(SearchText, out res);
                return res;
            }
        }
    }

    public class CheckableSelectOption : SelectOption
    {

        public CheckableSelectOption(SelectOption selectOption)
        {
            Name = selectOption.Name;
            Description = selectOption.Description;
        }

        public bool IsChecked { get; set; }
    }
}
