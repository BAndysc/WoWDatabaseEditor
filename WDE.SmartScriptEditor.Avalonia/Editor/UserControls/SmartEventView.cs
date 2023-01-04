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

        public static readonly DirectProperty<SmartEventView, ICommand?> DeselectActionsOfDeselectedEventsRequestProperty =
            AvaloniaProperty.RegisterDirect<SmartEventView, ICommand?>(
                nameof(DeselectActionsOfDeselectedEventsRequest),
                o => o.DeselectActionsOfDeselectedEventsRequest,
                (o, v) => o.DeselectActionsOfDeselectedEventsRequest = v);
        
        public static AvaloniaProperty DirectOpenParameterProperty =
            AvaloniaProperty.Register<SmartEventView, ICommand>(nameof(DirectOpenParameter));
        
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

        public ICommand? DirectOpenParameter
        {
            get => (ICommand?) GetValue(DirectOpenParameterProperty);
            set => SetValue(DirectOpenParameterProperty, value);
        }

        protected override void OnEdit()
        {
            EditEventCommand?.Execute(DataContext);
        }

        protected override void OnDirectEdit(bool controlPressed, object context)
        {
            if (controlPressed) 
                DirectOpenParameter?.Execute(context);
            else
                DirectEditParameter?.Execute(context);
        }
        
        protected override void DeselectOthers()
        {
            DeselectActionsOfDeselectedEventsRequest?.Execute(null);
        }

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<SmartEventView, Control, bool>("Selected");
        public static bool GetSelected(Control control) => (bool?)control.GetValue(SelectedProperty) ?? false;
        public static void SetSelected(Control control, bool value) => control.SetValue(SelectedProperty, value);
    }
}
