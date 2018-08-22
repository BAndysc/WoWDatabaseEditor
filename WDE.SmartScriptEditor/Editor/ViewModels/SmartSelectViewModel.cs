using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

using WDE.SmartScriptEditor.Data;
using Prism.Ioc;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartSelectViewModel : BindableBase
    {
        private readonly Func<SmartGenericJsonData, bool> _predicate;
        private readonly ObservableCollection<SmartItem> _allItems = new ObservableCollection<SmartItem>();

        private CollectionViewSource _items;
        private SmartItem _selectedItem;
        private string _searchBox;

        public ICollectionView AllItems => _items.View;

        public string SearchBox
        {
            get { return _searchBox; }
            set { SetProperty(ref _searchBox, value); _items.View.Refresh(); }
        }

        public SmartItem SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value);  }
        }

        public SmartSelectViewModel(string file, SmartType type, Func<SmartGenericJsonData, bool> predicate)
        {
            _predicate = predicate;
            string group = null;
            foreach (string line in File.ReadLines("SmartData/" + file))
            {
                if (line.IndexOf(" ", StringComparison.Ordinal) == 0)
                {
                    if (!SmartDataManager.GetInstance().Contains(type, line.Trim()))
                        continue;

                    SmartItem i = new SmartItem();
                    var data = SmartDataManager.GetInstance().GetDataByName(type, line.Trim());

                    i.Group = group;
                    i.Name = data.NameReadable;
                    i.Id = data.Id;
                    i.Help = data.Help;
                    i.Deprecated = data.Deprecated;
                    i.Data = data;

                    _allItems.Add(i);
                }
                else
                {
                    group = line;
                }
            }

            _items = new CollectionViewSource();
            _items.Source = _allItems;
            _items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            _items.Filter += ItemsOnFilter;

            if (_items.View.MoveCurrentToFirst())
            {
                SelectedItem = _items.View.CurrentItem as SmartItem;
            }
        }

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            var item = filterEventArgs.Item as SmartItem;

            if (_predicate != null && !_predicate(item.Data))
                filterEventArgs.Accepted = false;
            else
                filterEventArgs.Accepted = string.IsNullOrEmpty(SearchBox) || item.Name.ToLower().Contains(SearchBox.ToLower());
        }
    }

    public class SmartItem
    {
        public string Name { get; set; }
        public bool Deprecated { get; set; }
        public string Help { get; set; }
        public int Id { get; set; }
        public string Group { get; set; }
        public SmartGenericJsonData Data;
    }
}
