using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.WPF.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartScriptView.xaml
    /// </summary>
    public partial class SmartScriptView : UserControl
    {
        public static DependencyProperty DeleteEventCommandProperty =
            DependencyProperty.Register("DeleteEventCommand", typeof(ICommand), typeof(SmartScriptView));

        public SmartScriptView()
        {
            InitializeComponent();
        }

        public ICommand DeleteEventCommand
        {
            get => (ICommand) GetValue(DeleteEventCommandProperty);
            set => SetValue(DeleteEventCommandProperty, value);
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                DeleteEventCommand?.Execute(this);
        }
    }
}