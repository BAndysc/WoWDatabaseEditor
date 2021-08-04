using System.Windows.Input;
using Avalonia;
using Avalonia.Input;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class GlobalVariableView : SelectableTemplatedControl
    {
        public static AvaloniaProperty DeselectAllButGlobalVariablesRequestProperty =
            AvaloniaProperty.Register<GlobalVariableView, ICommand>(nameof(DeselectAllButGlobalVariablesRequest));

        public static AvaloniaProperty EditGlobalVariableCommandProperty =
            AvaloniaProperty.Register<GlobalVariableView, ICommand>(nameof(EditGlobalVariableCommand));
        
        public ICommand DeselectAllButGlobalVariablesRequest
        {
            get => (ICommand) GetValue(DeselectAllButGlobalVariablesRequestProperty);
            set => SetValue(DeselectAllButGlobalVariablesRequestProperty, value);
        }

        public ICommand EditGlobalVariableCommand
        {
            get => (ICommand) GetValue(EditGlobalVariableCommandProperty);
            set => SetValue(EditGlobalVariableCommandProperty, value);
        }

        protected override void DeselectOthers()
        {
            DeselectAllButGlobalVariablesRequest?.Execute(null);
        }

        protected override void OnEdit()
        {
            EditGlobalVariableCommand?.Execute(DataContext);
        }
    }
}