using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class WindowViewDocumentWrapper : UserControl
{
    public WindowViewDocumentWrapper()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}