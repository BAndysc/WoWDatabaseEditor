using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WDE.SmartScriptEditor.Editor.Views
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
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FindVisualChild<TextBox>(Parameters.ItemContainerGenerator.ContainerFromIndex(0))?.Focus();
        }

        public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        return (T) child;

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null)
                        return childItem;
                }
            }

            return null;
        }
    }
}