using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.Parameters.Views;

public partial class UnixTimestampEditorView : UserControl
{
    public UnixTimestampEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}