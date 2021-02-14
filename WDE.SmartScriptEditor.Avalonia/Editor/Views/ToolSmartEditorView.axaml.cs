using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Components;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    public partial class ToolSmartEditorView : ToolView
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