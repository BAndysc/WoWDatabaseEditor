using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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