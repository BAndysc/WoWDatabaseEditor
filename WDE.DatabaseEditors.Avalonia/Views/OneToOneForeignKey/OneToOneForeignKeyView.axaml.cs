using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DatabaseEditors.Avalonia.Views.OneToOneForeignKey;

public class OneToOneForeignKeyView : UserControl
{
    public OneToOneForeignKeyView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}