using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using WDE.Parameters;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    public class ParameterDataTemplateSelector : IDataTemplate
    {
        public DataTemplate Generic { get; set; }
        public DataTemplate BoolParameter { get; set; }
        public DataTemplate ButtonParameter { get; set; }
        
        public IControl Build(object item)
        {
            if (item is EditableParameterViewModel<int> intParam && intParam.Parameter.Parameter is BoolParameter boolParameter)
                return BoolParameter.Build(item);
            if (item is EditableParameterActionViewModel aevm)
                return ButtonParameter.Build(item);
            return Generic.Build(item);
        }

        public bool Match(object data)
        {
            return data is EditableParameterViewModel || data is EditableParameterActionViewModel;
        }
    }
}