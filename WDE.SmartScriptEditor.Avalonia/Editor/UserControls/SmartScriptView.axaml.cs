using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartScriptView.xaml
    /// </summary>
    public partial class SmartScriptView : UserControl
    {
        public SmartScriptView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}