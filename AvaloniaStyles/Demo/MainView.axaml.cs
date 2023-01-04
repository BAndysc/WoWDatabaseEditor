using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaStyles.Demo;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void ShowDemoDialog(object? sender, RoutedEventArgs e)
    {
        if (Parent is Window w)
            new MessageBox().ShowDialog(w);
        else
            Console.WriteLine("not supported");
    }
}