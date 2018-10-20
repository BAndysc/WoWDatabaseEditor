using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace GeminiGraphEditor
{
    public class ConnectionItemsControl : ListBox
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ConnectionItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ConnectionItem; 
        }

        public ConnectionItemsControl()
        {
            SelectionMode = SelectionMode.Extended;
        }
    }
}
