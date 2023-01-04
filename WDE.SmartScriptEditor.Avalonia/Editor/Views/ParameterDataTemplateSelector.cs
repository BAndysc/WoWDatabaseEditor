using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using WDE.Common.Parameters;
using WDE.Parameters;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    public class ParameterDataTemplateSelector : IDataTemplate
    {
        public DataTemplate? Generic { get; set; }
        public DataTemplate? ButtonParameter { get; set; }
        public DataTemplate? NumberedButtonParameter { get; set; }
        
        public Control? Build(object? item)
        {
            if (item is NumberedEditableParameterActionViewModel && NumberedButtonParameter != null)
                return NumberedButtonParameter.Build(item);
            if (item is EditableParameterActionViewModel && ButtonParameter != null)
                return ButtonParameter.Build(item);
            return Generic?.Build(item) ?? new Panel();
        }

        public bool Match(object? data)
        {
            return data is EditableParameterViewModel || data is EditableParameterActionViewModel;
        }
    }
}