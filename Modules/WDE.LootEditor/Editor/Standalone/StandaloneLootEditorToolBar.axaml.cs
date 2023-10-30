using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.LootEditor.Editor.Standalone;

public partial class StandaloneLootEditorToolBar : UserControl
{
    public StandaloneLootEditorToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}