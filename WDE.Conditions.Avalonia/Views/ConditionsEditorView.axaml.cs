using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.Conditions.Avalonia.Views
{
    public partial class ConditionsEditorView : DialogViewBase
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