using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = this;
    }
    
    public List<A> Items { get; } = new List<A>()
    {
        new A(),
        new A(),
    };
    
    public List<ColumnDescriptor> Columns { get; } = new List<ColumnDescriptor>()
    {
        ColumnDescriptor.TextColumn("Name", "Name", 100, false)
    };
}

public class A
{
    public string Name => "abc";
}