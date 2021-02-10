using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.HistoryWindow.Views
{
    /// <summary>
    ///     Interaction logic for HistoryView.xaml
    /// </summary>
    public class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}