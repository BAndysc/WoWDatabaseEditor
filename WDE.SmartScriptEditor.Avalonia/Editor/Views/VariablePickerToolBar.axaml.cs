using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views;

public class VariablePickerToolBar : UserControl
{
    public VariablePickerToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}