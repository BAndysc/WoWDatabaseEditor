using System.Windows;
using System.Windows.Controls;
using WDE.DatabaseEditors.Models;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.WPF.Helpers
{
    public class FieldValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GenericTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ParameterValueHolder<long> holder && holder.Parameter is BoolParameter)
                return BoolTemplate;
            // if (item is IDbTableField)
            return GenericTemplate;

            // return base.SelectTemplate(item, container);
        }
    }
}