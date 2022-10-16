using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views;

public class SmartScriptEditorToolBar : UserControl
{
    public SmartScriptEditorToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}