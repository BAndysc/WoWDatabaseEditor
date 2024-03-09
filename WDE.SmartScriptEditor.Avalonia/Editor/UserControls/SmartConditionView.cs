using System.Windows.Input;
using Avalonia.Input;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartConditionView : SelectableTemplatedControl
    {
        public static readonly AvaloniaProperty DeselectAllButConditionsRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllButConditionsRequest));

        public static readonly AvaloniaProperty EditConditionCommandProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(EditConditionCommand));
        
        public static readonly AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectEditParameter));
        

        public ICommand DirectEditParameter
        {
            get => (ICommand?) GetValue(DirectEditParameterProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DirectEditParameterProperty, value);
        }
        
        public ICommand DeselectAllButConditionsRequest
        {
            get => (ICommand?) GetValue(DeselectAllButConditionsRequestProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DeselectAllButConditionsRequestProperty, value);
        }

        public ICommand EditConditionCommand
        {
            get => (ICommand?) GetValue(EditConditionCommandProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(EditConditionCommandProperty, value);
        }

        protected override void OnEdit()
        {
            EditConditionCommand?.Execute(DataContext);
        }

        protected override void OnDirectEdit(bool controlPressed, object context)
        {
            DirectEditParameter?.Execute(context);
        }

        protected override void DeselectOthers()
        {
            DeselectAllButConditionsRequest?.Execute(null);
        }
    }
}
