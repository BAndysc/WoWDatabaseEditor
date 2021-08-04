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
        public static AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<GlobalVariableView, ICommand>(nameof(DeselectAllRequest));

        public static AvaloniaProperty DeselectAllButGlobalVariablesRequestProperty =
            AvaloniaProperty.Register<GlobalVariableView, ICommand>(nameof(DeselectAllButGlobalVariablesRequest));

        public static AvaloniaProperty EditGlobalVariableCommandProperty =
            AvaloniaProperty.Register<GlobalVariableView, ICommand>(nameof(EditGlobalVariableCommand));
        
        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

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

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.ClickCount == 1)
            {
                DeselectAllButGlobalVariablesRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!e.KeyModifiers.HasFlag(MultiselectGesture))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
                e.Handled = true;
            }
        }

        protected override void OnEdit()
        {
            EditGlobalVariableCommand?.Execute(DataContext);
        }
    }
}