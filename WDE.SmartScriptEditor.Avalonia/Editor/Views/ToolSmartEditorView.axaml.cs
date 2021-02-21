using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    public partial class ToolSmartEditorView : UserControl
    {
        public ToolSmartEditorView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}