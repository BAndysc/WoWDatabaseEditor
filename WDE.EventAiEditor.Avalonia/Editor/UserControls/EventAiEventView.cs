using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WDE.Common.Avalonia.Controls;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for EventAiEventView.xaml
    /// </summary>
    public class EventAiEventView : SelectableTemplatedControl
    {
        public static readonly DirectProperty<EventAiEventView, ICommand?> EditEventCommandProperty =
            AvaloniaProperty.RegisterDirect<EventAiEventView, ICommand?>(
                nameof(EditEventCommand),
                o => o.EditEventCommand,
                (o, v) => o.EditEventCommand = v);

        public static readonly DirectProperty<EventAiEventView, ICommand?> DeselectActionsOfDeselectedEventsRequestProperty =
            AvaloniaProperty.RegisterDirect<EventAiEventView, ICommand?>(
                nameof(DeselectActionsOfDeselectedEventsRequest),
                o => o.DeselectActionsOfDeselectedEventsRequest,
                (o, v) => o.DeselectActionsOfDeselectedEventsRequest = v);
        
        
        public static readonly DirectProperty<EventAiEventView, ICommand?> DirectEditParameterProperty =
            AvaloniaProperty.RegisterDirect<EventAiEventView, ICommand?>(
                nameof(DirectEditParameter),
                o => o.DirectEditParameter,
                (o, v) => o.DirectEditParameter = v);
        
        private ICommand? editEventCommand;
        public ICommand? EditEventCommand
        {
            get => editEventCommand;
            set => SetAndRaise(EditEventCommandProperty, ref editEventCommand, value);
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
        
        protected override void DeselectOthers()
        {
            DeselectActionsOfDeselectedEventsRequest?.Execute(null);
        }

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<EventAiEventView, Control, bool>("Selected");
        public static bool GetSelected(Control control) => (bool?)control.GetValue(SelectedProperty) ?? false;
        public static void SetSelected(Control control, bool value) => control.SetValue(SelectedProperty, value);
    }
}
