using System.Windows;
using System.Windows.Controls;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.WPF.Helpers
{
    public class DbTableFieldValueContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? GenericTemplate { get; set; }
        public DataTemplate? ReadOnlyGenericTemplate { get; set; }
        public DataTemplate? BoolTemplate { get; set; }
        public DataTemplate? ParameterTemplate { get; set; }
        
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DbTableField<bool> boolField)
                return BoolTemplate!;
            if (item is IDbTableField field)
            {
                if (field.IsParameter)
                    return field.IsReadOnly ? ReadOnlyGenericTemplate! : ParameterTemplate!;
                return field.IsReadOnly ? ReadOnlyGenericTemplate! : GenericTemplate!;
            }
            
            return base.SelectTemplate(item, container);
        }
    }
}