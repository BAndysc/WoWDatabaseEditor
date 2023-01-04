using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class Window1 : Window
{
    public Window1()
    {

    }

    public Window1(IMainWindowHolder mainWindowHolder)
    {
        InitializeComponent();
        this.AttachDevTools();
        mainWindowHolder.RootWindow = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}