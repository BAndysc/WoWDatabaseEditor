using System.Windows;
using System.Windows.Controls;
using AvalonDock.Controls;

namespace WoWDatabaseEditor.Views
{
    public class PaneDocumentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DocumentTemplate { get; set; }
        public DataTemplate? AnchorableDocumentTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is ContentPresenter cp)
            {
                if (cp.TemplatedParent is LayoutAnchorableControl)
                    return AnchorableDocumentTemplate;
            }

            return DocumentTemplate;
        }
    }
}