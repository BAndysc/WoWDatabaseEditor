using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WDE.Blueprints.Editor.DisignTimeViewModels
{
    internal class NodePickerViewModel
    {
        private CollectionViewSource items { get; }

        public ICollectionView AllItems => items.View;

        private IEnumerable<NodePickerItemViewModel> _allItems =>
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
                new NodePickerItemViewModel("Get position", "Expressions"),
            };

        public NodePickerViewModel()
        {
            items = new CollectionViewSource();
            items.Source = _allItems;
            items.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        }
    }

    internal class NodePickerItemViewModel
    {
        public string Header { get; }
        public string Category { get; }

        public NodePickerItemViewModel(string header, string category)
        {
            Header = header;
            Category = category;
        }
    }
}
