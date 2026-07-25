using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class UnitConditionEditorView : DialogViewBase
    {
        public UnitConditionEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
