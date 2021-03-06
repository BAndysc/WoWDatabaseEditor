using System.Windows;
using System.Windows.Controls;
using WDE.Common.Annotations;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.WPF.Helpers
{
    public class DbTableEditorViewStyleSelector : StyleSelector
    {
        public Style? groupHeaderStyle { get; set; }
        public Style? fieldStyle { get; set; }
        
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is IDbTableColumnCategory)
                return groupHeaderStyle;
            else if (item is IDbTableField)
                return fieldStyle;
            return base.SelectStyle(item, container);
        }
    }
}