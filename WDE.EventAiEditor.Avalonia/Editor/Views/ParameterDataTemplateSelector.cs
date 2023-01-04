using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using WDE.Common.Parameters;
using WDE.Parameters;
using WDE.EventAiEditor.Editor.ViewModels.Editing;

namespace WDE.EventAiEditor.Avalonia.Editor.Views
{
    public class ParameterDataTemplateSelector : IDataTemplate
    {
        public DataTemplate? Generic { get; set; }
        public DataTemplate? ItemsParameter { get; set; }
        public DataTemplate? FlagParameter { get; set; }
        public DataTemplate? BoolParameter { get; set; }
        public DataTemplate? ButtonParameter { get; set; }
        
        public Control? Build(object? item)
        {
            if (item is EditableParameterViewModel<long> intParam)
            {
                if (intParam.Parameter.Parameter is BoolParameter && BoolParameter != null)
                    return BoolParameter.Build(item);
                if (intParam.Parameter.Parameter is FlagParameter && FlagParameter != null)
                    return FlagParameter.Build(item);
                if (intParam.UseModernPicker && ItemsParameter != null)
                    return ItemsParameter.Build(item);
            }
            else if (item is EditableParameterActionViewModel aevm && ButtonParameter != null)
                return ButtonParameter.Build(item);
            return Generic?.Build(item) ?? new Panel();
        }

        public bool Match(object? data)
        {
            return data is EditableParameterViewModel || data is EditableParameterActionViewModel;
        }
    }
}