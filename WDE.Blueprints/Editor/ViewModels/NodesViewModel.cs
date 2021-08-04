using WDE.Blueprints.Data;
using WDE.Blueprints.Enums;
using WDE.Blueprints.Managers;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class NodesViewModel : BindableBase
    {
        private readonly ObservableCollection<NodeDefinition> allItems = new();

        private readonly CollectionViewSource items;
        private string searchBox;
        private NodeDefinition selectedItem;
        private ConnectionViewModel contextConnection;

        public NodesViewModel(IBlueprintDefinitionsRegistry registry)
        {
            allItems.AddRange(registry.GetAllDefinitions());

            items = new CollectionViewSource
            {
                Source = allItems
            };
            items.GroupDescriptions.Add(new PropertyGroupDescription("NodeType"));
            items.Filter += ItemsOnFilter;

            if (items.View.MoveCurrentToFirst())
                SelectedItem = items.View.CurrentItem as NodeDefinition;
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

        public NodeDefinition SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        public ICollectionView AllItems => items.View;


        internal void SetCurrentConnectionContext(ConnectionViewModel context)
        {
            contextConnection = context;
            SearchBox = "";
        }

        private void ItemsOnFilter(object sender, FilterEventArgs filterEventArgs)
        {
            NodeDefinition item = filterEventArgs.Item as NodeDefinition;

            if (contextConnection != null)
            {
                if (contextConnection.From != null)
                {
                    var accept = false;
                    if (contextConnection.From.IoType == IoType.Exec)
                    {
                        if (item.NodeType == NodeType.Statement)
                            accept = true;
                    }

                    if (!accept && (item.Inputs == null || item.Inputs.All(o => o.Type != contextConnection.From.IoType)))
                    {
                        filterEventArgs.Accepted = false;
                        return;
                    }
                }
                else if (contextConnection.To != null)
                {
                    var accept = false;
                    if (contextConnection.To.IoType == IoType.Exec)
                    {
                        if (item.NodeType == NodeType.Statement)
                            accept = true;
                    }

                    if (!accept && (item.Outputs == null || item.Outputs.All(o => o.Type != contextConnection.To.IoType)))
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

            if (allItems.Where(i => i.Header.ToLower() == SearchBox.ToLower()).Count() == 1)
                filterEventArgs.Accepted = item.Header.ToLower() == SearchBox.ToLower();
            else
                filterEventArgs.Accepted = item.Header.ToLower().Contains(SearchBox.ToLower());
        }
    }
}