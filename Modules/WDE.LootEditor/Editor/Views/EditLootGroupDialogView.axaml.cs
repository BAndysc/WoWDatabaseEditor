using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.LootEditor.Editor.Views;

public partial class EditLootGroupDialogView : UserControl
{
    public EditLootGroupDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}