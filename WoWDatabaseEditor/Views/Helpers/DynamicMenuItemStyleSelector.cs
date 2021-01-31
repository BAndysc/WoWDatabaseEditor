using System.Windows;
using System.Windows.Controls;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Providers;

namespace WoWDatabaseEditor.Views.Helpers
{
    public class DynamicMenuItemStyleSelector: StyleSelector
    {
        public Style? MainMenuItemStyle { get; set; }
        public Style? CategoryItemStyle { get; set; }
        public Style? DocumentItemStyle { get; set; }
        public Style? CommandItemStyle { get; set; }
        public Style? SeparatorItemStyle { get; set; }
        
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is IMainMenuItem)
                return MainMenuItemStyle!;
            if (item is IMenuCategoryItem)
                return CategoryItemStyle!;
            if (item is IMenuDocumentItem)
                return DocumentItemStyle!;
            if (item is IMenuCommandItem)
                return CommandItemStyle!;
            if (item is IMenuSeparator)
                return SeparatorItemStyle!;

            return base.SelectStyle(item, container);
        }
    }
}