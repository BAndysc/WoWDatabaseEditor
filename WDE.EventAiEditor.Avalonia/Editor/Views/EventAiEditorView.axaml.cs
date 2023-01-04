using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.EventAiEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for EventAiEditorView
    /// </summary>
    public partial class EventAiEditorView : UserControl
    {
        public EventAiEditorView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}