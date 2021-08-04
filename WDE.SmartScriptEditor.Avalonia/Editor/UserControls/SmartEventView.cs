using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WDE.Common.Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartEventView.xaml
    /// </summary>
    public class SmartEventView : SelectableTemplatedControl
    {
        public static readonly DirectProperty<SmartEventView, ICommand?> EditEventCommandProperty =
            AvaloniaProperty.RegisterDirect<SmartEventView, ICommand?>(
                nameof(EditEventCommand),
                o => o.EditEventCommand,
                (o, v) => o.EditEventCommand = v);

        public static readonly DirectProperty<SmartEventView, ICommand?> DeselectAllRequestProperty =
            AvaloniaProperty.RegisterDirect<SmartEventView, ICommand?>(
                nameof(DeselectAllRequest),
                o => o.DeselectAllRequest,
                (o, v) => o.DeselectAllRequest = v);
        
        public static readonly DirectProperty<SmartEventView, ICommand?> DeselectActionsOfDeselectedEventsRequestProperty =
            AvaloniaProperty.RegisterDirect<SmartEventView, ICommand?>(
                nameof(DeselectActionsOfDeselectedEventsRequest),
                o => o.DeselectActionsOfDeselectedEventsRequest,
                (o, v) => o.DeselectActionsOfDeselectedEventsRequest = v);
        
        
        public static readonly DirectProperty<SmartEventView, ICommand?> DirectEditParameterProperty =
            AvaloniaProperty.RegisterDirect<SmartEventView, ICommand?>(
                nameof(DirectEditParameter),
                o => o.DirectEditParameter,
                (o, v) => o.DirectEditParameter = v);
        
        private ICommand? editEventCommand;
        public ICommand? EditEventCommand
        {
            get => editEventCommand;
            set => SetAndRaise(EditEventCommandProperty, ref editEventCommand, value);
        }

        private ICommand? deselectAllRequest;
        public ICommand? DeselectAllRequest
        {
            get => deselectAllRequest;
            set => SetAndRaise(DeselectAllRequestProperty, ref deselectAllRequest, value);
        }

        private ICommand? deselectActionsOfDeselectedEventsRequest;
        public ICommand? DeselectActionsOfDeselectedEventsRequest
        {
            get => deselectActionsOfDeselectedEventsRequest;
            set => SetAndRaise(DeselectActionsOfDeselectedEventsRequestProperty, ref deselectActionsOfDeselectedEventsRequest, value);
        }
        
        private ICommand? directEditParameter;
        public ICommand? DirectEditParameter
        {
            get => directEditParameter;
            set => SetAndRaise(DirectEditParameterProperty, ref directEditParameter, value);
        }

        protected override void OnEdit()
        {
            EditEventCommand?.Execute(DataContext);
        }

        protected override void OnDirectEdit(object context)
        {
            DirectEditParameter?.Execute(context);
        }
        
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    return;
                }

                DeselectActionsOfDeselectedEventsRequest?.Execute(null);

                if (!GetSelected(this))
                {
                    if (!e.KeyModifiers.HasFlag(MultiselectGesture))
                        DeselectAllRequest?.Execute(null);
                    SetSelected(this, true);
                }
            
                e.Handled = true;
            }
        }

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<SmartEventView, IControl, bool>("Selected");
        public static bool GetSelected(IControl control) => (bool)control.GetValue(SelectedProperty);
        public static void SetSelected(IControl control, bool value) => control.SetValue(SelectedProperty, value);
    }
}