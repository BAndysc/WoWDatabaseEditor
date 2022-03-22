using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow
{
    public class SingleRowDbTableEditorToolBar : UserControl
    {
        public SingleRowDbTableEditorToolBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}