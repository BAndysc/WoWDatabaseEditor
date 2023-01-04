using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.Common.Avalonia.Themes
{
    public class ParameterDataTemplateSelector : IDataTemplate
    {
        public DataTemplate? Generic { get; set; }
        public DataTemplate? BoolParameter { get; set; }
        
        public Control? Build(object? item)
        {
            if (item is IParameterValueHolder<long> intParam && intParam.Parameter is BoolParameter boolParameter && BoolParameter != null)
                return BoolParameter.Build(item);
            return Generic?.Build(item) ?? new Panel();
        }

        public bool Match(object? data)
        {
            return data is IParameterValueHolder;
        }
    }
}