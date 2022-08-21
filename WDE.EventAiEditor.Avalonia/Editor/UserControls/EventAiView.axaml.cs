using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for EventAiView.xaml
    /// </summary>
    public partial class EventAiView : UserControl
    {
        public EventAiView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}