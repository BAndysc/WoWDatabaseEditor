using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WoWDatabaseEditor.Extensions;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    public class ItemFromListProviderViewModel : BindableBase
    {
        private readonly bool asFlags;

        private readonly CollectionViewSource items;

        private string search = "";

        public ItemFromListProviderViewModel(Dictionary<int, SelectOption> items, bool asFlags)
        {
            this.asFlags = asFlags;
            RawItems = new ObservableCollection<KeyValuePair<int, CheckableSelectOption>>();

            foreach (int key in items.Keys)
                RawItems.Add(new KeyValuePair<int, CheckableSelectOption>(key, new CheckableSelectOption(items[key])));

            Columns = new ObservableCollection<ColumnDescriptor>
            {
                new("Key", "Key", 50),
                new("Name", "Value.Name"),
                new("Description", "Value.Description")
            };

            if (asFlags)
                Columns.Insert(0, new ColumnDescriptor("", "Value.IsChecked", null, true));

            this.items = new CollectionViewSource();
            this.items.Source = RawItems;
            this.items.Filter += ItemsOnFilter;
        }

        public ObservableCollection<KeyValuePair<int, CheckableSelectOption>> RawItems { get; set; }
        public ObservableCollection<ColumnDescriptor> Columns { get; set; }

        public ICollectionView AllItems => items.View;

        public KeyValuePair<int, CheckableSelectOption>? SelectedItem { get; set; }

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
            var model = filterEventArgs.Item as KeyValuePair<int, CheckableSelectOption>?;
            filterEventArgs.Accepted = string.IsNullOrEmpty(SearchText) ||
                                       model != null && model.Value.Value.Name.ToLower().Contains(SearchText.ToLower());
        }

        public int GetEntry()
        {
            if (asFlags)
            {
                var val = 0;
                foreach (var item in RawItems)
                {
                    if (item.Value.IsChecked)
                        val |= item.Key;
                }

                return val;
            }

            if (SelectedItem != null)
                return SelectedItem.Value.Key;

            int res;
            int.TryParse(SearchText, out res);
            return res;
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