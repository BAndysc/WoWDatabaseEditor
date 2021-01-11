using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartEventView.xaml
    /// </summary>
    public partial class SmartEventView : UserControl
    {
        public static DependencyProperty EditEventCommandProperty
            = DependencyProperty.Register(
                "EditEventCommand",
                typeof(ICommand),
                typeof(SmartEventView));


        public static DependencyProperty DeselectAllRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectAllRequest),
                typeof(ICommand),
                typeof(SmartEventView));

        public static DependencyProperty DeselectActionsOfDeselectedEventsRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectActionsOfDeselectedEventsRequest),
                typeof(ICommand),
                typeof(SmartEventView));

        public static DependencyProperty IsSelectedProperty
            = DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(SmartEventView),
                new PropertyMetadata(false));

        public SmartEventView() { InitializeComponent(); }

        public ICommand EditEventCommand
        {
            get => (ICommand) GetValue(EditEventCommandProperty);
            set => SetValue(EditEventCommandProperty, value);
        }

        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DeselectActionsOfDeselectedEventsRequest
        {
            get => (ICommand) GetValue(DeselectActionsOfDeselectedEventsRequestProperty);
            set => SetValue(DeselectActionsOfDeselectedEventsRequestProperty, value);
        }

        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DeselectActionsOfDeselectedEventsRequest?.Execute(null);
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
            }
            else if (e.ClickCount == 2) EditEventCommand?.Execute(DataContext);
        }
    }
}