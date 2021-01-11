using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public partial class SmartActionView : UserControl
    {
        public static DependencyProperty IsSelectedProperty
            = DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(SmartActionView),
                new PropertyMetadata(false));

        public static DependencyProperty DeselectAllRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectAllRequest),
                typeof(ICommand),
                typeof(SmartActionView));

        public static DependencyProperty DeselectAllEventsRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectAllEventsRequest),
                typeof(ICommand),
                typeof(SmartActionView));

        public static DependencyProperty EditActionCommandProperty
            = DependencyProperty.Register(
                nameof(EditActionCommand),
                typeof(ICommand),
                typeof(SmartActionView));

        public SmartActionView() { InitializeComponent(); }

        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DeselectAllEventsRequest
        {
            get => (ICommand) GetValue(DeselectAllEventsRequestProperty);
            set => SetValue(DeselectAllEventsRequestProperty, value);
        }

        public ICommand EditActionCommand
        {
            get => (ICommand) GetValue(EditActionCommandProperty);
            set => SetValue(EditActionCommandProperty, value);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ClickCount == 1)
            {
                DeselectAllEventsRequest?.Execute(null);
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
            }
            else if (e.ClickCount == 2) EditActionCommand?.Execute(DataContext);
        }
    }
}