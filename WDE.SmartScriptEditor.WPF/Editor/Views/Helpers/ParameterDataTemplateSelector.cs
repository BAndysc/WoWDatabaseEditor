using System.Windows;
using System.Windows.Controls;
using WDE.Parameters;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.WPF.Editor.Views.Helpers
{
    public class ParameterDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Generic { get; set; }
        public DataTemplate BoolParameter { get; set; }
        public DataTemplate ButtonParameter { get; set; }
        
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is EditableParameterViewModel<int> intParam && intParam.Parameter.Parameter is BoolParameter boolParameter)
                return BoolParameter;
            if (item is EditableParameterActionViewModel aevm)
                return ButtonParameter;
            return Generic;
        }
    }
}