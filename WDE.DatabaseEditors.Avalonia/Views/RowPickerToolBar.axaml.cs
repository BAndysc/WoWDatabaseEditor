using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DatabaseEditors.Avalonia.Views;

public partial class RowPickerToolBar : UserControl
{
    public RowPickerToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}