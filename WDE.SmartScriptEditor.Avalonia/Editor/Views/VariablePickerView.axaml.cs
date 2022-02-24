using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views;

public class VariablePickerView : UserControl
{
    public VariablePickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}