using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.MapSpawnsEditor.Views;

public class SpawnsQueriesToolView : UserControl
{
    public SpawnsQueriesToolView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}