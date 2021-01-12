using System.Windows;
using System.Windows.Controls;

namespace GeminiGraphEditor
{
    public class ConnectionItemsControl : ListBox
    {
        public ConnectionItemsControl()
        {
            SelectionMode = SelectionMode.Extended;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ConnectionItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ConnectionItem;
        }
    }
}