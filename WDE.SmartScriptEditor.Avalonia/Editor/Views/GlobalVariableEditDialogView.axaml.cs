using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    public class GlobalVariableEditDialogView : DialogViewBase
    {
        public GlobalVariableEditDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}