using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using WDE.Blueprints.Data;
using WDE.Blueprints.Enums;
using WDE.Blueprints.Managers;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class NodesViewModel : BindableBase
    {
        private readonly ObservableCollection<NodeDefinition> _allItems = new ObservableCollection<NodeDefinition>();

        private CollectionViewSource _items;
        private NodeDefinition _selectedItem;
        private string _searchBox;
        private ConnectionViewModel contextConnection = null;

        public string SearchBox
        {
            get { return _searchBox; }
            set { SetProperty(ref _searchBox, value); _items.View.Refresh(); }
        }

        public NodeDefinition SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        public ICollectionView AllItems => _items.View;

        public NodesViewModel(IBlueprintDefinitionsRegistry registry)
        {
            _allItems.AddRange(registry.GetAllDefinitions());

            _items = new CollectionViewSource
            {
                Source = _allItems
            };
            _items.GroupDescriptions.Add(new PropertyGroupDescription("NodeType"));
            _items.Filter += ItemsOnFilter;

            if (_items.View.MoveCurrentToFirst())
            {
                SelectedItem = _items.View.CurrentItem as NodeDefinition;
            }
        }


        internal void SetCurrentConnectionContext(ConnectionViewModel context)
        {
            contextConnection = context;
            SearchBox = "";
        }

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            var item = filterEventArgs.Item as NodeDefinition;

            if (contextConnection != null)
            {
                if (contextConnection.From != null)
                {
                    bool accept = false;
                    if (contextConnection.From.IOType == IOType.Exec)
                    {
                        if (item.NodeType == NodeType.Statement)
                            accept = true;
                    }
                    if (!accept && (item.Inputs == null || item.Inputs.All(o => o.Type != contextConnection.From.IOType)))
                    {
                        filterEventArgs.Accepted = false;
                        return;
                    }
                }
                else if (contextConnection.To != null)
                {
                    bool accept = false;
                    if (contextConnection.To.IOType == IOType.Exec)
                    {
                        if (item.NodeType == NodeType.Statement)
                            accept = true;
                    }
                    if (!accept && (item.Outputs == null || item.Outputs.All(o => o.Type != contextConnection.To.IOType)))
                    {
                        filterEventArgs.Accepted = false;
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(SearchBox))
            {
                filterEventArgs.Accepted = true;
                return;
            }

            if (_allItems.Where(i => i.Header.ToLower() == SearchBox.ToLower()).Count() == 1)
            {
                filterEventArgs.Accepted = item.Header.ToLower() == SearchBox.ToLower();
            }
            else
            {
                filterEventArgs.Accepted = item.Header.ToLower().Contains(SearchBox.ToLower());

            }
        }
    }
}
