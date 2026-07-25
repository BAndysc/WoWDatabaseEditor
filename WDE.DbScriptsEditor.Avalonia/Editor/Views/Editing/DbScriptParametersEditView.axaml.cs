using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DbScriptsEditor.Avalonia.Editor.Views.Editing
{
    public partial class DbScriptParametersEditView : UserControl
    {
        public DbScriptParametersEditView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
