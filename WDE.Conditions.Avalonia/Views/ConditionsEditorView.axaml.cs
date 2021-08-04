using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.Conditions.Avalonia.Views
{
    public class ConditionsEditorView : DialogViewBase
    {
        public ConditionsEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}