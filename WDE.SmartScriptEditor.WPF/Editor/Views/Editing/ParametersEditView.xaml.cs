using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.WPF.Editor.Views.Editing
{
    /// <summary>
    ///     Interaction logic for ParametersEditView.xaml
    /// </summary>
    public partial class ParametersEditView : UserControl
    {
        public ParametersEditView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ParametersEditViewModel viewModel)
                viewModel.BeforeAccept -= ForceUpdateBinding;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ParametersEditViewModel viewModel)
            {
                viewModel.BeforeAccept += ForceUpdateBinding;
                if (viewModel.FocusFirst)
                    FindVisualChild<TextBox>(Parameters.ItemContainerGenerator.ContainerFromIndex(0))?.Focus();
            }
        }

        public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T x)
                        return (T) child;

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null)
                        return childItem;
                }
            }

            return null;
        }

        // hack to Update TextBox binding
        private void ForceUpdateBinding()
        {
            var focusedElement = Keyboard.FocusedElement as FrameworkElement;

            if (focusedElement is TextBox)
            {
                var expression = focusedElement.GetBindingExpression(TextBox.TextProperty);
                expression?.UpdateSource();
            }
        }
    }
}