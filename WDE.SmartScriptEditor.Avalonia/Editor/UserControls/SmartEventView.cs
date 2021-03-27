using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Models;

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
        
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.Handled)
                return;
            
            if (e.ClickCount == 1)
            {
                if (DirectEditParameter != null)
                {
                    /*if (e.OriginalSource is Run originalRun && originalRun.DataContext != null &&
                        originalRun.DataContext != DataContext)
                    {
                        DirectEditParameter.Execute(originalRun.DataContext);
                        return;
                    }*/
                }

                DeselectActionsOfDeselectedEventsRequest?.Execute(null);

                if (!GetSelected(this))
                {
                    if (!e.KeyModifiers.HasFlag(MultiselectGesture))
                        DeselectAllRequest?.Execute(null);
                    SetSelected(this, true);
                }
            }
            else if (e.ClickCount == 2)
            {
                EditEventCommand?.Execute(DataContext);
            }
            
            e.Handled = true;
        }

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<SmartEventView, IControl, bool>("Selected");
        public static bool GetSelected(IControl control) => (bool)control.GetValue(SelectedProperty);
        public static void SetSelected(IControl control, bool value) => control.SetValue(SelectedProperty, value);
    }
}