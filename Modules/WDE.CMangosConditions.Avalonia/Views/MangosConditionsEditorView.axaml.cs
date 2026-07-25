using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class MangosConditionsEditorView : DialogViewBase
    {
        public MangosConditionsEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
