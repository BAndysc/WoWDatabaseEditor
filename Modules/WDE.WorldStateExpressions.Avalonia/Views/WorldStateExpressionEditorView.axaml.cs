using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.WorldStateExpressions.Avalonia.Views
{
    public partial class WorldStateExpressionEditorView : DialogViewBase
    {
        public WorldStateExpressionEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
