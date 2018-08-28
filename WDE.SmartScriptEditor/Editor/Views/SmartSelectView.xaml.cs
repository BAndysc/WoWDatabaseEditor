using System.Windows;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.Editor.Views
{
    /// <summary>
    /// Interaction logic for SmartSelectView.xaml
    /// </summary>
    public partial class SmartSelectView : Window
    {
        public SmartSelectView()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = (Items.SelectedItem != null);
            Close();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Items.Items.Count > 0)
                {
                    if (Items.SelectedIndex == -1)
                        Items.SelectedIndex = 0;

                    Control_OnMouseDoubleClick(null, null);
                }
            }
        }
    }
}
