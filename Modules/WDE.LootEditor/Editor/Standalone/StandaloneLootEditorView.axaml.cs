using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.LootEditor.Editor.Standalone;

public partial class StandaloneLootEditorView : UserControl
{
    public StandaloneLootEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}