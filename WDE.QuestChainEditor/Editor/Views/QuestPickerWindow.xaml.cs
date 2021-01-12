using System.Windows;
using System.Windows.Input;

namespace WDE.QuestChainEditor.Editor.Views
{
    /// <summary>
    ///     Interaction logic for QuestPickerWindow.xaml
    /// </summary>
    public partial class QuestPickerWindow : Window
    {
        public QuestPickerWindow()
        {
            InitializeComponent();
        }

        private void NodePicker_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                DialogResult = true;
                Close();
            }
            else
                base.OnPreviewKeyDown(e);
        }
    }
}