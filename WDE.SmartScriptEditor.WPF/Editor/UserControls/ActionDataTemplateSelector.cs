using System.Windows;
using System.Windows.Controls;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.WPF.Editor.UserControls
{
    public class ActionDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ActionTemplate { get; set; }
        public DataTemplate CommentTemplate { get; set; } 
        
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SmartAction {Id: SmartConstants.ActionComment})
                return CommentTemplate;
            
            return ActionTemplate;
        }
    }
}