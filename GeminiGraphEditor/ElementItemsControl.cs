using System.Windows;
using System.Windows.Controls;

namespace GeminiGraphEditor
{
    public class ElementItemsControl : ListBox
    {
        public ElementItemsControl()
        {
            SelectionMode = SelectionMode.Extended;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ElementItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ElementItem;
        }
    }
}