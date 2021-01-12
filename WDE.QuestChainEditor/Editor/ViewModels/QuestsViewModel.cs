using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Mvvm;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class QuestsViewModel : BindableBase
    {
        private readonly ObservableCollection<QuestDefinition> allItems = new();

        private readonly CollectionViewSource items;
        private string searchBox;
        private QuestDefinition selectedItem;

        public QuestsViewModel(IQuestsProvider registry)
        {
            allItems.AddRange(registry.Quests);

            items = new CollectionViewSource
            {
                Source = allItems
            };
            items.Filter += ItemsOnFilter;

            if (items.View.MoveCurrentToFirst())
                SelectedItem = items.View.CurrentItem as QuestDefinition;
        }

        public string SearchBox
        {
            get => searchBox;
            set
            {
                SetProperty(ref searchBox, value);
                items.View.Refresh();
            }
        }

        public QuestDefinition SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        public QuestDefinition ChosenItem
        {
            get
            {
                if (SelectedItem != null)
                    return SelectedItem;

                AllItems.MoveCurrentToFirst();
                return AllItems.CurrentItem as QuestDefinition;
            }
        }

        public ICollectionView AllItems => items.View;

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            QuestDefinition item = filterEventArgs.Item as QuestDefinition;

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