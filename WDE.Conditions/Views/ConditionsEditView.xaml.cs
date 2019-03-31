using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace WDE.Conditions.Views
{
    /// <summary>
    /// Interaction logic for ConditionsEditView.xaml
    /// </summary>
    public partial class ConditionsEditView : Window
    {
        public static Regex numbersOnlyRegex = new Regex("[^0-9.-]+");

        public ConditionsEditView()
        {
            InitializeComponent();
        }

        private void Button_Click_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = numbersOnlyRegex.IsMatch(e.Text);
        }
    }
}
