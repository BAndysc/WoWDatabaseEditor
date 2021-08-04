namespace WDE.Blueprints.Editor.DisignTimeViewModels
{
    internal class NodePickerViewModel
    {
        public NodePickerViewModel()
        {
            Items = new CollectionViewSource();
            Items.Source = allItems;
            Items.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        }

        private CollectionViewSource Items { get; }

        public ICollectionView AllItems => Items.View;

        private IEnumerable<NodePickerItemViewModel> allItems =>
            new[]
            {
                new NodePickerItemViewModel("On spell hit", "Events"),
                new NodePickerItemViewModel("On reset", "Events"),
                new NodePickerItemViewModel("On passenger boarded", "Events"),

                new NodePickerItemViewModel("Go to point", "Actions"),
                new NodePickerItemViewModel("Say", "Actions"),
                new NodePickerItemViewModel("Play sound", "Actions"),
                new NodePickerItemViewModel("Foreach", "Actions"),
                new NodePickerItemViewModel("If (branch)", "Actions"),

                new NodePickerItemViewModel("Me", "Expressions"),
                new NodePickerItemViewModel("Get position", "Expressions")
            };
    }

    internal class NodePickerItemViewModel
    {
        public NodePickerItemViewModel(string header, string category)
        {
            Header = header;
            Category = category;
        }

        public string Header { get; }
        public string Category { get; }
    }
}