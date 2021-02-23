using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Components;

namespace WDE.HistoryWindow.Views
{
    /// <summary>
    ///     Interaction logic for HistoryView.xaml
    /// </summary>
    public class HistoryView : ToolView
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