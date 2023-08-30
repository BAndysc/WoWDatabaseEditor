using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace WDE.MapSpawns.Views;

public class SpawnsToolView : UserControl
{
    public SpawnsToolView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InputElement_OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
    }
}