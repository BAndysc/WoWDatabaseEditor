using System.Windows;
using System.Windows.Controls;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.Parameters;

namespace WDE.DatabaseEditors.WPF.Helpers
{
    public class FieldValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GenericTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }

        public override DataTemplate SelectTemplate(object param, DependencyObject container)
        {
            if ((param is DatabaseCellViewModel vm && vm.ParameterValue is ParameterValue<long> holder && holder.Parameter is BoolParameter) ||
                (param is ViewModels.MultiRow.DatabaseCellViewModel vm2 && vm2.ParameterValue is ParameterValue<long> holder2 && holder2.Parameter is BoolParameter))
                return BoolTemplate;
            return GenericTemplate;
        }
    }
}