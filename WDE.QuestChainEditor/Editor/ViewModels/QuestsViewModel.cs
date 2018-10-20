using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class QuestsViewModel : BindableBase
    {
        private readonly ObservableCollection<QuestDefinition> _allItems = new ObservableCollection<QuestDefinition>();

        private CollectionViewSource _items;
        private QuestDefinition _selectedItem;
        private string _searchBox;

        public string SearchBox
        {
            get { return _searchBox; }
            set { SetProperty(ref _searchBox, value); _items.View.Refresh(); }
        }

        public QuestDefinition SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        public QuestDefinition ChosenItem
        {
            get {
                if (SelectedItem != null)
                    return SelectedItem;

                AllItems.MoveCurrentToFirst();
                return AllItems.CurrentItem as QuestDefinition;
            }
        }

        public ICollectionView AllItems => _items.View;

        public QuestsViewModel(IQuestsProvider registry)
        {
            _allItems.AddRange(registry.Quests);

            _items = new CollectionViewSource
            {
                Source = _allItems
            };
            _items.Filter += ItemsOnFilter;

            if (_items.View.MoveCurrentToFirst())
            {
                SelectedItem = _items.View.CurrentItem as QuestDefinition;
            }
        }
        
        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            var item = filterEventArgs.Item as QuestDefinition;

            if (string.IsNullOrEmpty(SearchBox))
            {
                filterEventArgs.Accepted = true;
                return;
            }

            uint numeric;

            filterEventArgs.Accepted = item.Title.ToLower().Contains(SearchBox.ToLower());

            if (uint.TryParse(SearchBox, out numeric))
                filterEventArgs.Accepted |= item.Id == numeric;            
        }
    }
}
