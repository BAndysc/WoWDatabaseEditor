using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartScriptEditorView
    /// </summary>
    public class SmartScriptEditorView : UserControl
    {
        public SmartScriptEditorView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}