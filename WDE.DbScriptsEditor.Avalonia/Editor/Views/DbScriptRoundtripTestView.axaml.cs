using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DbScriptsEditor.Avalonia.Editor.Views
{
    public partial class DbScriptRoundtripTestView : UserControl
    {
        public DbScriptRoundtripTestView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
