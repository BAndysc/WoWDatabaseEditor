using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WoWDatabaseEditor.Views
{
    public class PaneDocumentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DocumentTemplate
        {
            get;
            set;
        }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return DocumentTemplate;
        }
    }
}
