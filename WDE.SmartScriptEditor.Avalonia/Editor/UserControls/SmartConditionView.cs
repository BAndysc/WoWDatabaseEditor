using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Interactivity;
using WDE.Common.Avalonia.Controls;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartConditionView : SelectableTemplatedControl
    {
        public static readonly AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllRequest));

        public static readonly AvaloniaProperty DeselectAllButConditionsRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllButConditionsRequest));

        public static readonly AvaloniaProperty EditConditionCommandProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(EditConditionCommand));
        
        public static readonly AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectEditParameter));
        
        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DirectEditParameter
        {
            get => (ICommand) GetValue(DirectEditParameterProperty);
            set => SetValue(DirectEditParameterProperty, value);
        }
        
        public ICommand DeselectAllButConditionsRequest
        {
            get => (ICommand) GetValue(DeselectAllButConditionsRequestProperty);
            set => SetValue(DeselectAllButConditionsRequestProperty, value);
        }

        public ICommand EditConditionCommand
        {
            get => (ICommand) GetValue(EditConditionCommandProperty);
            set => SetValue(EditConditionCommandProperty, value);
        }
        
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (lastClickCount == 2 && (e.Timestamp - lastPressedTimestamp) <= 1000)
            {
                EditConditionCommand?.Execute(DataContext);
                e.Handled = true;
            }
        }

        private ulong lastPressedTimestamp = 0;
        private int lastClickCount = 0;
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            lastPressedTimestamp = e.Timestamp;
            lastClickCount = e.ClickCount;
            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    DirectEditParameter?.Execute(tb.OverContext);
                    return;
                }

                DeselectAllButConditionsRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!e.KeyModifiers.HasFlag(MultiselectGesture))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
                e.Handled = true;
            }
        }
    }
}