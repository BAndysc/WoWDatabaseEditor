using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Components;

namespace WDE.EventAiEditor.Avalonia.Editor.Views
{
    public partial class ToolEventAiEditorView : ToolView
    {
        public ToolEventAiEditorView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}