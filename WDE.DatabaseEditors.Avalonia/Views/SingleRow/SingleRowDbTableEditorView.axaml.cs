using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow
{
    public class SingleRowDbTableEditorView : UserControl
    {
        public SingleRowDbTableEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}