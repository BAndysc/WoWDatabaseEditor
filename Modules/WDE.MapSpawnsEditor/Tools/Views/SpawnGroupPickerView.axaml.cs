using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.MapSpawnsEditor.Tools.Views;

public partial class SpawnGroupPickerView : UserControl
{
    public SpawnGroupPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}