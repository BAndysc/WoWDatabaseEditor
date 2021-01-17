using System.Windows;
using System.Windows.Controls;
using WDE.Common.Managers;
using WDE.Common.Windows;

namespace WoWDatabaseEditor.Views.Helpers
{
    class PanesStyleSelector : StyleSelector
    {
        public Style? ToolStyle { get; set; }

        public Style? DocumentStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is ITool)
                return ToolStyle!;

            if (item is IDocument)
                return DocumentStyle!;

            return base.SelectStyle(item, container);
        }
    }
}