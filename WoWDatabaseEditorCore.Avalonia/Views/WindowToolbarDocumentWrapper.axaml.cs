using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class WindowToolbarDocumentWrapper : UserControl
{
    public WindowToolbarDocumentWrapper()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}