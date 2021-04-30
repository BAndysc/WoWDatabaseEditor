using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.Avalonia.Helpers
{
    public class FieldValueTemplateSelector : IDataTemplate
    {
        public DataTemplate? GenericTemplate { get; set; }
        public DataTemplate? BoolTemplate { get; set; }

        public IControl Build(object param)
        {
            if ((param is DatabaseCellViewModel vm && vm.ParameterValue is ParameterValue<long> holder && holder.Parameter is BoolParameter) ||
                (param is ViewModels.MultiRow.DatabaseCellViewModel vm2 && vm2.ParameterValue is ParameterValue<long> holder2 && holder2.Parameter is BoolParameter))
                return BoolTemplate!.Build(param);
            return GenericTemplate!.Build(param);
        }

        public bool Match(object data)
        {
            return data is DatabaseCellViewModel or ViewModels.MultiRow.DatabaseCellViewModel;
        }
    }
}